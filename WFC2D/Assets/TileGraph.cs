using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

class TileGraph
{
    private List<List<BitArray>> allowedNeighbors;
    private int width;
    private int height;
    private List<int> socketIndices;

    public TileGraph(List<Sprite> tileSprites)
    {
        allowedNeighbors = new List<List<BitArray>>(tileSprites.Count);
        socketIndices = new List<int>
        {
            0,
            6,
            13
        };

        width = tileSprites[0].texture.width;
        height = tileSprites[0].texture.height;

        // TODO: transpose dimensions 2 and 3
        for (int i = 0; i < tileSprites.Count; i++)
        {
            allowedNeighbors.Add(new List<BitArray>(4));

            for (int direction = 0; direction < 4; direction++)
            {
                allowedNeighbors[i].Add(new BitArray(tileSprites.Count, false));

                for (int j = 0; j < tileSprites.Count; j++)
                {
                    if (CheckSpriteCompatibility(tileSprites[i], tileSprites[j], direction))
                    {
                        allowedNeighbors[i][direction][j] = true;
                    }
                }
            }
        }
    }

    private bool CheckSpriteCompatibility(Sprite sprite1, Sprite sprite2, int direction)
    {
        if (direction == 0)
        {
            foreach (int y in socketIndices)
                if (sprite1.texture.GetPixel(0, y) != sprite2.texture.GetPixel(width - 1, y))
                    return false;
            return true;
        }
        else if (direction == 1)
        {
            foreach (int x in socketIndices)
                if (sprite1.texture.GetPixel(x, height - 1) != sprite2.texture.GetPixel(x, 0))
                    return false;
            return true;
        }
        else if (direction == 2)
        {
            foreach (int y in socketIndices)
                if (sprite1.texture.GetPixel(width - 1, y) != sprite2.texture.GetPixel(0, y))
                    return false;
            return true;
        }
        else if (direction == 3)
        {
            foreach (int x in socketIndices)
                if (sprite1.texture.GetPixel(x, 0) != sprite2.texture.GetPixel(x, height - 1))
                    return false;
            return true;
        }
        else
        {
            throw new System.Exception("Invalid direction");
        }
    }


    public List<List<BitArray>> GetAllowedNeighbors()
    {
        return allowedNeighbors;
    }
}