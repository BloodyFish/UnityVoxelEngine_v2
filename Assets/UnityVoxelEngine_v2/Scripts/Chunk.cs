using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

namespace BloodyFish.UnityVoxelEngine.v2
{
    public enum GenerationPhase
    {
        IS_GEN_TERRAIN,
        DONE_GEN_TERRAIN,
        IS_GEN_MESH_VALUES,
        OPEN_FOR_MESH_GEN,
        IS_GEN_MESH,
        IDLE
    }

    public struct ChunkValues
    {
        [ReadOnly]
        public const int WIDTH = 16;

        [ReadOnly]
        public const int LENGTH = 16;

        [ReadOnly]
        public const int HEIGHT = 384;

        [NativeDisableParallelForRestriction]
        public NativeArray<short> blocks;

        public int2 pos;
        public int2 worldSpacePos;
        
        public GenerationPhase generationPhase;
        public Unity.Mathematics.Random random;

        public MeshValues terrainMeshValues;
        public MeshValues waterMeshValues;

        public short biomeID;

        public JobHandle treeGenJobHandle;
        public TreeGenJob treeGenJob;

        public JobHandle meshGenJobHandle;
        public StartMeshGenJob meshGenJob;
    }

    public struct BlockBufferValues
    {
            public NativeArray<short> blocks;
            public int2 pos;
    }

    public class Chunk
    {
        public static NativeQueue<int2> busyChunks = new NativeQueue<int2>(Allocator.Persistent);

        public static Material mat;
        public static Material waterMat;

        public readonly static int2[] offsets =
        {
                new int2(0, 0),
                new int2(1, 0),
                new int2(-1, 0),
                new int2(0, 1),
                new int2(0, -1),

                // Diagonal neighbors only for block buffer creation. We don't need to check diagonals for mesh generation 
                new int2(-1, -1),
                new int2(1, 1),
                new int2(1, -1),
                new int2(-1, 1),
        };

        // FindChunkCenter uses the relatibve position of the chunk not teh actuial one (i.e (0, 0), (1, 0))
        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 FindChunkCenter(int2 chunkPos)
        {
            return new Vector3((chunkPos.x * ChunkValues.WIDTH) + ChunkValues.WIDTH / 2, ChunkValues.HEIGHT / 2, (chunkPos.y * ChunkValues.LENGTH) + ChunkValues.LENGTH / 2);
        } 

        public static ChunkValues CreateChunk(int2 pos)
        {
            ChunkValues chunkVals = new ChunkValues();
            chunkVals.blocks = new NativeArray<short>(ChunkValues.WIDTH * ChunkValues.LENGTH * ChunkValues.HEIGHT, Allocator.Persistent);
            chunkVals.pos = pos;
            chunkVals.worldSpacePos = new int2(pos.x * ChunkValues.WIDTH, pos.y * ChunkValues.LENGTH);

            chunkVals.generationPhase = GenerationPhase.IDLE;
            
            chunkVals.terrainMeshValues = new MeshValues()
            {
                verts = new NativeList<float3>(0, Allocator.Persistent),
                tris = new NativeList<int>(0, Allocator.Persistent),
                UVs = new NativeList<float2>(0, Allocator.Persistent),
                colors = new NativeList<Color32>(0, Allocator.Persistent)
            };

            chunkVals.waterMeshValues = new MeshValues()
            {
                verts = new NativeList<float3>(0, Allocator.Persistent),
                tris = new NativeList<int>(0, Allocator.Persistent),
                UVs = new NativeList<float2>(0, Allocator.Persistent),
                colors = new NativeList<Color32>(0, Allocator.Persistent)
            };

            // Create Base Chunk
            GameObject chunkObj = new GameObject(string.Format("Chunk ({0}, {1})", chunkVals.pos.x, chunkVals.pos.y));
            chunkObj.transform.position = new Vector3(chunkVals.pos.x * ChunkValues.WIDTH, 0, chunkVals.pos.y * ChunkValues.LENGTH);
            chunkObj.AddComponent<MeshCollider>();
            chunkObj.AddComponent<MeshFilter>();
            chunkObj.AddComponent<MeshRenderer>();
            chunkObj.GetComponent<Renderer>().material = mat;

            // Create Water Chunk
            GameObject waterObj = new GameObject("Water");
            waterObj.transform.position = new Vector3(chunkVals.pos.x * ChunkValues.WIDTH, 0, chunkVals.pos.y * ChunkValues.LENGTH);
            waterObj.AddComponent<MeshFilter>();
            waterObj.AddComponent<MeshRenderer>();
            waterObj.GetComponent<Renderer>().material = waterMat;

            waterObj.transform.parent = chunkObj.transform;


            GenerationManager.chunkObjectDictionary.Add(pos, chunkObj);
            GenerationManager.chunkDictionary.Add(pos, chunkVals);

            // Create Block Buffer for neighboring chunks since we can't create Persistent NativeArrays in jobs
            for(int i = 1; i < offsets.Length; i++)
            {
                int2 neighborPos = pos + offsets[i];
                if (!GenerationManager.bufferDictionary.ContainsKey(neighborPos))
                {
                    NativeArray<short> buffer = new NativeArray<short>(ChunkValues.WIDTH * ChunkValues.LENGTH * ChunkValues.HEIGHT, Allocator.Persistent);
                    GenerationManager.bufferDictionary.TryAdd(neighborPos, new BlockBufferValues { blocks = buffer, pos = neighborPos });
                }
            }

            if (pos.Equals(GenerationManager.instance.currentChunkPos))
            {
                chunkObj.GetComponent<MeshCollider>().enabled = true;
            }
            else
            {
                chunkObj.GetComponent<MeshCollider>().enabled = false;
            }

            return chunkVals;
        }

