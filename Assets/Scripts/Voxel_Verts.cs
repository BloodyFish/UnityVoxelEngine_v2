using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class Voxel_Verts
{
    [DllImport("VoxelEngine_v2", EntryPoint = "FrontFace")]
    public static extern IntPtr FrontFace(float x, float y, float z);

    [DllImport("VoxelEngine_v2", EntryPoint = "BackFace")]
    public static extern IntPtr BackFace(float x, float y, float z);

    [DllImport("VoxelEngine_v2", EntryPoint = "LeftFace")]
    public static extern IntPtr LeftFace(float x, float y, float z);

    [DllImport("VoxelEngine_v2", EntryPoint = "RightFace")]
    public static extern IntPtr RightFace(float x, float y, float z);

    [DllImport("VoxelEngine_v2", EntryPoint = "TopFace")]
    public static extern IntPtr TopFace(float x, float y, float z);

    [DllImport("VoxelEngine_v2", EntryPoint = "BottomFace")]
    public static extern IntPtr BottomFace(float x, float y, float z);

    [DllImport("VoxelEngine_v2", EntryPoint = "DeleteVerts")]
    public static extern void DeleteVerts(IntPtr arrayPointer);

    public static Vector3[] GetFrontFace(float x, float y, float z)
    {
        IntPtr ptr = FrontFace(x, y, z);
        return GetResult(ptr);
    }
    public static Vector3[] GetBackFace(float x, float y, float z)
    {
        IntPtr ptr = BackFace(x, y, z);
        return GetResult(ptr);
    }

    public static Vector3[] GetLeftFace(float x, float y, float z)
    {
        IntPtr ptr = LeftFace(x, y, z);
        return GetResult(ptr);
    }

    public static Vector3[] GetRightFace(float x, float y, float z)
    {
        IntPtr ptr = RightFace(x, y, z);
        return GetResult(ptr);
    }
    public static Vector3[] GetTopFace(float x, float y, float z)
    {
        IntPtr ptr = TopFace(x, y, z);
        return GetResult(ptr);
    }
    public static Vector3[] GetBottomFace(float x, float y, float z)
    {
        IntPtr ptr = BottomFace(x, y, z);
        return GetResult(ptr);
    }

    private static Vector3[] GetResult(IntPtr ptr)
    {
        // In C++, it's all a list of 12 floats. We will need to put each set of 3 into a four vector3s (12/3 = 4) eventually.
        float[] result = new float[12];

        Marshal.Copy(ptr, result, 0, 12);
        DeleteVerts(ptr);

        Vector3[] vector_result = new Vector3[4];

        int count = 0;
        for(int i = 0; i < 4; i++)
        {
            Vector3 vector = new Vector3();
            vector.x = result[count++];
            vector.y = result[count++];
            vector.z = result[count++];

            vector_result[i] = vector;
        }

        return vector_result;
    }
}
