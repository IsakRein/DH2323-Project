using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Tilemaps;
using System.Text;

class State
{
    public bool isCollapsed = false;
    public int state = -1;

    private Vector3Int pos;
    public BitArray superPosition;
    private int stateCount;
    private int possibleStates;

    public State(int stateCount, Vector3Int pos)
    {
        this.stateCount = stateCount;
        possibleStates = stateCount;
        superPosition = new BitArray(stateCount, true);
        this.pos = pos;
    }

    public void Reset()
    {
        isCollapsed = false;
        state = -1;
        superPosition.SetAll(true);
        possibleStates = stateCount;
    }

    public bool Constrain(BitArray constraints)
    {
        for (int i = 0; i < stateCount; i++)
        {
            if (constraints[i] && superPosition[i])
            {
                superPosition[i] = false;
                possibleStates--;
            }
        }

        if (possibleStates == 1)
        {
            Collapse();
            return true;
        }

        if (possibleStates == 0)
        {
            Debug.Log($"No possible states for {pos}");
            return false;
        }

        return true;
    }

    public static string SuperPositionString(BitArray superPosition)
    {
        var sb = new StringBuilder();

        for (int i = 0; i < superPosition.Count; i++)
        {
            char c = superPosition[i] ? '1' : '0';
            sb.Append(c);
        }

        return sb.ToString();
    }

    public void Collapse()
    {
        Assert.IsTrue(possibleStates > 0);

        int stateIndex = Random.Range(0, possibleStates);

        for (int i = 0; i < superPosition.Length; i++)
        {
            if (superPosition[i])
            {
                if (stateIndex == 0)
                {
                    state = i;
                    break;
                }
                stateIndex--;
            }
        }

        Assert.IsTrue(state != -1);

        isCollapsed = true;
    }

    public void ForceCollapse(int state)
    {
        Assert.IsTrue(superPosition[state]);

        this.state = state;
        isCollapsed = true;
    }

    public int GetEntropy()
    {
        Assert.IsFalse(isCollapsed);

        int entropy = 0;
        for (int i = 0; i < superPosition.Length; i++)
        {
            if (superPosition[i])
            {
                entropy++;
            }
        }

        return entropy;
    }

    public State Clone()
    {
        State clone = new State(stateCount, pos);
        clone.isCollapsed = isCollapsed;
        clone.state = state;
        clone.superPosition = (BitArray)superPosition.Clone();
        clone.possibleStates = possibleStates;
        return clone;
    }
}

class GridSnapshot
{
    public List<List<List<State>>> GridState;

    public GridSnapshot(List<List<List<State>>> grid)
    {
        GridState = new List<List<List<State>>>(grid.Count);

        foreach (var plane in grid)
        {
            List<List<State>> newPlane = new List<List<State>>(plane.Count);
            foreach (var row in plane)
            {
                List<State> newRow = new List<State>(row.Count);
                foreach (var state in row)
                {
                    newRow.Add(state.Clone());
                }
                newPlane.Add(newRow);
            }
            GridState.Add(newPlane);
        }
    }
}

public class WaveFunctionCollapse
{
    public bool isCollapsed = false;
    private int width;
    private int height;
    private int depth;
    private int stateCount;
    private List<List<List<State>>> grid;
    private List<List<BitArray>> allowedNeighbors;
    private List<int> allowedOnBottomEdge;
    private Stack<GridSnapshot> backtrackStack = new Stack<GridSnapshot>();
    private int consecutiveFailures = 0;

