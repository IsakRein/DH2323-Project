using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

public class Generator : MonoBehaviour
{
    public int width = 10;
    public int depth = 10;
    public int height = 10;
    public float waitBetweenIterations = 0.5f;
    public int seed = 0;
    public List<List<BitArray>> allowedNeighbors;
    public List<GameObject> tiles;
    public List<Mesh> transformedMeshes;
    public List<Material[]> transformedMaterials;
    public List<int> allowedOnEdge;

    private List<Mesh> tileMeshes;
    private List<MeshRenderer> tileMeshRenderers;
    private List<int> allowedOnEdgeTransformed;
    private List<List<List<bool>>> isInstaniated;
    private WaveFunctionCollapse wfc;

    public GameObject emptyTile;

    // Start is called before the first frame update
    void Start()
    {
        Random.InitState(seed);
        CreateTileMeshes();
        CreateTransformedTiles();
        RemoveDuplicateTransformedTiles();
        CreateIsInstantiated();
        TileGraph3D tileGraph = new(transformedMeshes);

        wfc = new WaveFunctionCollapse(width, height, depth, tileGraph.GetAllowedNeighbors(), allowedOnEdgeTransformed);
        StartCoroutine(UpdateGrid());

    }

    private void CreateTransformedTiles()
    {
        transformedMeshes = new List<Mesh>(tiles.Count * 4);
        transformedMaterials = new List<Material[]>(tiles.Count * 4);
        allowedOnEdgeTransformed = new List<int>();

        for (int tileIndex = 0; tileIndex < tiles.Count; tileIndex++)
        {
            GameObject tile = tiles[tileIndex];
            Mesh mesh = tile.GetComponent<MeshFilter>().sharedMesh;
            Material[] materials = tile.GetComponent<MeshRenderer>().sharedMaterials;

            for (int i = 0; i < 4; i++)
            {
                Mesh newMesh = new Mesh();
                Vector3[] vertices = (Vector3[])mesh.vertices.Clone();
                int[] triangles = (int[])mesh.triangles.Clone();
                Vector3[] normals = (Vector3[])mesh.normals.Clone();
                Vector2[] uv = (Vector2[])mesh.uv.Clone();

                // rotate vertices
                Quaternion rotation = Quaternion.Euler(0, 90 * i, 0);
                for (int j = 0; j < vertices.Length; j++)
                {
                    vertices[j] = rotation * vertices[j];
                    normals[j] = rotation * normals[j];
                }

                newMesh.vertices = vertices;
                newMesh.triangles = triangles;
                newMesh.normals = normals;
                newMesh.uv = uv;

                newMesh.subMeshCount = mesh.subMeshCount;
                for (int k = 0; k < mesh.subMeshCount; k++)
                {
                    newMesh.SetTriangles(mesh.GetTriangles(k), k);
                }

                newMesh.RecalculateNormals();
                newMesh.RecalculateBounds();

                transformedMeshes.Add(newMesh);
                transformedMaterials.Add(materials);

                Assert.IsTrue(transformedMeshes.Count == transformedMaterials.Count);

                if (allowedOnEdge.Contains(tileIndex + 1))
                {
                    allowedOnEdgeTransformed.Add(transformedMeshes.Count - 1);
                }
            }
        }
    }

    private void RemoveDuplicateTransformedTiles()
    {
        List<Mesh> uniqueMeshes = new List<Mesh>(transformedMeshes.Count);
        List<Material[]> uniqueMaterials = new List<Material[]>(transformedMaterials.Count);
        List<int> uniqueAllowedOnEdge = new List<int>(allowedOnEdgeTransformed.Count);

        uniqueMeshes.Add(new Mesh());
        uniqueMaterials.Add(new Material[0]);

        for (int i = 0; i < transformedMeshes.Count; i++)
        {
            bool isUnique = true;
            for (int j = 0; j < uniqueMeshes.Count; j++)
            {
                if (AreMeshesEqual(transformedMeshes[i], uniqueMeshes[j]))
                {
                    isUnique = false;
                    break;
                }
            }
            if (isUnique)
            {
                uniqueMeshes.Add(transformedMeshes[i]);
                uniqueMaterials.Add(transformedMaterials[i]);
                if (allowedOnEdgeTransformed.Contains(i))
                {
                    uniqueAllowedOnEdge.Add(uniqueMeshes.Count - 1);
                }

            }
        }
        transformedMeshes = uniqueMeshes;
        transformedMaterials = uniqueMaterials;
        allowedOnEdgeTransformed = uniqueAllowedOnEdge;
    }

