using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;


public class Generator : MonoBehaviour
{
    public int width = 10;
    public int height = 10;

    public float waitBetweenIterations = 0.5f;
    public int seed = 0;
    public GameObject tile;
    public List<List<BitArray>> allowedNeighbors;
    public List<Sprite> tileSprites;
    public Sprite unFilledSprite;

    public List<Sprite> transformedSprites;
    private int tileCount;
    private List<List<Tile>> tiles;
    private WaveFunctionCollaspe wfc;

    void Start()
    {
        UnityEngine.Random.InitState(seed);

        transformedSprites = TransformSprites();

        tileCount = transformedSprites.Count;
        TileGraph tileGraph = new(transformedSprites);

        Debug.Log($"Tile count: {tileCount}");

        this.wfc = new WaveFunctionCollaspe(width, height, tileCount, tileGraph.GetAllowedNeighbors());

        InitializeGrid();
        StartCoroutine(UpdateGrid());
    }

    private List<Sprite> TransformSprites()
    {
        List<Sprite> result = new();

        foreach (Sprite sprite in tileSprites)
        {
            float pixelsPerUnit = sprite.pixelsPerUnit;

            List<Sprite> modifiedSprites = new();
            Sprite rotated90 = RotateSprite(sprite, 90, pixelsPerUnit);
            Sprite rotated180 = RotateSprite(sprite, 180, pixelsPerUnit);
            Sprite rotated270 = RotateSprite(sprite, 270, pixelsPerUnit);
            modifiedSprites.Add(sprite);
            modifiedSprites.Add(rotated90);
            modifiedSprites.Add(rotated180);
            modifiedSprites.Add(rotated270);
            modifiedSprites.Add(FlipSprite(sprite, true, false, pixelsPerUnit));
            modifiedSprites.Add(FlipSprite(sprite, false, true, pixelsPerUnit));
            modifiedSprites.Add(FlipSprite(rotated90, true, false, pixelsPerUnit));
            modifiedSprites.Add(FlipSprite(rotated90, false, true, pixelsPerUnit));
            modifiedSprites.Add(FlipSprite(rotated180, true, false, pixelsPerUnit));
            modifiedSprites.Add(FlipSprite(rotated180, false, true, pixelsPerUnit));
            modifiedSprites.Add(FlipSprite(rotated270, true, false, pixelsPerUnit));
            modifiedSprites.Add(FlipSprite(rotated270, false, true, pixelsPerUnit));

            for (int i = 0; i < modifiedSprites.Count; i++)
            {
                bool unique = true;
                for (int j = i + 1; j < modifiedSprites.Count; j++)
                {
                    if (SpritesEqual(modifiedSprites[i], modifiedSprites[j]))
                    {
                        unique = false;
                        break;
                    }
                }
                if (unique)
                {
                    result.Add(modifiedSprites[i]);
                }
            }
        }

        return result;
    }

    private bool SpritesEqual(Sprite sprite1, Sprite sprite2)
    {
        Texture2D texture1 = sprite1.texture;
        Texture2D texture2 = sprite2.texture;

        if (texture1.width != texture2.width || texture1.height != texture2.height)
        {
            return false;
        }

        for (int x = 0; x < texture1.width; x++)
        {
            for (int y = 0; y < texture1.height; y++)
            {
                if (texture1.GetPixel(x, y) != texture2.GetPixel(x, y))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private Sprite FlipSprite(Sprite originalSprite, bool flipX, bool flipY, float pixelsPerUnit)
    {
        // Create a new texture with the same dimensions as the original
        Texture2D flippedTexture = new(originalSprite.texture.width, originalSprite.texture.height);

        // Get the original pixels
        Color32[] originalPixels = originalSprite.texture.GetPixels32();

        // Flip the pixels
        Color32[] flippedPixels = new Color32[originalPixels.Length];
        int width = originalSprite.texture.width;
        int height = originalSprite.texture.height;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int newX = flipX ? width - x - 1 : x;
                int newY = flipY ? height - y - 1 : y;

                flippedPixels[newY * width + newX] = originalPixels[y * width + x];
            }
        }

        // Apply flipped pixels to the new texture
        flippedTexture.SetPixels32(flippedPixels);
        flippedTexture.Apply();
        flippedTexture.filterMode = FilterMode.Point;

        // Create and return a new sprite with the flipped texture
        return Sprite.Create(flippedTexture, new Rect(0, 0, flippedTexture.width, flippedTexture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }

    private Sprite RotateSprite(Sprite originalSprite, float angle, float pixelsPerUnit)
    {
        // Create a new texture with the same dimensions as the original
        Texture2D rotatedTexture = new Texture2D(originalSprite.texture.width, originalSprite.texture.height);

        // Get the original pixels
        Color[] originalPixels = originalSprite.texture.GetPixels();

        // Rotate the pixels
        Color[] rotatedPixels = new Color[originalPixels.Length];
        int width = originalSprite.texture.width;
        int height = originalSprite.texture.height;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int newX = x;
                int newY = y;

                switch ((int)angle)
                {
                    case 90:
                        newX = height - y - 1;
                        newY = x;
                        break;
                    case 180:
                        newX = width - x - 1;
                        newY = height - y - 1;
                        break;
                    case 270:
                        newX = y;
                        newY = width - x - 1;
                        break;
                }

                rotatedPixels[newY * width + newX] = originalPixels[y * width + x];
            }
        }

        // Apply rotated pixels to the new texture
        rotatedTexture.SetPixels(rotatedPixels);
        rotatedTexture.Apply();
        rotatedTexture.filterMode = FilterMode.Point;

        // Create and return a new sprite with the rotated texture
        return Sprite.Create(rotatedTexture, new Rect(0, 0, rotatedTexture.width, rotatedTexture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
    }


    public void Reset()
    {
        StopAllCoroutines();
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        this.wfc.Reset();
        InitializeGrid();
        StartCoroutine(UpdateGrid());
    }

    private void InitializeGrid()
    {
        this.tiles = new(height);

        float scale = 10.0f / math.max(width, height);

        for (int x = 0; x < width; x++)
        {
            List<Tile> col = new(height);

            for (int y = 0; y < height; y++)
            {
                Vector2 pos = new(x - ((width - 1) / 2.0f), y - ((height - 1) / 2.0f));
                pos *= scale;

                GameObject newTile = Instantiate(
                    tile,
                    pos,
                    Quaternion.identity);
                newTile.transform.SetParent(transform);
                newTile.transform.localScale = new Vector3(scale, scale, 1);
                Tile tileScript = newTile.GetComponent<Tile>();
                col.Add(tileScript);
            }

            tiles.Add(col);
        }
    }

    private IEnumerator UpdateGrid()
    {
        while (!wfc.isCollapsed)
        {
            wfc.Iterate();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (wfc.IsCollapsed(x, y))
                    {
                        int collapsedState = wfc.GetCollapsedState(x, y);
                        tiles[x][y].SetSprite(transformedSprites[collapsedState]);
                    }
                    else
                    {
                        tiles[x][y].SetSprite(unFilledSprite);
                        tiles[x][y].SetColor(new Color(175 / 255.0f, 175 / 255.0f, 175 / 255.0f, 1));
                        tiles[x][y].SetText(wfc.GetEntropy(x, y).ToString());
                    }
                }
            }
            yield return new WaitForSeconds(waitBetweenIterations);
        }
        yield return null;
    }
}
