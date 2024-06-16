using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

class TileGraph3D
{
    private List<List<BitArray>> allowedNeighbors;

    public TileGraph3D(List<Mesh> tileMeshes)
    {
        allowedNeighbors = new List<List<BitArray>>(tileMeshes.Count);

        for (int i = 0; i < tileMeshes.Count; i++)
        {
            allowedNeighbors.Add(new List<BitArray>(6));

            for (int direction = 0; direction < 6; direction++)
            {
                allowedNeighbors[i].Add(new BitArray(tileMeshes.Count, false));

                for (int j = 0; j < tileMeshes.Count; j++)
                {
                    if (CheckMeshCompatibility(tileMeshes[i], tileMeshes[j], direction))
                    {
                        allowedNeighbors[i][direction][j] = true;
                    }
                }
            }
        }


        // Empty is allowed above everything
        for (int i = 0; i < tileMeshes.Count; i++)
        {
            List<Vector3> mesh1Corners = GetVerticesAtSide(tileMeshes[i], 2);
            if (mesh1Corners.Count == 0)
            {
                allowedNeighbors[i][2][0] = true;
                allowedNeighbors[0][3][i] = true;
            }
        }

        for (int i = 0; i < 6; i++)
        {
            Assert.IsTrue(allowedNeighbors[0][i][0] == true);
        }

        for (int i = 0; i < tileMeshes.Count; i++)
        {
            Mesh mesh = tileMeshes[i];
            if (GetVerticesAtSide(mesh, 0).Count == 0)
            {
                continue;
            }
            if (GetVerticesAtSide(mesh, 1).Count == 0)
            {
                continue;
            }
            if (GetVerticesAtSide(mesh, 4).Count == 0)
            {
                continue;
            }
            if (GetVerticesAtSide(mesh, 5).Count == 0)
            {
                continue;
            }

            allowedNeighbors[0][2][i] = true;
            allowedNeighbors[i][3][0] = true;
        }

    }


    private bool CheckMeshCompatibility(Mesh mesh1, Mesh mesh2, int direction)
    {
        List<Vector3> mesh1Corners = GetVerticesAtSide(mesh1, direction);
        List<Vector3> mesh2Corners = GetVerticesAtSide(mesh2, GetOppositeDirection(direction));

        // Needs to be able to stand on something
        if (mesh1Corners.Count == 0 && mesh2Corners.Count == 0)
        {
            return direction != 2 && direction != 3;
        }
        else if (mesh1Corners.Count == 0 || mesh2Corners.Count == 0)
        {
            return false;
        }


        bool allMatch = true;
        for (int i = 0; i < mesh1Corners.Count; i++)
        {
            bool currentMatch = false;
            Vector3 flipped = FlipVector(mesh1Corners[i], direction);

            for (int j = 0; j < mesh2Corners.Count; j++)
            {
                if (VerticesMatch(flipped, mesh2Corners[j]))
                {
                    currentMatch = true;
                    break;
                }
            }

            if (!currentMatch)
            {
                allMatch = false;
                break;
            }
        }

        return allMatch;
    }

    private List<Vector3> GetVerticesAtSide(Mesh mesh, int direction)
    {
        List<Vector3> vertices = new();

        // Debug.Log($"VERTEX COUNT: {mesh.vertices.Length}");
        foreach (Vector3 vertex in mesh.vertices)
        {
            // Debug.Log($"Vertex: {vertex}");
            switch (direction)
            {
                case 0:
                    // Positive x
                    if (Mathf.Abs(vertex.x - 0.5f) < 0.01f)
                    {
                        vertices.Add(vertex);
                    }
                    break;
                case 1:
                    // Negative x
                    if (Mathf.Abs(vertex.x + 0.5f) < 0.01f)
                    {
                        vertices.Add(vertex);
                    }
                    break;
                case 2:
                    if (Mathf.Abs(vertex.y - 0.5f) < 0.01f)
                    {
                        vertices.Add(vertex);
                    }
                    break;
                case 3:
                    if (Mathf.Abs(vertex.y + 0.5f) < 0.01f)
                    {
                        vertices.Add(vertex);
                    }
                    break;
                case 4:
                    if (Mathf.Abs(vertex.z - 0.5f) < 0.01f)
                    {
                        vertices.Add(vertex);
                    }
                    break;
                case 5:
                    if (Mathf.Abs(vertex.z + 0.5f) < 0.01f)
                    {
                        vertices.Add(vertex);
                    }
                    break;
            }
        }

        return vertices;
    }

    private int GetOppositeDirection(int direction)
    {
        switch (direction)
        {
            case 0: return 1;
            case 1: return 0;
            case 2: return 3;
            case 3: return 2;
            case 4: return 5;
            case 5: return 4;
            default: throw new System.Exception("Invalid direction");
        }
    }

    private Vector3 FlipVector(Vector3 vec, int direction)
    {
        Vector3 flipped = new(vec.x, vec.y, vec.z);
        switch (direction)
        {
            case 0:
                flipped.x *= -1;
                break;
            case 1:
                flipped.x *= -1;
                break;
            case 2:
                flipped.y *= -1;
                break;
            case 3:
                flipped.y *= -1;
                break;
            case 4:
                flipped.z *= -1;
                break;
            case 5:
                flipped.z *= -1;
                break;
        }
        return flipped;
    }

    private bool VerticesMatch(Vector3 vertex1, Vector3 vertex2)
    {
        return Vector3.Distance(vertex1, vertex2) < 0.01f;
    }

    public List<List<BitArray>> GetAllowedNeighbors()
    {
        return allowedNeighbors;
    }
}
