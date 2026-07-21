using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static UnityEditor.PlayerSettings;

namespace BloodyFish.UnityVoxelEngine.v2
{
    public struct WorldGenConstants
    {
        public const int WATER_LEVEL = 63;
        public const int BEACH_HEIGHT = 3;
        public const int SCALE = 320;
    }

    public class GenerationManager : MonoBehaviour
    {
        public static GenerationManager instance;

        [SerializeField] Transform player;
        public int2 currentChunkPos;

        public NoiseParameters noise2DParam;

        public  NoiseParameters noise3DParam;
        public NoiseParameters caveNoiseParam;  // unused... for now
        public NoiseParameters temperatureNoiseParam;
        public NoiseParameters perciptationNoiseParam;

        public List<Biome> biomes;
        public static NativeArray<BiomeParameters> biomeParams;
        [SerializeField] Block defaultStoneBlock;

        [SerializeField] bool randomizeSeed;
        [SerializeField] int seedInput;
        [SerializeField] int renderDistance; // Render Distance in chunks
        private int blockRenderDistance;
        public static int seed;
        public static float3 seedOffset;

        public static Dictionary<int2, GameObject> chunkObjectDictionary = new Dictionary<int2, GameObject>();
        public static NativeParallelHashMap<int2, ChunkValues> chunkDictionary = new NativeParallelHashMap<int2, ChunkValues>(0, Allocator.Persistent);
        public static NativeParallelHashMap<int2, BlockBufferValues> bufferDictionary = new NativeParallelHashMap<int2, BlockBufferValues>(0, Allocator.Persistent);
        Queue<int2> chunkQueue = new Queue<int2>();

        Coroutine generateChunkCoroutine;

        int2[] offsets =
        {
            new int2(1, 0),
            new int2(-1, 0),
            new int2(0, 1),
            new int2(0, -1)
        };


        private static Dictionary<float, float> continentalnessToHeight = new Dictionary<float, float>()
        {
            // Negative continentalness values will map to ocean and beach heights
            {-1f, 0f },
            {-0.5f, 5f},
            {-0.2f, WorldGenConstants.WATER_LEVEL},
            {-0.1f, 65},

            // Positive continentalness will map to mountain and hilly heights
            {0f, 70f},
            {0.3f, 100},
            {1f, WorldGenConstants.SCALE }

        };

        public static NativeArray<float> continentalness = new NativeArray<float>(continentalnessToHeight.Keys.Count, Allocator.Persistent);
        public static NativeArray<float> heightFromContinentalness = new NativeArray<float>(continentalnessToHeight.Values.Count, Allocator.Persistent);


        private void OnEnable()
        {
            // Make the default Stone block have the blockID of 1
            // Since all blocks are set as 1 during the first step of generation, this makes it so that all blocks will be Stone at first
            defaultStoneBlock.blockID = 1;

            // In order to get the correct texture, we need to get all the possible blocks out of the Resources folder
            List<Block> possibleBlocks_list = Resources.LoadAll<Block>("Blocks").ToList();

            // Make sure Stone is the first block in the block list
            possibleBlocks_list.Remove(defaultStoneBlock);
            possibleBlocks_list.Insert(0, defaultStoneBlock);

            // We convert the list we just created into a NativeArray<BlockData> so that Job System can be utilized
            Block.possibleBlocks = new NativeArray<BlockData>(possibleBlocks_list.Count, Allocator.Persistent);

            for (int i = 0; i < possibleBlocks_list.Count; i++)
            {
                short ID = (short)(i + 1);
                possibleBlocks_list[i].blockID = ID;

                BlockData blockData = new BlockData
                {
                    blockID = ID,
                    tint = possibleBlocks_list[i].tint,
                    texCoord_front = possibleBlocks_list[i].texCoord_front,
                    texCoord_back = possibleBlocks_list[i].texCoord_back,
                    texCoord_left = possibleBlocks_list[i].texCoord_left,
                    texCoord_right = possibleBlocks_list[i].texCoord_right,
                    texCoord_top = possibleBlocks_list[i].texCoord_top,
                    texCoord_bottom = possibleBlocks_list[i].texCoord_bottom,
                    isTransparent = possibleBlocks_list[i].isTransparent,
                    canGrowTree = possibleBlocks_list[i].canGrowTree
                };

                Block.possibleBlocks[i] = blockData;
            }
            possibleBlocks_list.Clear();

            // Create a NativeArray of structs that represent our biomes
            biomeParams = new NativeArray<BiomeParameters>(biomes.Count, Allocator.Persistent);
            for(int i = 0; i < biomes.Count; i++)
            {
                biomeParams[i] = Biome.CreateNewBiomeParameter(biomes[i]);
            }


            if (randomizeSeed) seed = (int)(UnityEngine.Random.value * int.MaxValue);
            else seed = seedInput;

            // Populate our NativeArrays with the correct values
            for(int i = 0;  i < continentalness.Length; i++)
            {
                KeyValuePair<float, float> element = continentalnessToHeight.ElementAt(i);
                continentalness[i] = element.Key;
                heightFromContinentalness[i] = element.Value;
            }

            Unity.Mathematics.Random random = new Unity.Mathematics.Random((uint)seed);
            seedOffset = random.NextFloat3(new float3(-999999, -999999, -999999), new float3(999999, 999999, 999999));
            //print(seedOffset);
        }

