using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

public class Voxel_Verts
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FrontFace(List<Vector3> verts, float x, float y, float z)
    {
        // TRIS: 0, 1, 2,
        //		 2, 3, 2
        verts.Add(new Vector3(0.5f + x, -0.5f + y, 0.5f + z));
        verts.Add(new Vector3(0.5f + x, 0.5f + y, 0.5f + z));
        verts.Add(new Vector3(-0.5f + x, 0.5f + y, 0.5f + z));
        verts.Add(new Vector3(-0.5f + x, -0.5f + y, 0.5f + z));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void BackFace(List<Vector3> verts, float x, float y, float z)
    {
        // TRIS: 4, 5, 6
        //		 6, 7, 4
        verts.Add(new Vector3(-0.5f + x, -0.5f + y, -0.5f + z));
        verts.Add(new Vector3(-0.5f + x, 0.5f + y, -0.5f + z));
        verts.Add(new Vector3(0.5f + x, 0.5f + y, -0.5f + z));
        verts.Add(new Vector3(0.5f + x, -0.5f + y, -0.5f + z));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void LeftFace(List<Vector3> verts, float x, float y, float z)
    {
        // TRIS: 8, 9, 10,
        //		 10, 11, 8

        verts.Add(new Vector3(-0.5f + x, -0.5f + y, 0.5f + z));
        verts.Add(new Vector3(-0.5f + x, 0.5f + y, 0.5f + z));
        verts.Add(new Vector3(-0.5f + x, 0.5f + y, -0.5f + z));
        verts.Add(new Vector3(-0.5f + x, -0.5f + y, -0.5f + z));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void RightFace(List<Vector3> verts, float x, float y, float z)
    {
        // TRIS: 12, 13, 14,
        //		 14, 15, 12

        verts.Add(new Vector3(0.5f + x, -0.5f + y, -0.5f + z));
        verts.Add(new Vector3(0.5f + x, 0.5f + y, -0.5f + z));
        verts.Add(new Vector3(0.5f + x, 0.5f + y, 0.5f + z));
        verts.Add(new Vector3(0.5f + x, -0.5f + y, 0.5f + z));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void TopFace(List<Vector3> verts, float x, float y, float z)
    {
        // TRIS: 16, 17, 18,
        //		 18, 19, 16

        verts.Add(new Vector3(-0.5f + x, 0.5f + y, -0.5f + z));
        verts.Add(new Vector3(-0.5f + x, 0.5f + y, 0.5f + z));
        verts.Add(new Vector3(0.5f + x, 0.5f + y, 0.5f + z));
        verts.Add(new Vector3(0.5f + x, 0.5f + y, -0.5f + z));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void BottomFace(List<Vector3> verts, float x, float y, float z)
    {
        // TRIS: 20, 21, 22,
        //		 22, 23, 20

        verts.Add(new Vector3(-0.5f + x, -0.5f + y, 0.5f + z));
        verts.Add(new Vector3(-0.5f + x, -0.5f + y, -0.5f + z));
        verts.Add(new Vector3(0.5f + x, -0.5f + y, -0.5f + z));
        verts.Add(new Vector3(0.5f + x, -0.5f + y, 0.5f + z));
    }
}