    public WaveFunctionCollapse(int width, int height, int depth, List<List<BitArray>> allowedNeighbors, List<int> allowedOnBottomEdge)
    {
        stateCount = allowedNeighbors[0][0].Count;
        this.width = width;
        this.height = height;
        this.depth = depth;
        this.allowedNeighbors = allowedNeighbors;
        this.allowedOnBottomEdge = allowedOnBottomEdge;

        grid = new List<List<List<State>>>(width);

        // Print allowed neighbors for state 1
        for (int i = 0; i < 6; i++)
        {
            Debug.Log($"Allowed neighbors for state 1 in direction {i}: {State.SuperPositionString(allowedNeighbors[1][i])}");
        }

        // Initialize the grid
        for (int x = 0; x < width; x++)
        {
            List<List<State>> plane = new List<List<State>>(height);
            for (int y = 0; y < height; y++)
            {
                List<State> row = new List<State>(depth);
                for (int z = 0; z < depth; z++)
                {
                    State state = new State(stateCount, new Vector3Int(x, y, z));
                    row.Add(state);
                }
                plane.Add(row);
            }
            grid.Add(plane);
        }

        CreateEdgeConstraints();
    }

    public void Reset()
    {
        isCollapsed = false;
        backtrackStack.Clear();
        consecutiveFailures = 0;

        foreach (List<List<State>> plane in grid)
        {
            foreach (List<State> row in plane)
            {
                foreach (State state in row)
                {
                    state.Reset();
                }
            }
        }
        CreateEdgeConstraints();
    }

