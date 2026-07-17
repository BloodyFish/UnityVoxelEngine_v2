using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
namespace BloodyFish.UnityVoxelEngine.v2
{
    public class TerrainPainter
    {
        [BurstCompile]
        public static JobHandle Paint(int2 worldSpaceChunkPos, int2 chunkPos, ref NativeArray<short> blocks, ref Unity.Mathematics.Random random, JobHandle dependency, out TerrainPaintJob paintJob)
        {
            paintJob = new TerrainPaintJob()
            {
                blocks = blocks,
                chunkDictionary = GenerationManager.chunkDictionary,
                bufferDictionary = GenerationManager.bufferDictionary,
                random = random,
                chunkPos = chunkPos,
                biomeParams = GenerationManager.biomeParams,

                xPos = worldSpaceChunkPos.x,
                zPos = worldSpaceChunkPos.y,
                seedOffset = GenerationManager.seedOffset,

                temperatureNoiseParam = GenerationManager.instance.temperatureNoiseParam,
                precipationNoiseParam = GenerationManager.instance.perciptationNoiseParam

            };

            // NOTE: It is NOT a good idea to make the terrain paint job an IJobParallelFor. It will cause race conditions and lots of errors.
            JobHandle paintJobHandle = paintJob.ScheduleParallel(blocks.Length, GenerationManager.GetGoodBatchSize(blocks.Length), dependency);
            return paintJobHandle;
        }


        // We need to pass random by reference or else it will reset each time, making it look like no randomness is applied
        [BurstCompile]
        public static void PaintTerrain(ref Unity.Mathematics.Random random, int i, int2 chunkPos, int3 blockPos, 
            ref NativeArray<short> blocks,
            BiomeParameters biomeParam,
            NativeParallelHashMap<int2, BlockBufferValues> bufferDictionary,
            NativeParallelHashMap<int2, ChunkValues> chunkDictionary)
        {

            if (i > 0)
            {
                // We only want non-stone on the top 3 layers (with variation, of course)
                if (Chunk.GetBlock(chunkPos, new int3(blockPos.x, blockPos.y + random.NextInt(3, 6), blockPos.z), blocks, bufferDictionary, chunkDictionary) == 0)
                {
                    if (blockPos.y >= WorldGenConstants.SCALE - random.NextInt(60, 75))
                    {
                        Chunk.SetBlock(biomeParam.snowBlockID, chunkPos, blockPos, blocks, bufferDictionary, chunkDictionary);
                    }

                    //else if (y >= Generation.SCALE - random.Next(90, 100) || Block.GetSlopeOfBlock(x, y, z, chunk) >= 1f) { Block.SetBlock(Block.STONE, x, y, z, chunk); }
                    else if (blockPos.y >= WorldGenConstants.SCALE - random.NextInt(90, 100))
                    { 
                        Chunk.SetBlock(biomeParam.stoneBlockID, chunkPos, blockPos, blocks, bufferDictionary, chunkDictionary); 
                    }

                    else if (blockPos.y > (WorldGenConstants.WATER_LEVEL + WorldGenConstants.BEACH_HEIGHT) - random.NextInt(1, 3))
                    {
                        if (Chunk.GetBlock(chunkPos, new int3(blockPos.x, blockPos.y + 1, blockPos.z), blocks, bufferDictionary, chunkDictionary) == 0) 
                        {
                            Chunk.SetBlock(biomeParam.topBlockID, chunkPos, blockPos, blocks, bufferDictionary, chunkDictionary); 
                        }
                        else 
                        { 
                            Chunk.SetBlock(biomeParam.middleBlockID, chunkPos, blockPos, blocks, bufferDictionary, chunkDictionary); 
                        }
                    }

                    else
                    {
                        Chunk.SetBlock(biomeParam.beachBlockID, chunkPos, blockPos, blocks, bufferDictionary, chunkDictionary);
                    }
                }
                
            }
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FillWater(int i, int2 chunkPos, int3 blockPos,
            ref NativeArray<short> blocks,
            NativeParallelHashMap<int2, BlockBufferValues> bufferDictionary,
            NativeParallelHashMap<int2, ChunkValues> chunkDictionary)
        {
            if (blockPos.y <= WorldGenConstants.WATER_LEVEL && i == 0)
            {
               Chunk.SetBlock(-1, chunkPos, blockPos, blocks, bufferDictionary, chunkDictionary);
            }
        }
    }

    // NOTE: It is NOT a good idea to make the terrain paint job an IJobParallelFor. It will cause race conditions and lots of errors.

    [BurstCompile]
    public struct TerrainPaintJob : IJobParallelForBatch
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<short> blocks;

        [NativeDisableContainerSafetyRestriction]
        public NativeParallelHashMap<int2, BlockBufferValues> bufferDictionary;

        [ReadOnly]
        [NativeDisableContainerSafetyRestriction]
        public NativeParallelHashMap<int2, ChunkValues> chunkDictionary;

        [ReadOnly]
        public Unity.Mathematics.Random random;

        [ReadOnly]
        public int2 chunkPos;

        [ReadOnly]
        public NativeArray<BiomeParameters> biomeParams;
        
        [ReadOnly]
        public int xPos, zPos;

        [ReadOnly]
        public float3 seedOffset;

        [ReadOnly]
        public NoiseParameters temperatureNoiseParam, precipationNoiseParam;


        public void Execute(int startIndex, int count)
        {
            for(int i = startIndex; i < startIndex + count; i++)
            {
                int x = i % ChunkValues.WIDTH;
                int z = (i / ChunkValues.WIDTH) % ChunkValues.LENGTH;
                int y = (i / (ChunkValues.WIDTH * ChunkValues.LENGTH)) % ChunkValues.HEIGHT;

                float xOffset = xPos + seedOffset.x;
                float zOffset = zPos + seedOffset.z;

                float noiseX = x + xOffset;
                float noiseZ = z + zOffset;

                BiomeParameters biome = Biome.GetBiome(noiseX, y + seedOffset.y, noiseZ, temperatureNoiseParam, precipationNoiseParam, biomeParams);

                // The ID of the current, unpainted block
                short id = blocks[i];

                int3 blockPos = new int3(x, y, z);

                TerrainPainter.PaintTerrain(ref random, id, chunkPos, blockPos, ref blocks, biome, bufferDictionary, chunkDictionary);
                TerrainPainter.FillWater(id, chunkPos, blockPos, ref blocks, bufferDictionary, chunkDictionary);
            }      
        }
    }
}