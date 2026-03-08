using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class Block : ScriptableObject
{
    public string blockName;

    public int blockID;

    public Vector2Int texCoord_front;
    public Vector2Int texCoord_back;
    public Vector2Int texCoord_left;
    public Vector2Int texCoord_right;
    public Vector2Int texCoord_top;
    public Vector2Int texCoord_bottom;

    [HideInInspector]
    public static Block[] possibleBlocks;

    [HideInInspector] public const int STONE = 1;
    [HideInInspector] public const int DIRT = 2;
    [HideInInspector] public const int GRASS = 3;
    [HideInInspector] public const int SAND = 4;
    [HideInInspector] public const int SNOW = 5;

    public static float GetSlopeOfBlock(int x, int y, int z, int[] blocks)
    {
        // 3D SLOPE = (y2 - y1) / sqrt((x2 - x1)^2 + (z2 - z1)^2)

        int y1 = y;
        int x1 = x;
        int z1 = z;

        int y2 = y;
        int x2 = x;
        int z2 = z;

        if (x > 0 && x <= Chunk.Width - 1) x1 = x - 1;
        else x2 = x + 1;

        if (z > 0 && z <= Chunk.Length - 1) z1 = z - 1;
        else z2 = z + 1;


        int yDelta = 15;
        if (y - yDelta < 0) yDelta = 0;

        for (int i = 0; i < yDelta; i++)
        {
            if (blocks[Chunk.GetFlatIndex(x2, y + i, z2)] == 0)
            {
                y2 = y + i;
                break;
            }
            y2 = y + i;
        }

        // Using direct multiplication is faster than Math.Pow
        float xDelta = x2 - x1;
        float zDelta = z2 - z1;

        float slope = (y2 - y1) / MathF.Sqrt(xDelta * xDelta + zDelta * zDelta);

        // Math.Abs is inneficient, so using if slope < 0, then -slope else slope is faster
        return (slope < 0) ? -slope : slope;
    }

}


public class BlockComparer: IComparer<Block>
{
    public int Compare(Block x, Block y)
    {
        if (x.blockID > y.blockID) return 1;
        if (x.blockID < y.blockID) return -1;
        return 0;
    }
}