        private void Awake()
        {
            instance = this;
        }

        void Start()
        {
            print("Seed: " + seed);

            Chunk.mat = Resources.Load<Material>("ChunkMaterial");
            Chunk.waterMat = Resources.Load<Material>("WaterMaterial");


            blockRenderDistance = renderDistance * ChunkValues.WIDTH;

            generateChunkCoroutine = StartCoroutine(GenerateChunk());
            StartCoroutine(MeshChunks());
        }

        private void Update()
        {

            //print(Vector2.Distance(new Vector2Int(currentChunkPos.x * ChunkValues.WIDTH, currentChunkPos.y * ChunkValues.LENGTH), player.position) <= renderDistance);
            //print(Vector2.Distance(new Vector2Int(currentChunkPos.x * ChunkValues.WIDTH, currentChunkPos.y * ChunkValues.LENGTH), new Vector2(player.position.x, player.position.z)));

            if(!currentChunkPos.Equals(UpdateCurrentChunk()))
            {
                // Disable our chunk's (and its neighbors') colliders
                Chunk.SetChunkCollsions(currentChunkPos, false);

                currentChunkPos = UpdateCurrentChunk();
                StopCoroutine(generateChunkCoroutine);
                generateChunkCoroutine = StartCoroutine(GenerateChunk());

                // Enable our new current chunk's (and its neighbors') colliders
                Chunk.SetChunkCollsions(currentChunkPos, true);
            }

            foreach (int2 chunkPos in chunkObjectDictionary.Keys.ToList())
            {
                chunkDictionary.TryGetValue(chunkPos, out ChunkValues chunk);
                if (chunk.generationPhase == GenerationPhase.IS_GEN_TERRAIN && chunk.treeGenJobHandle.IsCompleted)
                {
                    chunk.treeGenJobHandle.Complete();
                    chunk.blocks = new NativeArray<short>(chunk.treeGenJob.blocks, Allocator.Persistent);
                    chunk.treeGenJob.blocks.Dispose();

                    chunk.generationPhase = GenerationPhase.DONE_GEN_TERRAIN;
                    chunkDictionary[chunkPos] = chunk;

                    Chunk.busyChunks.Enqueue(chunkPos);

                    //print(chunk.blocks.Length);


                    NativeList<ChunkValues> chunks = new NativeList<ChunkValues>(0, Allocator.Persistent);
                    NativeArray<int2> busyChunksArray = Chunk.busyChunks.ToArray(Allocator.Temp);


                    // Cycle through possible neighbors and add them to "chunks"
                    // NOTE: one of the offsets is int(0, 0) whihc includes the current chunk
                    for (int i = 0; i < Chunk.offsets.Length; i++)
                    {
                        if (chunkDictionary.TryGetValue(chunk.pos + Chunk.offsets[i], out ChunkValues neighbor) && (neighbor.blocks.Length > 0 || bufferDictionary[chunk.pos + Chunk.offsets[i]].blocks.Length > 0))
                        {
                            // We only want to mesh neighbors that are done generating terrain (or are at a further stage)
                            if(neighbor.generationPhase >= GenerationPhase.DONE_GEN_TERRAIN)
                            {
                                // We call MergeBlockBuffer() here so that any last minute additions to the buffer can be accounted for
                                Chunk.MergeBlockBuffer(neighbor.pos, neighbor.blocks);
                                neighbor.generationPhase = GenerationPhase.IS_GEN_MESH_VALUES;
                                chunkDictionary[neighbor.pos] = neighbor;
                                chunks.Add(neighbor);

                                if (!busyChunksArray.Contains(neighbor.pos))
                                {
                                    Chunk.busyChunks.Enqueue(neighbor.pos);
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
                        chunkDictionary[m_chunk.pos] = m_chunk;
                    }

                    chunkDictionary[chunkPos] = chunk;
                }

                NativeList<ChunkValues> m_chunks = chunk.meshGenJob.chunkValsArray;
                if (chunk.generationPhase == GenerationPhase.IS_GEN_MESH_VALUES && chunk.meshGenJobHandle.IsCompleted && m_chunks.IsCreated)
                {
                    for(int i = 0; i < chunk.meshGenJob.chunkValsArray.Length; i++)
                    {
                        ChunkValues m_chunk = m_chunks[i];
                        m_chunk.meshGenJobHandle.Complete();

                        m_chunk.generationPhase = GenerationPhase.OPEN_FOR_MESH_GEN;
                        chunkDictionary[m_chunk.pos] = m_chunk;
                    }

                    m_chunks.Dispose();
                }

                

                Vector2Int chunkPosVector2 = new Vector2Int(chunkPos.x * ChunkValues.WIDTH, chunkPos.y * ChunkValues.LENGTH);
                if (Vector2.Distance(chunkPosVector2, new Vector2(player.position.x, player.position.z)) > blockRenderDistance)
                {
                    Chunk.DisposeOfChunk(chunkPos);
                }

                if (!CalculateIfInCameraFrustrum(Chunk.FindChunkCenter(chunkPos)))
                {
                    if(chunkObjectDictionary.TryGetValue(chunkPos, out GameObject chunkObj))
                    {
                        chunkObj.GetComponent<MeshRenderer>().enabled = false;
                        chunkObj.transform.GetChild(0).GetComponent<MeshRenderer>().enabled = false;
                    }

                }
                else if (chunkObjectDictionary.TryGetValue(chunkPos, out GameObject chunkObj))
                {
                    chunkObj.GetComponent<MeshRenderer>().enabled = true;
                    chunkObj.transform.GetChild(0).GetComponent<MeshRenderer>().enabled = true;
                }
            }

            for(int i = 0; i < Chunk.busyChunks.Count; i++)
            {
                int2 chunkPos = Chunk.busyChunks.Peek();
                if(Vector2.Distance(new Vector2Int(chunkPos.x * ChunkValues.WIDTH, chunkPos.y * ChunkValues.WIDTH), new Vector2(player.position.x, player.position.z)) > blockRenderDistance)
                {
                    Chunk.busyChunks.Dequeue();
                }
            }
        }

        // We must dealocate everything ourselves when we quit the game
        private void OnDisable()
        {
            print("Disposing Garbage");

            foreach (int2 chunkPos in chunkObjectDictionary.Keys.ToList())
            {
                Chunk.DisposeOfChunk(chunkPos);
            }

            chunkDictionary.Dispose();
            bufferDictionary.Dispose();

            continentalness.Dispose();
            heightFromContinentalness.Dispose();
            biomeParams.Dispose();

            Chunk.busyChunks.Dispose();
            Block.possibleBlocks.Dispose();
        }

        [BurstCompile]
        private int2 UpdateCurrentChunk()
        {
            int xPos;
            int zPos;

            if (player.position.x < 0)
            {
                xPos = (int)(player.position.x / ChunkValues.WIDTH) - 1;
            }
            else
            {
                xPos = (int)(player.position.x / ChunkValues.WIDTH);
            }

            if (player.position.z < 0)
            {
                zPos = (int)(player.position.z / ChunkValues.LENGTH) - 1;
            }
            else
            {
                zPos = (int)(player.position.z / ChunkValues.LENGTH);
            }
            
            return new int2(xPos, zPos);
        }

        private void OnDrawGizmos()
        {
            Vector3 size = new Vector3(ChunkValues.WIDTH, ChunkValues.HEIGHT, ChunkValues.LENGTH);

            Gizmos.color = Color.red;

            Gizmos.DrawWireCube(Chunk.FindChunkCenter(currentChunkPos), size);

            Gizmos.color = Color.blue;

            foreach(int2 chunkPos in chunkQueue)
            {
                Gizmos.DrawWireCube(Chunk.FindChunkCenter(chunkPos), size);
            }

            // FOR TESTING PURPOSES:
            /*Gizmos.color = Color.powderBlue;
            foreach(int2 chunkPos in Chunk.busyChunks.ToArray(Allocator.Temp))
            {
                if(chunkDictionary.TryGetValue(chunkPos, out ChunkValues chunk) && chunk.generationPhase == GenerationPhase.OPEN_FOR_MESH_GEN)
                {
                    Gizmos.color = Color.green;
                }
                Gizmos.DrawWireCube(Chunk.FindChunkCenter(chunkPos), size);
            }*/ 
        }

        IEnumerator GenerateChunk()
        {
            while (true)
            {
                if(chunkQueue.Count == 0)
                {
                    chunkQueue.Enqueue(currentChunkPos);
                }

                Vector2 playerPos = new Vector2(player.position.x, player.position.z);
                int2 chunkPos = chunkQueue.Dequeue();

                // Even though we did all these checks in GetValidMembers, we need to check again here because the player may have moved since the chunk was added to the queue
                if (!chunkDictionary.ContainsKey(chunkPos) 
                    && CalculateIfInCameraFrustrum(Chunk.FindChunkCenter(chunkPos))
                    && Vector2.Distance(new Vector2(chunkPos.x * ChunkValues.WIDTH, chunkPos.y * ChunkValues.LENGTH), playerPos) <= blockRenderDistance)
                {
                    // Create the chunk and generate it
                    ChunkValues chunk = Chunk.CreateChunk(chunkPos);
                    Chunk.Generate(chunkPos, chunk);
                }
  
                StartCoroutine(GetValidMembers(chunkPos, playerPos));

                yield return null;
            }
        }

        IEnumerator GetValidMembers(int2 chunkPos, Vector2 playerPos)
        {
            foreach(int2 offset in offsets)
            {
                int offsetAmount = 1;

                bool isValid = true;

                int2 potentialNeighborPos = chunkPos + (offset * offsetAmount); 
                
                do
                {
                    /*if(chunkDictionary.TryGetValue(potentialNeighborPos, out ChunkValues chunk) && !Chunk.BusyChunkContains(potentialNeighborPos) && chunk.generationPhase == GenerationPhase.OPEN_FOR_MESH_GEN)
                    {       
                        Chunk.Meshify(ref chunk);
                    }*/

                    if (!CalculateIfInCameraFrustrum(Chunk.FindChunkCenter(potentialNeighborPos))
                    || Vector2.Distance(new Vector2(potentialNeighborPos.x * ChunkValues.WIDTH, potentialNeighborPos.y * ChunkValues.LENGTH), playerPos) > blockRenderDistance)
                    {
                        isValid = false;
                        break;
                    }

                    offsetAmount++;
                    potentialNeighborPos = chunkPos + (offset * offsetAmount);
                    
                    yield return null;
                }
                while (chunkQueue.Contains(potentialNeighborPos) || chunkDictionary.ContainsKey(potentialNeighborPos));

                if (isValid)
                {
                    chunkQueue.Enqueue(potentialNeighborPos);
                }

                yield return null;
            }
        }

        IEnumerator MeshChunks()
        {
            while (true)
            {
                if(Chunk.busyChunks.Count > 0)
                {
                    int2 chunkPos = Chunk.busyChunks.Peek();

                    if(chunkDictionary.TryGetValue(chunkPos, out ChunkValues chunk))
                    {
                        Chunk.busyChunks.TryDequeue(out chunkPos);
                        if (CalculateIfInCameraFrustrum(Chunk.FindChunkCenter(chunkPos)) && chunk.generationPhase == GenerationPhase.OPEN_FOR_MESH_GEN)
                        {
                            Chunk.Meshify(ref chunk);
                            
                            yield return null;
                            continue;
                        }
    

                        Chunk.busyChunks.Enqueue(chunkPos);
                    }
                }
                yield return null;
            }
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetGoodBatchSize(int containerSize)
        {
            // The code for finding batch size was found on Unity Discussions
            // https://discussions.unity.com/t/how-do-i-determine-the-batch-size-to-assign-to-a-job-to-take-advantage-of-parallel-processing/934481/5
            var batchSize = containerSize / Environment.ProcessorCount;

            if (batchSize < 64)
            {
                batchSize = 64;
            }
            return batchSize;
        }

        public static bool CalculateIfInCameraFrustrum(Vector3 center)
        {
            // Code from https://docs.unity3d.com/ScriptReference/GeometryUtility.CalculateFrustumPlanes.html
            // Ordering: [0] = Left, [1] = Right, [2] = Bottom, [3] = Top, [4] = Near, [5] = Far

            // Calculate the planes from the main camera's view frustum
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);

            Bounds bounds = new Bounds(center, new Vector3(ChunkValues.WIDTH, ChunkValues.HEIGHT, ChunkValues.LENGTH));
            return GeometryUtility.TestPlanesAABB(planes, bounds);
        }
    }
}