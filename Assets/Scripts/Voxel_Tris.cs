using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class Voxel_Tris
{
    [DllImport("VoxelEngine_v2", EntryPoint = "CreateTris")]
    public static extern IntPtr CreateTris(int offset);


    [DllImport("VoxelEngine_v2", EntryPoint = "DeleteTris")]
    public static extern void DeleteVerts(IntPtr arrayPointer);

    public static int[] GenerateTris(int offset)
    {
        IntPtr ptr = CreateTris(offset);
        return GetResult(ptr);
    }
    
    private static int[] GetResult(IntPtr ptr)
    {
        // In C++, it's all a list of 12 floats. We will need to put each set of 3 into a four floats (12/3 = 4) eventually.
        int[] result = new int[6];

        Marshal.Copy(ptr, result, 0, 6);
        DeleteVerts(ptr);

        return result;
    }
}
