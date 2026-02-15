using UnityEngine;

[CreateAssetMenu]
public class Block : ScriptableObject
{
    public int blockID;

    public int blockID_front;
    public int blockID_back;
    public int blockID_left;
    public int blockID_right;
    public int blockID_top;
    public int blockID_bottom;

    public string blockName;
}
