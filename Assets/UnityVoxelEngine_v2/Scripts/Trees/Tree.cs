using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace BloodyFish.UnityVoxelEngine.v2
{
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
        public static void GenerateTrunk(int minHeight, int maxHeight, short trunkBlockID, int2 chunkPos, int3 blockPos, 
            NativeArray<short> blocks,
            NativeParallelHashMap<int2, BlockBufferValues> bufferDictionary,
            NativeParallelHashMap<int2, ChunkValues> chunkDictionary, 
            ref Unity.Mathematics.Random random, out int height)
        {
            height = random.NextInt(minHeight, maxHeight + 1);
            for (int h = 0; h < height; h++)
            {
                int3 trunkPos = new int3(blockPos.x, blockPos.y + 1 + h, blockPos.z);
                if (Chunk.GetBlock(chunkPos, trunkPos, blocks, bufferDictionary, chunkDictionary) == 0)
                {
                    Chunk.SetBlock(trunkBlockID, chunkPos, trunkPos, blocks, bufferDictionary, chunkDictionary);
                }
            }
        }

        [BurstCompile]
        public static void GenerateCanopy(int canopyOverhang, int minCanopyHeight, int maxCanopyHeight, short leafBlockID, int2 chunkPos, int3 blockPos,
            NativeArray<short> blocks,
            NativeParallelHashMap<int2, BlockBufferValues> bufferDictionary,
            NativeParallelHashMap<int2, ChunkValues> chunkDictionary,
            ref Unity.Mathematics.Random random) 
        {
            int canopyHeight = random.NextInt(minCanopyHeight, maxCanopyHeight + 1);

            int i = 0;
            for (int y = 0; y < canopyHeight; y++)
            {
                if (y == 0 || y == canopyHeight - 1) i = 1;
                else i = 0;

                for (int x = -(canopyOverhang - i); x <= (canopyOverhang - i); x++)
                {
                    for (int z = -(canopyOverhang - i); z <= (canopyOverhang - i); z++)
                    {
                        /*if (x == -canopyOverhang && z == -canopyOverhang) continue;
                        if (x == -canopyOverhang && z == canopyOverhang) continue;
                        if (x == canopyOverhang && z == canopyOverhang) continue;
                        if (x == canopyOverhang && z == -canopyOverhang) continue;*/

                        if (Mathf.Abs(x) == canopyOverhang && Mathf.Abs(z) == canopyOverhang) continue;

                        int m_x = x + blockPos.x;
                        int m_y = y + blockPos.y;
                        int m_z = z + blockPos.z;

                        //if (m_x < 0 || m_x >= Chunk.Width || m_z < 0 || m_z >= Chunk.Length || m_y >= Chunk.Height) continue;
                        if (y >= ChunkValues.HEIGHT) continue;

                        int3 leafPos = new int3(m_x, m_y, m_z);
                        if (Chunk.GetBlock(chunkPos, leafPos, blocks, bufferDictionary, chunkDictionary) == 0)
                        {
                            Chunk.SetBlock(leafBlockID, chunkPos, leafPos, blocks, bufferDictionary, chunkDictionary);
                        }
                    }
                }
            }

        }
    }
}
