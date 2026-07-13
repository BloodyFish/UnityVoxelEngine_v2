using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

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

        [SerializeField] NoiseParameters noise2DParam;
        [SerializeField] NoiseParameters noise3DParam; // unused... for now
        [SerializeField] NoiseParameters caveNoiseParam;

        [SerializeField] bool randomizeSeed;
        [SerializeField] int seedInput;
        [SerializeField] int renderDistance;
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


        // FOR TESTING
        [ReadOnly]
        public static Tree defaultTree;

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
            // In order to get the correct texture, we need to get all the possible blocks out of the Resources folder
            // The blocks in possibleBlocks should be sorted by their blockID.
            // So a block with blockID 1 should be at index 0, a block with blockID 2 should be at index 1, and so on.
            List<Block> possibleBlocks_list = Resources.LoadAll<Block>("Blocks").ToList();
            possibleBlocks_list.Sort(new BlockComparer());

            // We convert the list we just created into a NativeArray<BlockData> so that Job System can be utilized
            Block.possibleBlocks = new NativeArray<BlockData>(possibleBlocks_list.Count, Allocator.Persistent);
            for (int i = 0; i < possibleBlocks_list.Count; i++)
            {
                BlockData blockData = new BlockData
                {
                    blockID = possibleBlocks_list[i].blockID,
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
            print(seedOffset);
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
            defaultTree = Resources.Load<Tree>("Trees/BasicTree");


            renderDistance = renderDistance * ChunkValues.WIDTH;

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
                Vector2Int chunkPosVector2 = new Vector2Int(chunkPos.x * ChunkValues.WIDTH, chunkPos.y * ChunkValues.LENGTH);
                if (Vector2.Distance(chunkPosVector2, new Vector2(player.position.x, player.position.z)) > renderDistance)
                {
                    Destroy(chunkObjectDictionary[chunkPos]);
                    chunkObjectDictionary.Remove(chunkPos);

                    ChunkValues chunkValues = chunkDictionary[chunkPos];
                    chunkDictionary.Remove(chunkPos);
                    chunkValues.blocks.Dispose();
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
                if(Vector2.Distance(new Vector2Int(chunkPos.x * ChunkValues.WIDTH, chunkPos.y * ChunkValues.WIDTH), new Vector2(player.position.x, player.position.z)) > renderDistance)
                {
                    Chunk.busyChunks.Dequeue();
                }
            }
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
            Gizmos.color = Color.red;

            Gizmos.DrawWireCube(Chunk.FindChunkCenter(currentChunkPos), new Vector3(ChunkValues.WIDTH, ChunkValues.HEIGHT, ChunkValues.LENGTH));

            Gizmos.color = Color.blue;

            foreach(int2 chunkPos in chunkQueue)
            {
                Gizmos.DrawWireCube(Chunk.FindChunkCenter(chunkPos), new Vector3(ChunkValues.WIDTH, ChunkValues.HEIGHT, ChunkValues.LENGTH));

            }
        }

        IEnumerator GenerateChunk()
        {
            chunkQueue.Clear();

            // Clearing Chunk.busyChunks is important becuase when moving the player large distances, whats in this Queue takes priority
            //Chunk.busyChunks.Clear();

            //chunkQueue.Enqueue(currentChunkPos);

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
                    && Vector2.Distance(new Vector2(chunkPos.x * ChunkValues.WIDTH, chunkPos.y * ChunkValues.LENGTH), playerPos) <= renderDistance)
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
                    || Vector2.Distance(new Vector2(potentialNeighborPos.x * ChunkValues.WIDTH, potentialNeighborPos.y * ChunkValues.LENGTH), playerPos) > renderDistance)
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
                        if (CalculateIfInCameraFrustrum(Chunk.FindChunkCenter(chunkPos)) && chunk.generationPhase == GenerationPhase.OPEN_FOR_MESH_GEN)
                        {
                            Chunk.busyChunks.TryDequeue(out chunkPos);
                            Chunk.Meshify(ref chunk);
                        }
                        else
                        {
                            Chunk.busyChunks.TryDequeue(out chunkPos);
                            Chunk.busyChunks.Enqueue(chunkPos);
                        }
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