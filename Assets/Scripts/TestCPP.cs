using System.Runtime.InteropServices;
using UnityEngine;

public class TestCPP : MonoBehaviour
{
    [DllImport("VoxelEngine_v2", EntryPoint = "PrintRandomNum")]
    public static extern int PrintRandomNum();

    void Start()
    {
        print(PrintRandomNum());
    }
}
