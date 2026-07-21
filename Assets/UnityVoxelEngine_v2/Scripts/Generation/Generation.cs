using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.InputSystem.Interactions;


namespace BloodyFish.UnityVoxelEngine.v2
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
        public static JobHandle GenTerrain(int2 worldSpaceChunkPos, ref NativeArray<short> blocks, ref Unity.Mathematics.Random random, out GenerateChunkValuesJob generationJob)
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
        public NativeArray<short> blocks;

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


        public void Execute(int startIndex, int count )
        {
            for(int index = startIndex; index < startIndex + count; index++)
            {
                int x = index % ChunkValues.WIDTH;
                int z = index / ChunkValues.WIDTH;

                float noiseVal_2D = NoiseGen.GetNoise(worldSpaceChunkPos, seedOffset, x, z, noise2D);

                // Get the length of our continentalness to height spline
                int height = GetTerrainHeight(continentalness.Length, continentalness, heightFromContinentalness, noiseVal_2D);

                for (int y = 0; y < height; y++)
                {
                    float noiseVal_3D = NoiseGen.GetNoise(worldSpaceChunkPos, seedOffset, x, y, z, noise3D);
                    //float m_caveNoise = NoiseGen.GetNoise(noiseX, y + seedOffset.y, noiseZ, caveNoise);

                    if (noiseVal_3D > 0f)
                    {
                        int i = Block.GetFlatIndex(x, y, z);
                        blocks[i] = 1;
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
        [NativeDisableContainerSafetyRestriction]
        public NativeList<ChunkValues> chunkValsArray;

        [ReadOnly]
        [NativeDisableContainerSafetyRestriction]
        public NativeParallelHashMap<int2, ChunkValues> chunkDictionary;

        [ReadOnly]
        [NativeDisableParallelForRestriction]
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