    private bool AreMeshesEqual(Mesh mesh1, Mesh mesh2)
    {
        bool allEqual = true;
        for (int i = 0; i < mesh1.vertices.Length; i++)
        {
            bool currentEqual = false;

            for (int j = 0; j < mesh2.vertices.Length; j++)
            {
                if (mesh1.vertices[i] == mesh2.vertices[j])
                {
                    currentEqual = true;
                    break;
                }
            }

            if (!currentEqual)
            {
                allEqual = false;
                break;
            }
        }
        return allEqual;
    }

    private void CreateTileMeshes()
    {
        tileMeshes = new List<Mesh>(tiles.Count);
        tileMeshRenderers = new List<MeshRenderer>(tiles.Count);
        foreach (GameObject tile in tiles)
        {
            tileMeshes.Add(tile.GetComponent<MeshFilter>().sharedMesh);
            tileMeshRenderers.Add(tile.GetComponent<MeshRenderer>());
        }
    }

    private void CreateIsInstantiated()
    {
        isInstaniated = new List<List<List<bool>>>(width);
        for (int x = 0; x < width; x++)
        {
            isInstaniated.Add(new List<List<bool>>(height));
            for (int y = 0; y < height; y++)
            {
                isInstaniated[x].Add(new List<bool>(depth));
                for (int z = 0; z < depth; z++)
                {
                    isInstaniated[x][y].Add(false);
                }
            }
        }
    }

    public void Reset()
    {
        StopAllCoroutines();
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        CreateIsInstantiated();
        wfc.Reset();
        StartCoroutine(UpdateGrid());
    }

    IEnumerator UpdateGrid()
    {
        DrawGrid();
        // while (transform.childCount == 0)
        // {
        //     wfc.Reset();
        while (!wfc.isCollapsed)
        {
            if (!wfc.Iterate())
            {
                if (transform.childCount > 0)
                {
                    foreach (Transform child in transform)
                    {
                        Destroy(child.gameObject);
                    }
                    CreateIsInstantiated();
                }
            }
            DrawGrid();
            yield return new WaitForSeconds(waitBetweenIterations);
        }
        //     yield return new WaitForSeconds(waitBetweenIterations);
        // }
        yield return null;
    }

    void DrawGrid()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    if (!isInstaniated[x][y][z] && wfc.IsCollapsed(x, y, z))
                    {
                        CreateObject(x, y, z, wfc.GetCollapsedState(x, y, z));
                    }
                }
            }
        }
    }

    private void CreateObject(int x, int y, int z, int state)
    {
        string name = $"Tile_{x}_{y}_{z}_State_{state}";

        if (state == 0)
        {
            // if (x == 0 || x == width - 1 || y == 0 || y == height - 1 || z == 0 || z == depth - 1)
            // {
            //     return;
            // }
            // GameObject newTile = Instantiate(emptyTile, new Vector3(x, y, z), Quaternion.identity);
            // newTile.name = name;
            // newTile.transform.SetParent(transform);
            return;
        }

        GameObject tile = new(name);
        tile.transform.SetParent(transform);
        tile.transform.SetLocalPositionAndRotation(new Vector3(x, y, z) - new Vector3((width - 1) / 2.0f, 0, (depth - 1) / 2.0f), Quaternion.identity);
        // tile.transform.SetLocalPositionAndRotation(new Vector3(x, y, z), Quaternion.identity);
        tile.AddComponent<MeshFilter>().mesh = transformedMeshes[state];
        tile.AddComponent<MeshRenderer>().materials = transformedMaterials[state];
        isInstaniated[x][y][z] = true;
    }
}
