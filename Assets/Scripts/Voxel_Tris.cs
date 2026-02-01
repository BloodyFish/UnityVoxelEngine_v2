using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class Voxel_Tris
{
    [DllImport("VoxelEngine_v2", EntryPoint = "FrontTris")]
    public static extern IntPtr FrontTris(float x, float y, float z);

    [DllImport("VoxelEngine_v2", EntryPoint = "BackTris")]
    public static extern IntPtr BackTris(float x, float y, float z);

    [DllImport("VoxelEngine_v2", EntryPoint = "LeftTris")]
    public static extern IntPtr LeftTris(float x, float y, float z);

    [DllImport("VoxelEngine_v2", EntryPoint = "RightTris")]
    public static extern IntPtr RightTris(float x, float y, float z);

    [DllImport("VoxelEngine_v2", EntryPoint = "TopTris")]
    public static extern IntPtr TopTris(float x, float y, float z);

    [DllImport("VoxelEngine_v2", EntryPoint = "BottomTris")]
    public static extern IntPtr BottomTris(float x, float y, float z);

    [DllImport("VoxelEngine_v2", EntryPoint = "DeleteTris")]
    public static extern void DeleteVerts(IntPtr arrayPointer);

    public static int[] GetFrontTris(float x, float y, float z)
    {
        IntPtr ptr = FrontTris(x, y, z);
        return GetResult(ptr);
    }
    public static int[] GetBackTris(float x, float y, float z)
    {
        IntPtr ptr = BackTris(x, y, z);
        return GetResult(ptr);
    }

    public static int[] GetLeftTris(float x, float y, float z)
    {
        IntPtr ptr = LeftTris(x, y, z);
        return GetResult(ptr);
    }

    public static int[] GetRightTris(float x, float y, float z)
    {
        IntPtr ptr = RightTris(x, y, z);
        return GetResult(ptr);
    }
    public static int[] GetTopTris(float x, float y, float z)
    {
        IntPtr ptr = TopTris(x, y, z);
        return GetResult(ptr);
    }
    public static int[] GetBottomTris(float x, float y, float z)
    {
        IntPtr ptr = BottomTris(x, y, z);
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