        [BurstCompile]
        public static void Generate(int2 pos, ChunkValues chunkVals)
        {
            // ^ means XOR. Works better for creating unique numbers
            chunkVals.random = new Unity.Mathematics.Random((uint)(GenerationManager.seed ^ pos.x ^ pos.y * int.MaxValue));
            chunkVals.generationPhase = GenerationPhase.IS_GEN_TERRAIN;

            JobHandle generationJobHandle = Generation.GenTerrain(chunkVals.worldSpacePos, ref chunkVals.blocks, ref chunkVals.random, out GenerateChunkValuesJob generationJob);
            JobHandle paintJobHandle = TerrainPainter.Paint(chunkVals.worldSpacePos, pos, ref chunkVals.blocks, ref chunkVals.random, generationJobHandle, out TerrainPaintJob paintJob);
            JobHandle treeGenJobHandle = TreeGenerator.PlantTrees(chunkVals.worldSpacePos, pos, ref chunkVals.blocks, ref chunkVals.random, paintJobHandle, out TreeGenJob treeGenJob);

            // We only need to track the last job in the dependency chain
            chunkVals.treeGenJobHandle = treeGenJobHandle;
            chunkVals.treeGenJob = treeGenJob;

            GenerationManager.chunkDictionary[pos] = chunkVals;
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MergeBlockBuffer(int2 chunkPos, NativeArray<short> blocks)
        {

            if(GenerationManager.bufferDictionary.TryGetValue(chunkPos, out BlockBufferValues blockBuffer))
            {
                for (int i = 0; i < blocks.Length; i++)
                {
                    short bufferBlockID = blockBuffer.blocks[i];
                    
                    if (blocks[i] == 0 && bufferBlockID != 0)
                    {
                        blocks[i] = bufferBlockID;
                    }
                }

                GenerationManager.bufferDictionary.Remove(chunkPos);
            }
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetBlock(short blockID, int2 chunkPos, int3 blockPos, 
            NativeArray<short> blocks,
            NativeParallelHashMap<int2, BlockBufferValues> bufferDictionary,
            NativeParallelHashMap<int2, ChunkValues> chunkDictionary)
        {
            GetBlocksRelativeChunk(chunkPos, blockPos, ref blocks, bufferDictionary, chunkDictionary);
            blockPos = GetRelativeCoordinates(blockPos);

            blocks[Block.GetFlatIndex(blockPos.x, blockPos.y, blockPos.z)] = blockID;
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short GetBlock(int2 chunkPos, int3 blockPos,
            NativeArray<short> blocks,
            NativeParallelHashMap<int2, BlockBufferValues> bufferDictionary,
            NativeParallelHashMap<int2, ChunkValues> chunkDictionary)
        {
            GetBlocksRelativeChunk(chunkPos, blockPos, ref blocks, bufferDictionary, chunkDictionary);
            blockPos = GetRelativeCoordinates(blockPos);

            return blocks[Block.GetFlatIndex(blockPos.x, blockPos.y, blockPos.z)];
        }

        // The only reason we use the ref keyword here is so that we can assign using "="
        // Otheriwse, changing individual indexes in a NativeArray can be donw like a normal array, no need to pass by ref

        [BurstCompile]
        private static void GetBlocksRelativeChunk(int2 chunkPos, int3 blockPos, 
            ref NativeArray<short> blocks,
            NativeParallelHashMap<int2, BlockBufferValues> bufferDictionary,
            NativeParallelHashMap<int2, ChunkValues> chunkDictionary)
        {
            int2 possibleChunkPos = chunkPos;

            if (blockPos.x < 0) possibleChunkPos += new int2(-1,0);
            if (blockPos.x >= ChunkValues.WIDTH) possibleChunkPos += new int2(1, 0);

            if (blockPos.z < 0) possibleChunkPos += new int2(0, -1);
            if (blockPos.z >= ChunkValues.LENGTH) possibleChunkPos += new int2(0, 1);

            if (!possibleChunkPos.Equals(chunkPos))
            {
                bool exists = chunkDictionary.TryGetValue(possibleChunkPos, out ChunkValues currentChunk);

                // If currentChunk is null create block buffer and add blocks to that!
                // We also need to check if currentChunk is generating terrain. If it is, and we add blocks directly to the chunk before painting,
                // the leaves will be painted over!
                if (!exists || currentChunk.generationPhase != GenerationPhase.IDLE)
                {   
                    // We created the buffer in Chunk.CreateChunk() so we should be able to get it here
                    blocks = bufferDictionary[possibleChunkPos].blocks;
                }
            }
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int3 GetRelativeCoordinates(int3 blockPos)
        {
            int newX = blockPos.x;
            int newZ = blockPos.z;
            if (blockPos.x >= 0) newX = blockPos.x % ChunkValues.WIDTH;
            if (blockPos.z >= 0) newZ = blockPos.z % ChunkValues.LENGTH;

            if (blockPos.x < 0) newX = ChunkValues.WIDTH + blockPos.x;
            if (blockPos.z < 0) newZ = ChunkValues.LENGTH + blockPos.z;

            return new int3(newX, blockPos.y, newZ);
        }


        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetChunkCollsions(int2 chunkPos, bool isActive)
        {
            foreach (int2 offset in offsets)
            {
                if(GenerationManager.chunkObjectDictionary.TryGetValue(chunkPos + offset, out GameObject chunkObj))
                {
                    chunkObj.GetComponent<MeshCollider>().enabled = isActive;
                }
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Meshify(ref ChunkValues chunkVals)
        {
            chunkVals.generationPhase = GenerationPhase.IS_GEN_MESH;

            GameObject chunkObj = GenerationManager.chunkObjectDictionary[chunkVals.pos];

            Mesher.Meshify(chunkObj, chunkVals.terrainMeshValues);
            Mesher.Meshify(chunkObj.transform.GetChild(0).gameObject, chunkVals.waterMeshValues);

            chunkVals.generationPhase = GenerationPhase.IDLE;
        }

        public static void DisposeOfChunk(int2 chunkPos)
        {
            ChunkValues chunkValues = GenerationManager.chunkDictionary[chunkPos];
            GenerationManager.chunkDictionary.Remove(chunkPos);

            chunkValues.treeGenJobHandle.Complete();
            chunkValues.meshGenJobHandle.Complete();
            chunkValues.blocks.Dispose();

            chunkValues.terrainMeshValues.verts.Dispose();
            chunkValues.terrainMeshValues.tris.Dispose();
            chunkValues.terrainMeshValues.UVs.Dispose();
            chunkValues.terrainMeshValues.colors.Dispose();


            chunkValues.waterMeshValues.verts.Dispose();
            chunkValues.waterMeshValues.tris.Dispose();
            chunkValues.waterMeshValues.UVs.Dispose();
            chunkValues.waterMeshValues.colors.Dispose();


            GenerationManager.bufferDictionary.TryGetValue(chunkPos, out BlockBufferValues blockBufferValues);
            BlockBufferValues bufferValues = blockBufferValues;
            GenerationManager.bufferDictionary.Remove(chunkPos);
            bufferValues.blocks.Dispose();
        }
    }
}
