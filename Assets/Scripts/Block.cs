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
}
