using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace BloodyFish.UnityVoxelEngine.v2
{
    public class TreeGenerator
    {

        [BurstCompile]
        public static JobHandle PlantTrees(int2 worldSpaceChunkPos, int2 chunkPos, ref NativeArray<short> blocks, ref Unity.Mathematics.Random random, JobHandle dependency, out TreeGenJob treeGenJob)
        {
            treeGenJob = new TreeGenJob()
            {
                minHeight = GenerationManager.defaultTree.minHeight,
                maxHeight = GenerationManager.defaultTree.maxHeight,
                canopyOverhang = GenerationManager.defaultTree.canopyOverhang,
                minCanopyHeight = GenerationManager.defaultTree.minCanopyHeight,
                maxCanopyHeight = GenerationManager.defaultTree.maxCanopyHeight,
                blocks = blocks,
                possibleBlocks = Block.possibleBlocks,
                chunkDictionary = GenerationManager.chunkDictionary,
                bufferDictionary = GenerationManager.bufferDictionary,
                random = random,
                worldSpaceChunkPos = worldSpaceChunkPos,
                chunkPos = chunkPos,
                seedOffset = GenerationManager.seedOffset,
                biomeParams = GenerationManager.biomeParams,
                temperatureNoiseParam = GenerationManager.instance.temperatureNoiseParam,
                precipationNoiseParam = GenerationManager.instance.perciptationNoiseParam
            };

            //Debug.Log(GenerationManager.chunkDictionary[chunkPos].biomeID);

            int size = ChunkValues.WIDTH * ChunkValues.LENGTH * ChunkValues.HEIGHT;
            JobHandle treeJobHandle = treeGenJob.ScheduleParallel(size, GenerationManager.GetGoodBatchSize(size), dependency);
            return treeJobHandle;
        }

    }

    [BurstCompile]
    public struct TreeGenJob: IJobParallelForBatch
    {
        public int minHeight;
        public int maxHeight;

        /*public short trunkBlockID;
        public short leafBlockID;*/

        public int canopyOverhang;
        public int minCanopyHeight;
        public int maxCanopyHeight;

        [NativeDisableParallelForRestriction]
        public NativeArray<short> blocks;

        [ReadOnly]
        public NativeArray<BlockData> possibleBlocks;

        [NativeDisableContainerSafetyRestriction]
        public NativeParallelHashMap<int2, BlockBufferValues> bufferDictionary;

        [ReadOnly]
        [NativeDisableContainerSafetyRestriction]
        public NativeParallelHashMap<int2, ChunkValues> chunkDictionary;

        public Unity.Mathematics.Random random;

        [ReadOnly]
        public int2 worldSpaceChunkPos;

        [ReadOnly]
        public int2 chunkPos;

        [ReadOnly]
        public float3 seedOffset;

        [ReadOnly]
        public NativeArray<BiomeParameters> biomeParams;

        [ReadOnly]
        public NoiseParameters temperatureNoiseParam, precipationNoiseParam;



        public void Execute(int startIndex, int count)
        {
            for(int i = startIndex; i < startIndex + count; i++ ) 
            {
                // We can check every other block
                if (i % 2 == 0)
                {
                    int x = i % ChunkValues.WIDTH;
                    int z = (i / ChunkValues.WIDTH) % ChunkValues.LENGTH;
                    int y = (i / (ChunkValues.WIDTH * ChunkValues.LENGTH)) % ChunkValues.HEIGHT;

                    if (y < ChunkValues.HEIGHT - 1 && y > WorldGenConstants.WATER_LEVEL)
                    {
                        int3 blockPos = new int3(x, y, z);
                        int currentBlockID = Chunk.GetBlock(chunkPos, blockPos, blocks, bufferDictionary, chunkDictionary);

                        if (currentBlockID > 0)
                        {
                            BlockData currentBlock = possibleBlocks[currentBlockID - 1];

                            // CalculateBiome
                            short biomeID = Biome.GetBiome(worldSpaceChunkPos, seedOffset, x, y, z, temperatureNoiseParam, precipationNoiseParam, biomeParams);


                            if (currentBlock.canGrowTree && random.NextInt(0, biomeParams[biomeID].treeDensity) == 1 && !Block.GetNeighboringBlocks(x, y + 1, z, blocks).Contains(biomeParams[biomeID].treeStemBlockID))
                            {
                                // While we are here, we might as well make it so that trees growing on grass blocks replace that block with dirt
                                if (currentBlockID == biomeParams[biomeID].topBlockID) Chunk.SetBlock(biomeParams[biomeID].middleBlockID, chunkPos, blockPos, blocks, bufferDictionary, chunkDictionary);

                                Tree.GenerateTrunk(minHeight, maxHeight, biomeParams[biomeID].treeStemBlockID, chunkPos, blockPos, blocks, bufferDictionary, chunkDictionary, ref random, out int height);
                                Tree.GenerateCanopy(canopyOverhang, minCanopyHeight, maxCanopyHeight, biomeParams[biomeID].treeLeafBlockID, chunkPos, new int3(x, y + height, z), blocks, bufferDictionary, chunkDictionary, ref random);
                            }
                        }
                    }
                }
            }
        }
    }
}