    public void CreateEdgeConstraints()
    {
        // Go through all 6 sides of the grid
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    if (x == 0 || x == width - 1 || y == height - 1 || z == 0 || z == depth - 1)
                    {
                        ForceCollapse(x, y, z, 0);
                    }
                }
            }
        }

        BitArray bottomEdgeConstraints = new BitArray(stateCount, true);
        bottomEdgeConstraints[0] = false;
        foreach (int state in allowedOnBottomEdge)
        {
            bottomEdgeConstraints[state] = false;
        }

        for (int x = 0; x < width; x++)
        {
            if (depth > 1)
            {
                grid[x][0][1].Constrain(bottomEdgeConstraints);
                if (grid[x][0][1].isCollapsed)
                {
                    Propagate(new Vector3Int(x, 0, 1));
                }
                grid[x][0][depth - 2].Constrain(bottomEdgeConstraints);
                if (grid[x][0][depth - 2].isCollapsed)
                {
                    Propagate(new Vector3Int(x, 0, depth - 2));
                }
            }
        }

        for (int z = 0; z < depth; z++)
        {
            if (width > 1)
            {
                grid[1][0][z].Constrain(bottomEdgeConstraints);
                if (grid[1][0][z].isCollapsed)
                {
                    Propagate(new Vector3Int(1, 0, z));
                }
                grid[width - 2][0][z].Constrain(bottomEdgeConstraints);
                if (grid[width - 2][0][z].isCollapsed)
                {
                    Propagate(new Vector3Int(width - 2, 0, z));
                }
            }
        }
        CheckCollapsed();
    }

    public bool IsCollapsed(int x, int y, int z)
    {
        return grid[x][y][z].isCollapsed;
    }

    public void ForceCollapse(int x, int y, int z, int state)
    {
        grid[x][y][z].ForceCollapse(state);
        Propagate(new Vector3Int(x, y, z));
        CheckCollapsed();
    }

    private void CheckCollapsed()
    {
        isCollapsed = true;
        foreach (List<List<State>> plane in grid)
        {
            foreach (List<State> row in plane)
            {
                foreach (State state in row)
                {
                    if (!state.isCollapsed)
                    {
                        isCollapsed = false;
                        return;
                    }
                }
            }
        }
    }

    public bool Iterate()
    {
        Debug.Log($"Iterating with {backtrackStack.Count} previous states");
        Vector3Int pos = GetLowestEntropy();
        grid[pos.x][pos.y][pos.z].Collapse();
        bool ok = Propagate(pos);
        if (!ok)
        {
            Debug.Log($"Backtracking with {backtrackStack.Count} previous states");
            // Revert to previous state
            if (backtrackStack.Count > 0)
            {
                int backtrackSteps = Mathf.Min(consecutiveFailures + 1, backtrackStack.Count);
                for (int i = 0; i < backtrackSteps; i++)
                {
                    grid = backtrackStack.Pop().GridState;
                }
                consecutiveFailures++;
                return false;
            }
            else
            {
                Reset();
            }
        }
        else
        {
            consecutiveFailures = 0;
        }

        backtrackStack.Push(new GridSnapshot(grid));
        CheckCollapsed();
        return ok;
    }

    public int GetCollapsedState(int x, int y, int z)
    {
        Assert.IsTrue(grid[x][y][z].isCollapsed);
        return grid[x][y][z].state;
    }

    public int GetEntropy(int x, int y, int z)
    {
        Assert.IsFalse(grid[x][y][z].isCollapsed);
        return grid[x][y][z].GetEntropy();
    }

    List<Vector3Int> GetNeighbors(Vector3Int pos)
    {
        List<Vector3Int> deltas = new();

        if (pos.x > 0) deltas.Add(new Vector3Int(-1, 0, 0));
        if (pos.y > 0) deltas.Add(new Vector3Int(0, -1, 0));
        if (pos.z > 0) deltas.Add(new Vector3Int(0, 0, -1));
        if (pos.x < width - 1) deltas.Add(new Vector3Int(1, 0, 0));
        if (pos.y < height - 1) deltas.Add(new Vector3Int(0, 1, 0));
        if (pos.z < depth - 1) deltas.Add(new Vector3Int(0, 0, 1));

        return deltas;
    }

    int VectorToIdx(Vector3Int pos)
    {
        if (pos == new Vector3Int(1, 0, 0)) return 0;
        if (pos == new Vector3Int(-1, 0, 0)) return 1;
        if (pos == new Vector3Int(0, 1, 0)) return 2;
        if (pos == new Vector3Int(0, -1, 0)) return 3;
        if (pos == new Vector3Int(0, 0, 1)) return 4;
        if (pos == new Vector3Int(0, 0, -1)) return 5;

        throw new System.Exception("Invalid vector");
    }

    int GetEntropy(Vector3Int pos)
    {
        return grid[pos.x][pos.y][pos.z].GetEntropy();
    }

    Vector3Int GetLowestEntropy()
    {
        int lowestEntropy = int.MaxValue;
        List<Vector3Int> lowestEntropyPositions = new();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    Vector3Int pos = new Vector3Int(x, y, z);
                    if (grid[x][y][z].isCollapsed)
                    {
                        continue;
                    }
                    int entropy = GetEntropy(pos);

                    if (entropy < lowestEntropy)
                    {
                        lowestEntropy = entropy;
                        lowestEntropyPositions.Clear();
                    }
                    if (entropy == lowestEntropy)
                    {
                        lowestEntropyPositions.Add(pos);
                    }
                }
            }
        }

        Vector3Int lowestEntropyPos = lowestEntropyPositions[Random.Range(0, lowestEntropyPositions.Count)];
        return lowestEntropyPos;
    }

    private bool Propagate(Vector3Int pos)
    {
        Stack<Vector3Int> queue = new();
        queue.Push(pos);

        while (queue.Count > 0)
        {
            Vector3Int currentPos = queue.Pop();
            State currentState = grid[currentPos.x][currentPos.y][currentPos.z];

            Assert.IsTrue(currentState.isCollapsed);

            foreach (Vector3Int delta in GetNeighbors(currentPos))
            {
                Vector3Int neighborPos = currentPos + delta;
                State neighborState = grid[neighborPos.x][neighborPos.y][neighborPos.z];

                if (neighborState.isCollapsed)
                {
                    continue;
                }

                BitArray constraints = new(allowedNeighbors[currentState.state][VectorToIdx(delta)]);
                constraints.Not();

                bool ok = neighborState.Constrain(constraints);

                if (!ok)
                {
                    return false;
                }

                if (neighborState.isCollapsed)
                {
                    queue.Push(neighborPos);
                }
            }
        }
        return true;
    }
}