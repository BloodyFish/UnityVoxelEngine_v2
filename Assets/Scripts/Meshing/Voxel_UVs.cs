using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using UnityEngine;

namespace BloodyFish.UnityVoxelEngine.v2
{
    public class Voxel_UVs
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        // It's a lot faster to pass in a list and add to it then creating a new list and instantiating it with stuff
        public static void GetUVs(List<Vector2> uvs, float x, float y, float size)
        {
            // The coordinates of our texture atlas are as follows:
            // TOP LEFT = (0, 1)
            // TOP RIGHT = (1, 1)
            // BOTTOM LEFT = (0, 0)
            // BOTTOM RIGHT = (1, 0)

            float textureStep = 1 / size;

            float x0 = textureStep * x;
            float y0 = textureStep * y;
            float x1 = textureStep * (x + 1);
            float y1 = textureStep * (y + 1);

            // BOTTOM LEFT
            uvs.Add(new Vector2(x0, y0));

            // TOP LEFT
            uvs.Add(new Vector2(x0, y1));

            // TOP RIGHT
            uvs.Add(new Vector2(x1, y1));

            // BOTTOM RIGHT
            uvs.Add(new Vector2(x1, y0));
        }
    }
}
