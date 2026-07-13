using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;

namespace BloodyFish.UnityVoxelEngine.v2
{
    public class Voxel_Tris
    {
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        // It's a lot faster to pass in a list and add to it then creating a new list and instantiating it with stuff
        public static void GenerateTris(ref NativeList<int> tris, int offset)
        {
            tris.Add(0 + offset);
            tris.Add(1 + offset);
            tris.Add(2 + offset);
            tris.Add(2 + offset);
            tris.Add(3 + offset);
            tris.Add(0 + offset);
        }
    }
}
