using System.Collections;
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
    [HideInInspector] public const int SNOW = 4;

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
