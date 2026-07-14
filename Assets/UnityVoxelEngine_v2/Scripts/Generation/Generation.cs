using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;


namespace BloodyFish.UnityVoxelEngine.v2
{
    public class Generation
    {   
        [BurstCompile]
        public static JobHandle StartMeshGen(ref NativeList<ChunkValues> chunkValsArray, JobHandle dependency)
        {
            StartMeshGenJob meshGenJob = new StartMeshGenJob
            {
                chunkValsArray = chunkValsArray,
                chunkDictionary = GenerationManager.chunkDictionary,

                possibleBlocks = Block.possibleBlocks
            };

            JobHandle meshGenHandle = meshGenJob.Schedule(chunkValsArray.Length, GenerationManager.GetGoodBatchSize(chunkValsArray.Length), dependency);

            for(int i = 0; i < chunkValsArray.Length; i++)
            {
                ChunkValues chunkVals = chunkValsArray[i];
                chunkVals.generationPhase = GenerationPhase.OPEN_FOR_MESH_GEN;

                GenerationManager.chunkDictionary[chunkVals.pos] = chunkVals;
            }

            return meshGenHandle;
        }

        [BurstCompile]
        public static JobHandle GenTerrain(int2 worldSpaceChunkPos, ref NativeArray<short> blocks, ref Unity.Mathematics.Random random, out GenerateChunkValuesJob generationJob)
        {
            generationJob = new GenerateChunkValuesJob()
            {
                blocks = blocks,
                continentalness = GenerationManager.continentalness,
                heightFromContinentalness = GenerationManager.heightFromContinentalness,
                yOffset = 0,
                xPos = worldSpaceChunkPos.x,
                zPos = worldSpaceChunkPos.y,

                seedOffset = GenerationManager.seedOffset
            };

            JobHandle generationJobHandle = generationJob.Schedule();
            return generationJobHandle;
        }
    }

    // All the generation magic happens here!
    [BurstCompile]
    public struct GenerateChunkValuesJob : IJob
    {
        public NativeArray<short> blocks;

        [ReadOnly]
        public NativeArray<float> continentalness;

        [ReadOnly]
        public NativeArray<float> heightFromContinentalness;

        [ReadOnly]
        public int xPos, zPos, yOffset;

        [ReadOnly]
        public float3 seedOffset;

        public void Execute()
        {
            int splineLength = continentalness.Length;


            for (int index = 0; index < ChunkValues.WIDTH * ChunkValues.LENGTH; index++)
            {
                int x = index % ChunkValues.WIDTH;
                int z = index / ChunkValues.WIDTH;

                float noiseX = x + xPos + seedOffset.x;
                float noiseZ = z + zPos + seedOffset.z;

                float noiseVal_2D = 0;
                float frequency = 0.5f;
                float lacunarity = 3.5f;
                float octaves = 5;
                float amplitude = 1;
                float gain = 0.25f;

                float maxAmplitude = 0;
                for (int i = 0; i < octaves; i++)
                {
                    noiseVal_2D += Unity.Mathematics.noise.snoise(new float2(noiseX, noiseZ) * (frequency / 1000)) * amplitude;
                    maxAmplitude += amplitude;
                    frequency *= lacunarity;
                    amplitude *= gain;
                }
                noiseVal_2D /= maxAmplitude;

                // Get the length of our continentalness to height spline

                float h = 0;
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

                        h = slope * noiseVal_2D + b;
                        break;
                    }
                }

                for (int y = 0; y < h + yOffset; y++)
                {
                    int i = Block.GetFlatIndex(x, y, z);
                    blocks[i] = 1;
                }
            }
        }
    }

    [BurstCompile]
    struct StartMeshGenJob : IJobParallelFor
    {
        [NativeDisableContainerSafetyRestriction]
        public NativeList<ChunkValues> chunkValsArray;

        [ReadOnly]
        [NativeDisableContainerSafetyRestriction]
        public NativeParallelHashMap<int2, ChunkValues> chunkDictionary;

        [ReadOnly]
        public NativeArray<BlockData> possibleBlocks; 


        public void Execute(int i)
        {
            ChunkValues chunkVals = chunkValsArray[i];

            Mesher.GenerateMeshValues(ref chunkVals, chunkDictionary, possibleBlocks);
            Mesher.GenerateMeshValuesWater(ref chunkVals, chunkDictionary);

            chunkValsArray[i] = chunkVals;
        }
    }
}
