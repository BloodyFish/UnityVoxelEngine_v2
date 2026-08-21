using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;

namespace BloodyFish.UnityVoxelEngine
{
    public struct BlockData
    {
        public short blockID;
        public Color tint;

        public Vector2Int texCoord_front;
        public Vector2Int texCoord_back;
        public Vector2Int texCoord_left;
        public Vector2Int texCoord_right;
        public Vector2Int texCoord_top;
        public Vector2Int texCoord_bottom;

        public bool isTransparent;

        public bool canGrowTree;
    }

    [CreateAssetMenu]
    public class Block : ScriptableObject
    {
        public string blockName;

        [HideInInspector] public sbyte blockID;
        public Color32 tint;

        public Vector2Int texCoord_front;
        public Vector2Int texCoord_back;
        public Vector2Int texCoord_left;
        public Vector2Int texCoord_right;
        public Vector2Int texCoord_top;
        public Vector2Int texCoord_bottom;

        public bool isTransparent = false;

        public bool canGrowTree;

        // Use a struct to hold a representation of each block so that it works with Job System
        [HideInInspector] public static NativeArray<BlockData> possibleBlocks;

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Tells the compiler to inline this method
        public static int GetFlatIndex(int x, int y, int z)
        {
            int i = x + (z * ChunkValues.WIDTH) + (y * ChunkValues.WIDTH * ChunkValues.LENGTH);
            return i;
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static NativeArray<sbyte> GetNeighboringBlocks(int x, int y, int z, NativeArray<sbyte> blocks)
        {
            sbyte right = blocks[Block.GetFlatIndex(x + 1, y, z)];
            sbyte left = blocks[Block.GetFlatIndex(x - 1, y, z)];
            sbyte front = blocks[Block.GetFlatIndex(x, y, z + 1)];
            sbyte back = blocks[Block.GetFlatIndex(x, y, z - 1)];

            /*sbyte front_right = blocks[Block.GetFlatIndex(x + 1, y, z + 1)];
            sbyte front_left = blocks[Block.GetFlatIndex(x - 1, y, z + 1)];
            sbyte back_right = blocks[Block.GetFlatIndex(x + 1, y, z - 1)];
            sbyte back_left = blocks[Block.GetFlatIndex(x - 1, y, z - 1)];*/

            NativeArray<sbyte> neighbors = new NativeArray<sbyte>(4, Allocator.Temp);

            neighbors[0] = right;
            neighbors[1] = left;
            neighbors[2] = front;
            neighbors[3] = back;
            /*neighbors[4] = front_right;
            neighbors[5] = front_left;
            neighbors[6] = back_right;
            neighbors[7] = back_left;*/

            return neighbors;
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CheckTransparentBlockPlacement(BlockData currentBlock, BlockData adjacentBlock)
        {
            // If our current block is NOT transparent, and our adjacent block IS transparent, then we return true
            // When a block is being placed, and it is next to a transparent block, we can use this method to draw the face of the opaque block that is "touching"
            // This does NOT work the other way around. If our current block is transparent and the adjacent block is opaque, we do not want to return true; we shoudn't draw a face

            if (!currentBlock.isTransparent && adjacentBlock.isTransparent) return true;
            return false;
        }
    }
}