using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Tilemaps;
using System.Text;

// Todo: make [x][y] vs [(x, y)] consistent
class State
{
    public bool isCollapsed = false;
    public int state = -1;

    private Vector2Int pos;
    private BitArray superPosition;
    private int stateCount;
    private int possibleStates;

    public State(int stateCount, Vector2Int pos)
    {
        this.stateCount = stateCount;
        this.possibleStates = stateCount;
        this.superPosition = new BitArray(stateCount, true);
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
        Assert.IsFalse(isCollapsed);

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
            this.Collapse();
            return true;
        }

        if (possibleStates == 0)
        {
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
        Assert.IsFalse(isCollapsed);
        Assert.IsTrue(possibleStates > 0);

        int stateIndex = Random.Range(0, possibleStates);
        // Debug.Log($"Collapsing to {stateIndex}. Possible States: {possibleStates}. Superposition: {SuperPositionString(superPosition)}");

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
        Assert.IsFalse(isCollapsed);
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
}

class WaveFunctionCollaspe
{
    public bool isCollapsed = false;
    private int width;
    private int height;
    private List<List<State>> grid;
    private List<List<BitArray>> allowedNeighbors;

    public WaveFunctionCollaspe(int width, int height, int stateCount, List<List<BitArray>> allowedNeighbors)
    {
        this.width = width;
        this.height = height;
        this.grid = new List<List<State>>(height);
        this.allowedNeighbors = allowedNeighbors;

        for (int i = 0; i < allowedNeighbors.Count; i++)
        {
            for (int j = 0; j < allowedNeighbors[i].Count; j++)
            {
                Debug.Log($"Allowed neighbors for {i} in direction {j}: {State.SuperPositionString(allowedNeighbors[i][j])}");
            }
        }

        // Initialize the grid
        for (int x = 0; x < width; x++)
        {
            List<State> col = new(height);
            List<int> gridCol = new(height);

            for (int y = 0; y < height; y++)
            {
                State state = new(stateCount, new Vector2Int(x, y));
                col.Add(state);
            }
            grid.Add(col);
        }
    }

    public void Reset()
    {
        isCollapsed = false;

        foreach (List<State> col in grid)
        {
            foreach (State state in col)
            {
                state.Reset();
            }
        }
    }

    public bool IsCollapsed(int x, int y)
    {
        return grid[x][y].isCollapsed;
    }

    public void ForceCollapse(int x, int y, int state)
    {
        grid[x][y].ForceCollapse(state);
        Propagate(new Vector2Int(x, y));
        CheckCollapsed();
    }

    private void CheckCollapsed()
    {
        isCollapsed = true;
        foreach (List<State> col in grid)
        {
            foreach (State state in col)
            {
                if (!state.isCollapsed)
                {
                    isCollapsed = false;
                    return;
                }
            }
        }
    }

    public void Iterate()
    {
        Vector2Int pos = GetLowestEntropy();
        grid[pos.x][pos.y].Collapse();
        bool ok = Propagate(pos);
        if (!ok)
        {
            Reset();
        }
        CheckCollapsed();
    }

    public int GetCollapsedState(int x, int y)
    {
        Assert.IsTrue(grid[x][y].isCollapsed);

        return grid[x][y].state;
    }

    public int GetEntropy(int x, int y)
    {
        Assert.IsFalse(grid[x][y].isCollapsed);
        return grid[x][y].GetEntropy();
    }

    List<Vector2Int> GetNeighbors(Vector2Int pos)
    {
        List<Vector2Int> deltas = new();

        if (pos.x > 0)
        {
            deltas.Add(new Vector2Int(-1, 0));
        }
        if (pos.y > 0)
        {
            deltas.Add(new Vector2Int(0, -1));
        }
        if (pos.x < width - 1)
        {
            deltas.Add(new Vector2Int(1, 0));
        }
        if (pos.y < height - 1)
        {
            deltas.Add(new Vector2Int(0, 1));
        }

        return deltas;
    }

    int VectorToIdx(Vector2Int pos)
    {
        if (pos == new Vector2Int(-1, 0))
        {
            return 0;
        }
        if (pos == new Vector2Int(0, 1))
        {
            return 1;
        }
        if (pos == new Vector2Int(1, 0))
        {
            return 2;
        }
        if (pos == new Vector2Int(0, -1))
        {
            return 3;
        }

        throw new System.Exception("Invalid vector");
    }

    int GetEntropy(Vector2Int pos)
    {
        return grid[pos.x][pos.y].GetEntropy();
    }

    Vector2Int GetLowestEntropy()
    {
        int lowestEntropy = int.MaxValue;
        List<Vector2Int> lowestEntropyPositions = new();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2Int pos = new(x, y);
                if (grid[x][y].isCollapsed)
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

        Vector2Int lowestEntropyPos = lowestEntropyPositions[Random.Range(0, lowestEntropyPositions.Count)];

        return lowestEntropyPos;
    }

    private bool Propagate(Vector2Int pos)
    {
        Stack<Vector2Int> queue = new();
        queue.Push(pos);

        while (queue.Count > 0)
        {
            Vector2Int currentPos = queue.Pop();
            State currentState = grid[currentPos.x][currentPos.y];

            Assert.IsTrue(currentState.isCollapsed);

            foreach (Vector2Int delta in GetNeighbors(currentPos))
            {
                Vector2Int neighborPos = currentPos + delta;
                State neighborState = grid[neighborPos.x][neighborPos.y];

                if (neighborState.isCollapsed)
                {
                    continue;
                }

                BitArray constraints = new(allowedNeighbors[currentState.state][VectorToIdx(delta)]);
                constraints.Not();

                bool ok = neighborState.Constrain(constraints);

                if (!ok)
                {
                    Debug.Log("Contradiction");
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