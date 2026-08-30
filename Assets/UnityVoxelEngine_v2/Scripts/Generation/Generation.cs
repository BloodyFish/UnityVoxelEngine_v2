using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;


namespace BloodyFish.UnityVoxelEngine
{
    public class Generation
    {    
        [BurstCompile]
        public static JobHandle StartMeshGen(NativeList<ChunkValues> chunkValsArray, JobHandle dependency, out StartMeshGenJob meshGenJob)
        {
            meshGenJob = new StartMeshGenJob
            {
                chunkValsArray = chunkValsArray,
                chunkDictionary = GenerationManager.chunkDictionary,

                possibleBlocks = Block.possibleBlocks
            };

            JobHandle meshGenHandle = meshGenJob.Schedule(chunkValsArray.Length, GenerationManager.GetGoodBatchSize(chunkValsArray.Length), dependency);
            return meshGenHandle;
        }

        [BurstCompile]
        public static JobHandle GenTerrain(int2 worldSpaceChunkPos, NativeArray<sbyte> blocks, ref Unity.Mathematics.Random random, out GenerateChunkValuesJob generationJob)
        {
            generationJob = new GenerateChunkValuesJob()
            {
                blocks = blocks,
                continentalness = GenerationManager.continentalness,
                heightFromContinentalness = GenerationManager.heightFromContinentalness,

                worldSpaceChunkPos = worldSpaceChunkPos,

                seedOffset = GenerationManager.seedOffset,
                noise2D = GenerationManager.instance.noise2DParam,
                noise3D = GenerationManager.instance.noise3DParam,
                cellularNoiseFrequency = GenerationManager.instance.cellularNoiseFrequency,
                caveNoise = GenerationManager.instance.caveNoiseParam
            };

            int size = ChunkValues.WIDTH * ChunkValues.LENGTH;
            JobHandle generationJobHandle = generationJob.ScheduleParallel(size, GenerationManager.GetGoodBatchSize(size));
            return generationJobHandle;
        }
    }

    // All the generation magic happens here!
    [BurstCompile]
    public struct GenerateChunkValuesJob : IJobParallelForBatch
    {
        [NativeDisableParallelForRestriction]
        public NativeArray<sbyte> blocks;

        [ReadOnly]
        public NativeArray<float> continentalness;

        [ReadOnly]
        public NativeArray<float> heightFromContinentalness;

        [ReadOnly]
        public int2 worldSpaceChunkPos;

        [ReadOnly]
        public float3 seedOffset;

        [ReadOnly]
        public NoiseParameters noise2D, noise3D, caveNoise;

        [ReadOnly]
        public float cellularNoiseFrequency;


        public void Execute(int startIndex, int count )
        {
            float height = 0;
            float future_height = 0;

            for(int index = startIndex; index < startIndex + count; index++)
            {
                int x = index % ChunkValues.WIDTH;
                int z = index / ChunkValues.LENGTH;

                // Only calculate the 2D noise every other index
                // This is a performance optimization
                if(index % 2 == 0)
                {
                    float noiseVal_2D = NoiseGen.GetNoise(worldSpaceChunkPos, seedOffset, x, z, noise2D);
                    float future_noiseVal_2D = NoiseGen.GetNoise(worldSpaceChunkPos, seedOffset, x + 2, z, noise2D);

                    // Get the length of our continentalness to height spline
                    height = GetTerrainHeight(continentalness.Length, continentalness, heightFromContinentalness, noiseVal_2D);
                    future_height = GetTerrainHeight(continentalness.Length, continentalness, heightFromContinentalness, future_noiseVal_2D);
                }
                else
                {
                    height = math.lerp(height, future_height, 0.5f);
                }

                float2 cellularNoise = NoiseGen.GetCellularNoise(worldSpaceChunkPos, seedOffset, x, z, cellularNoiseFrequency);
                float riverNoise = cellularNoise.y - cellularNoise.x;

                float noiseVal_3D = 0;
                float future_noiseVal_3D = 0;
                for (int y = 0; y < height; y++)
                {

                    // This is a performance optimization
                    if(y % 2 == 0)
                    {
                        noiseVal_3D = NoiseGen.GetNoise(worldSpaceChunkPos, seedOffset, x, y, z, noise3D);
                        future_noiseVal_3D = NoiseGen.GetNoise(worldSpaceChunkPos, seedOffset, x, y + 2, z, noise3D);

                        //float m_caveNoise = NoiseGen.GetNoise(noiseX, y + seedOffset.y, noiseZ, caveNoise);
                    }
                    else
                    {
                        noiseVal_3D = math.lerp(noiseVal_3D, future_noiseVal_3D, 0.5f);
                    }

                    if (noiseVal_3D > 0f)
                    {
                        int i = Block.GetFlatIndex(x, y, z);
                        blocks[i] = 1;

                        float riverNoiseThreshold = 0.02f;
                        if(riverNoise < riverNoiseThreshold && y > WorldGenConstants.WATER_LEVEL - 1)
                        {
                            // This adds a slope to the river bed
                            if(y > height - ((riverNoiseThreshold * 100) - (riverNoise * 100)) - 1)
                            {
                                blocks[i] = -1;
                            }
                        }

                    }
                }
            }     
        }


        // This method MUST be inside the job or else Burst complains
        // This is a seperate method for readability, no other reason
        private static int GetTerrainHeight(int splineLength, NativeArray<float> continentalness,  NativeArray<float> heightFromContinentalness, float noiseVal_2D)
        {   
            int h = 0;
            for (int i = 0; i < splineLength - 1; i++)
            {
                float x1 = continentalness[i];
                float x2 = continentalness[i + 1];

                if (noiseVal_2D >= x1 && noiseVal_2D <= x2)
                {
                    // Create equation for this certain section of the spline:
                    float y1 = heightFromContinentalness[i];
                    float y2 = heightFromContinentalness[i + 1];

                    // y = mx + b
                    // b = y - mx

                    float slope = (y2 - y1) / (x2 - x1);
                    float b = y1 - slope * x1;

                    h = (int)(slope * noiseVal_2D + b);
                    break;
                }
            }

            return h;
        }
    }

    [BurstCompile]
    public struct StartMeshGenJob : IJobParallelFor
    {
        [ReadOnly]
        [NativeDisableParallelForRestriction]
        [NativeDisableContainerSafetyRestriction]
        public NativeList<ChunkValues> chunkValsArray;

        [ReadOnly]
        [NativeDisableParallelForRestriction]
        [NativeDisableContainerSafetyRestriction]
        public NativeParallelHashMap<int2, ChunkValues> chunkDictionary;

        [ReadOnly]
        [NativeDisableParallelForRestriction]
        [NativeDisableContainerSafetyRestriction]
        public NativeArray<BlockData> possibleBlocks;


        public void Execute(int i)
        {
            ChunkValues chunkVals = chunkValsArray[i];

            // We don't need to pass chunkVals by ref becuase the contents (which Mesher changes) are pointers
            // We will need to use ref when we pass in the verts, uvs, tris, etc. to add to them
            Mesher.GenerateMeshValues(chunkVals, chunkDictionary, possibleBlocks);
            Mesher.GenerateMeshValuesWater(chunkVals, chunkDictionary);
        }
    }
}
