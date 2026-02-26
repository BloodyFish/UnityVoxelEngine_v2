using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using UnityEngine;

[BurstCompile]
public class Voxel_Tris
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GenerateTris(List<int> tris, int offset)
    {
        tris.Add(0 + offset);
        tris.Add(1 + offset);
        tris.Add(2 + offset);
        tris.Add(2 + offset);
        tris.Add(3 + offset);
        tris.Add(0 + offset);
    }
}
