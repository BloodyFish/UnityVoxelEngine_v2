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

namespace BloodyFish.UnityVoxelEngine
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
        public const short WIDTH = 16;

        [ReadOnly]
        public const short LENGTH = 16;

        [ReadOnly]
        public const short HEIGHT = 384;

        [ReadOnly]
        public const int CHUNK_SIZE = WIDTH * LENGTH * HEIGHT;

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

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CalculateIfInRenderDistance(int2 chunkPos, float2 playerPos, int renderDistance)
        {
            // Use the distance formula
            float x1 = chunkPos.x * ChunkValues.WIDTH;
            float x2 = playerPos.x;

            float y1 = chunkPos.y * ChunkValues.LENGTH;
            float y2 = playerPos.y;

            float distance = math.sqrt(math.square(x2 - x1) + math.square(y2 - y1));
            if(distance <= renderDistance)
            {
                return true;
            }

            return false;
        }

        public static ChunkValues CreateChunk(int2 pos)
        {
            ChunkValues chunkVals = new ChunkValues();
            chunkVals.blocks = new NativeArray<short>(ChunkValues.CHUNK_SIZE, Allocator.Persistent);
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


            GenerationManager.chunkObjectDictionary.TryAdd(pos, chunkObj);
            GenerationManager.chunkDictionary.TryAdd(pos, chunkVals);

            // Create Block Buffer for neighboring chunks since we can't create Persistent NativeArrays in jobs
            for(int i = 1; i < offsets.Length; i++)
            {
                int2 neighborPos = pos + offsets[i];
                if (!GenerationManager.bufferDictionary.ContainsKey(neighborPos))
                {
                    NativeArray<short> buffer = new NativeArray<short>(ChunkValues.CHUNK_SIZE, Allocator.Persistent);
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

            // We don't need to pass in chunkVals.blocks as ref since even though NativeArrays aren't passed by reference, each index is a pointer, so accessing each index is the same
            JobHandle generationJobHandle = Generation.GenTerrain(chunkVals.worldSpacePos, chunkVals.blocks, ref chunkVals.random, out GenerateChunkValuesJob generationJob);
            JobHandle paintJobHandle = TerrainPainter.Paint(chunkVals.worldSpacePos, pos, chunkVals.blocks, ref chunkVals.random, generationJobHandle, out TerrainPaintJob paintJob);
            JobHandle treeGenJobHandle = TreeGenerator.PlantTrees(chunkVals.worldSpacePos, pos, chunkVals.blocks, ref chunkVals.random, paintJobHandle, out TreeGenJob treeGenJob);

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

                GenerationManager.bufferDictionary[chunkPos].blocks.Dispose();
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
        // Otheriwse, changing individual indexes in a NativeArray can be done like a normal array, no need to pass by ref
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
                //if (!exists || currentChunk.generationPhase <= GenerationPhase.IDLE)
                if(!exists)
                {   
                    if(bufferDictionary.TryGetValue(possibleChunkPos, out BlockBufferValues bufferVals))
                    {
                        blocks = bufferVals.blocks;
                    }
                }
                else
                {
                    blocks = currentChunk.blocks;
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

        [BurstCompile]
        public static void TerminateTerrainGeneration(int2 chunkPos, ChunkValues chunk)
        {
            // Routinely check if treeGenJobHandle is complete
            if (chunk.generationPhase == GenerationPhase.IS_GEN_TERRAIN && 
                chunk.treeGenJob.blocks.IsCreated && 
                chunk.treeGenJobHandle.IsCompleted)
            {
                chunk.treeGenJobHandle.Complete();
                chunk.blocks = new NativeArray<short>(chunk.treeGenJob.blocks, Allocator.Persistent);
                chunk.treeGenJob.blocks.Dispose();

                chunk.generationPhase = GenerationPhase.DONE_GEN_TERRAIN;
                GenerationManager.chunkDictionary[chunkPos] = chunk;

                busyChunks.Enqueue(chunkPos);

                //print(chunk.blocks.Length);

                NativeList<ChunkValues> chunks = new NativeList<ChunkValues>(0, Allocator.Persistent);
                NativeArray<int2> busyChunksArray = busyChunks.ToArray(Allocator.Temp);

                // Cycle through possible neighbors and add them to "chunks"
                // NOTE: one of the offsets is int(0, 0) which includes the current chunk
                for (int i = 0; i < offsets.Length; i++)
                {
                    if (GenerationManager.chunkDictionary.TryGetValue(chunk.pos + Chunk.offsets[i], out ChunkValues neighbor) && (neighbor.blocks.Length > 0 || GenerationManager.bufferDictionary[chunk.pos + offsets[i]].blocks.Length > 0))
                    {
                        // We only want to mesh neighbors that are done generating terrain (or are at a further stage)
                        if(neighbor.generationPhase >= GenerationPhase.DONE_GEN_TERRAIN)
                        {
                            // We call MergeBlockBuffer() here so that any last minute additions to the buffer can be accounted for
                            MergeBlockBuffer(neighbor.pos, neighbor.blocks);
                            neighbor.generationPhase = GenerationPhase.IS_GEN_MESH_VALUES;
                            GenerationManager.chunkDictionary[neighbor.pos] = neighbor;
                            chunks.Add(neighbor);

                            if (!busyChunksArray.Contains(neighbor.pos))
                            {
                                busyChunks.Enqueue(neighbor.pos);
                            }
                        }
                    }
                }

                JobHandle meshGenHandle = Generation.StartMeshGen(chunks, chunk.treeGenJobHandle, out StartMeshGenJob meshGenJob);

                for(int i = 0; i < chunks.Length; i++)
                {
                    ChunkValues m_chunk = chunks[i];
                    m_chunk.meshGenJobHandle = meshGenHandle;
                    m_chunk.meshGenJob = meshGenJob;
                    chunks[i] = m_chunk;
                    GenerationManager.chunkDictionary[m_chunk.pos] = m_chunk;
                }
            }
        }

        [BurstCompile]
        public static void TerminateMeshValueGeneration(ChunkValues chunk)
        {
            if (chunk.generationPhase == GenerationPhase.IS_GEN_MESH_VALUES && 
                chunk.meshGenJobHandle.IsCompleted && 
                chunk.meshGenJob.chunkValsArray.IsCreated)
            {
                NativeList<ChunkValues> m_chunks = chunk.meshGenJob.chunkValsArray;
                for(int i = 0; i < chunk.meshGenJob.chunkValsArray.Length; i++)
                {
                    ChunkValues m_chunk = m_chunks[i];
                    m_chunk.meshGenJobHandle.Complete();

                    m_chunk.generationPhase = GenerationPhase.OPEN_FOR_MESH_GEN;
                    GenerationManager.chunkDictionary[m_chunk.pos] = m_chunk;
                }

                m_chunks.Dispose();
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Meshify(ChunkValues chunkVals)
        {
            chunkVals.generationPhase = GenerationPhase.IS_GEN_MESH;
            GenerationManager.chunkDictionary[chunkVals.pos] = chunkVals;

            if(GenerationManager.chunkObjectDictionary.TryGetValue(chunkVals.pos, out GameObject chunkObj))
            {
                Mesher.Meshify(chunkObj, chunkVals.terrainMeshValues);
                Mesher.Meshify(chunkObj.transform.GetChild(0).gameObject, chunkVals.waterMeshValues);

                chunkVals.generationPhase = GenerationPhase.IDLE;
                GenerationManager.chunkDictionary[chunkVals.pos] = chunkVals;
            }
        }

        [BurstCompile]
        public static void DisposeOfChunk(int2 chunkPos)
        {
            if(GenerationManager.chunkDictionary.TryGetValue(chunkPos, out ChunkValues chunkValues))
            {
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
            }
 
            if(GenerationManager.bufferDictionary.TryGetValue(chunkPos, out BlockBufferValues blockBufferValues))
            {
                GenerationManager.bufferDictionary.Remove(chunkPos);
                blockBufferValues.blocks.Dispose();
            }
        }
    }
}
