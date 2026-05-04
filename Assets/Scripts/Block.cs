using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using UnityEngine;

namespace BloodyFish.UnityVoxelEngine.v2
{
    [CreateAssetMenu]
    public class Block : ScriptableObject
    {
        public string blockName;

        public int blockID;
        public Color32 tint;

        public Vector2Int texCoord_front;
        public Vector2Int texCoord_back;
        public Vector2Int texCoord_left;
        public Vector2Int texCoord_right;
        public Vector2Int texCoord_top;
        public Vector2Int texCoord_bottom;

        public bool isTransparent = false;

        public bool canGrowTree;

        [HideInInspector]
        public static Block[] possibleBlocks;

        // One Biomes are added, this method won't be used any more
        [HideInInspector] public const int STONE = 1;
        [HideInInspector] public const int DIRT = 2;
        [HideInInspector] public const int GRASS = 3;
        [HideInInspector] public const int SAND = 4;
        [HideInInspector] public const int SNOW = 5;
        [HideInInspector] public const int WOOD = 6;

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // Tells the compiler to inline this method
        public static int GetFlatIndex(int x, int y, int z)
        {
            int i = x + (z * Chunk.Width) + (y * Chunk.Width * Chunk.Length);
            return i;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Block GetBlockFromID(int id)
        {
            return possibleBlocks[id - 1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int[] GetNeighboringBlocks(int x, int y, int z, int[] blocks)
        {
            int right = blocks[Block.GetFlatIndex(x + 1, y, z)];
            int left = blocks[Block.GetFlatIndex(x - 1, y, z)];
            int front = blocks[Block.GetFlatIndex(x, y, z + 1)];
            int back = blocks[Block.GetFlatIndex(x, y, z - 1)];

            int front_right = blocks[Block.GetFlatIndex(x + 1, y, z + 1)];
            int front_left = blocks[Block.GetFlatIndex(x - 1, y, z + 1)];
            int back_right = blocks[Block.GetFlatIndex(x + 1, y, z - 1)];
            int back_left = blocks[Block.GetFlatIndex(x - 1, y, z - 1)];

            int[] neighbors = { right, left, front, back, front_right, front_left, back_right, back_left };

            return neighbors;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CheckTransparentBlockPlacement(Block currentBlock, Block adjacentBlock)
        {
            // If our current block is NOT transparent, and our adjacent block IS transparent, then we return true
            // When a block is being placed, and it is next to a transparent block, we can use this method to draw the face of the opaque block that is "touching"
            // This does NOT work the other way around. If our current block is transparent and the adjacent block is opaque, we do not want to return true; we shoudn't draw a face

            if (!currentBlock.isTransparent && adjacentBlock.isTransparent) return true;
            return false;
        }

        // TO-DO:
        public static float GetSlopeOfBlock(int x, int y, int z, Chunk chunk)
        {
            return 0;
        }
    }

    public class BlockComparer : IComparer<Block>
    {
        public int Compare(Block x, Block y)
        {
            if (x.blockID > y.blockID) return 1;
            if (x.blockID < y.blockID) return -1;
            return 0;
        }
    }
}