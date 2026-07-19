using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace BloodyFish.UnityVoxelEngine.v2
{
    public struct TreeValues
    {
        public short trunkBlockID;
        public short leafBlockID;

        public int minHeight;
        public int maxHeight;

        public int canopyOverhang;

        public int minCanopyHeight;
        public int maxCanopyHeight;
    }


    public class Tree : ScriptableObject
    {
        public Block trunkBlock;
        public Block leafBlock;

        public int minHeight;
        public int maxHeight;

        public int canopyOverhang;

        public int minCanopyHeight;
        public int maxCanopyHeight;


        [BurstCompile]
        public static void GenerateTrunk(TreeValues treeVals, int2 chunkPos, int3 blockPos, 
            NativeArray<short> blocks,
            NativeParallelHashMap<int2, BlockBufferValues> bufferDictionary,
            NativeParallelHashMap<int2, ChunkValues> chunkDictionary, 
            ref Unity.Mathematics.Random random, out int height)
        {
            height = random.NextInt(treeVals.minHeight, treeVals.maxHeight + 1);
            for (int h = 0; h < height; h++)
            {
                int3 trunkPos = new int3(blockPos.x, blockPos.y + 1 + h, blockPos.z);
                if (Chunk.GetBlock(chunkPos, trunkPos, blocks, bufferDictionary, chunkDictionary) == 0)
                {
                    Chunk.SetBlock(treeVals.trunkBlockID, chunkPos, trunkPos, blocks, bufferDictionary, chunkDictionary);
                }
            }
        }

        [BurstCompile]
        public static void GenerateCanopy(TreeValues treeVals, int2 chunkPos, int3 blockPos,
            NativeArray<short> blocks,
            NativeParallelHashMap<int2, BlockBufferValues> bufferDictionary,
            NativeParallelHashMap<int2, ChunkValues> chunkDictionary,
            ref Unity.Mathematics.Random random) 
        {
            int canopyHeight = random.NextInt(treeVals.minCanopyHeight, treeVals.maxCanopyHeight + 1);

            int i = 0;
            for (int y = 0; y < canopyHeight; y++)
            {
                if (y == 0 || y == canopyHeight - 1) i = 1;
                else i = 0;

                for (int x = -(treeVals.canopyOverhang - i); x <= (treeVals.canopyOverhang - i); x++)
                {
                    for (int z = -(treeVals.canopyOverhang - i); z <= (treeVals.canopyOverhang - i); z++)
                    {
                        /*if (x == -canopyOverhang && z == -canopyOverhang) continue;
                        if (x == -canopyOverhang && z == canopyOverhang) continue;
                        if (x == canopyOverhang && z == canopyOverhang) continue;
                        if (x == canopyOverhang && z == -canopyOverhang) continue;*/

                        if (Mathf.Abs(x) == treeVals.canopyOverhang && Mathf.Abs(z) == treeVals.canopyOverhang) continue;

                        int m_x = x + blockPos.x;
                        int m_y = y + blockPos.y;
                        int m_z = z + blockPos.z;

                        //if (m_x < 0 || m_x >= Chunk.Width || m_z < 0 || m_z >= Chunk.Length || m_y >= Chunk.Height) continue;
                        if (y >= ChunkValues.HEIGHT) continue;

                        int3 leafPos = new int3(m_x, m_y, m_z);
                        if (Chunk.GetBlock(chunkPos, leafPos, blocks, bufferDictionary, chunkDictionary) == 0)
                        {
                            Chunk.SetBlock(treeVals.leafBlockID, chunkPos, leafPos, blocks, bufferDictionary, chunkDictionary);
                        }
                    }
                }
            }

        }
    }
}
