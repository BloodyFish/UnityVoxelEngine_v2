using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;

namespace BloodyFish.UnityVoxelEngine.v2
{
    public class Chunk
    {
        // Whenever we add or remove from this list within a thread, we need to use a lock!
        public static List<Chunk> busyChunks = new List<Chunk>();

        public readonly int[] blocks;

        public Vector3Int pos;

        public static int Width { get; set; }
        public static int Length { get; set; }
        public static int Height { get; set; }


        public GameObject chunkObj;
        public GameObject waterObj;

        public static readonly Material mat = Resources.Load<Material>("ChunkMaterial");
        public static readonly Material waterMat = Resources.Load<Material>("WaterMaterial");

        System.Random random;

        public enum GenerationPhase { IS_GENERATING_TERRAIN,
                                      DONE_GENERATING_TERRAIN,
                                      OPEN_FOR_MESH_GEN, 
                                      IS_GEN_MESH,
                                      IDLE
                                    }

        public GenerationPhase generationPhase = GenerationPhase.IDLE;

        public readonly object _lock = new object();

        public Mesher mesher;
        public MeshValues terrainMeshValues;
        public MeshValues waterMeshValues;

        // Constructor
        public Chunk(Vector3Int pos)
        {
            blocks = new int[Width * Length * Height];
            this.pos = pos;

            random = new System.Random(Generation.seed);

            mesher = new Mesher(this);
            terrainMeshValues = new MeshValues()
            {
                verts = new List<Vector3>(),
                tris = new List<int>(),
                UVs = new List<Vector2>(),
                colors = new List<Color32>()
            };

            waterMeshValues = new MeshValues()
            {
                verts = new List<Vector3>(),
                tris = new List<int>(),
                UVs = new List<Vector2>(),
                colors = new List<Color32>()
            };

            CreateObj(ref chunkObj, null, "Chunk", mat);
            CreateObj(ref waterObj, chunkObj.transform, "Water", waterMat);

            Generation.chunkDictionary.TryAdd(pos, this);
        }
        private void CreateObj(ref GameObject obj, Transform parent, string name, Material mat)
        {
            obj = new GameObject("Chunk");
            obj.transform.position = pos;
            obj.AddComponent<MeshCollider>();
            obj.AddComponent<MeshFilter>();
            obj.AddComponent<MeshRenderer>();
            obj.GetComponent<Renderer>().material = mat;

            if (parent != null)
                obj.transform.parent = parent;
        }

        public void Generate()
        {
            generationPhase = GenerationPhase.IS_GENERATING_TERRAIN;

            GenerateChunkValues(0, pos.x, pos.z, Generation.continentalness, Generation.heightFromContinentalness, Generation.continentalness.Length);

            TerrainPainter.Paint(this, blocks, random);
            TreeGenerator.PlantTrees(Generation.defaultTree, this, blocks, random);

            generationPhase = GenerationPhase.DONE_GENERATING_TERRAIN;

            this.startMeshGen();
            Parallel.ForEach(GetChunkNeighbors(), n => {
                n?.startMeshGen();
            });
        }

        private void MergeBlockBuffer()
        {
            Generation.bufferDictionary.TryGetValue(this.pos, out int[] blockBuffer);
            if (blockBuffer != null)
            {
                for (int i = 0; i < blocks.Length; i++)
                {
                    if (blocks[i] == 0 && blockBuffer[i] != 0)
                    {
                        blocks[i] = blockBuffer[i];
                    }
                }
            }
            Generation.bufferDictionary.TryRemove(this.pos, out blockBuffer);
        }

        private void GenerateChunkValues(int yOffset, int xPos, int zPos, float[] continentalness, float[] heightFromContinentalness, int splineLength)
        {
            for (int x = 0; x < Chunk.Width; x++)
            {
                for (int z = 0; z < Chunk.Length; z++)
                {

                    float noiseX = x + xPos;
                    float noiseZ = z + zPos;

                    float noiseVal_2D = Noise.noise2D.GetNoise(noiseX, noiseZ);

                    // Get the length of our continentalness to height spline

                    float h = 0;
                    for (int i = 0; i < splineLength - 1; i++)
                    {
                        if (noiseVal_2D >= continentalness[i] && noiseVal_2D <= continentalness[i + 1])
                        {
                            // Create equation for this certain section of the spline:
                            float x1 = continentalness[i];
                            float x2 = continentalness[i + 1];
                            float y1 = heightFromContinentalness[i];
                            float y2 = heightFromContinentalness[i + 1];

                            // y = mx + b
                            // b = y - mx

                            float slope = (y2 - y1) / (x2 - x1);
                            float b = y1 - slope * x1;

                            h = slope * noiseVal_2D + b;
                        }
                    }

                    for (int y = 0; y < h + yOffset; y++)
                    {
                        float noiseVal_3D = Noise.noise3D.GetNoise(noiseX, (float)y, noiseZ);

                        int i = x + (z * Chunk.Width) + (y * Chunk.Width * Chunk.Length);

                        if (noiseVal_3D > 0)
                        {
                            blocks[i] = 1;
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void startMeshGen()
        {
            if (generationPhase == GenerationPhase.IS_GEN_MESH || generationPhase == GenerationPhase.IS_GENERATING_TERRAIN) return;

            // This lock is non-static
            // This chunk can not call this code when it is called from a seperate method / from another thread
            lock (_lock)
            {
                // We don't return if busyChunks contains our current chunk becuase that will cause holes
                if (!busyChunks.Contains(this)) busyChunks.Add(this);

                // We call MergeBlockBuffer() here so that any last minute additions to the buffer can be accounted for
                MergeBlockBuffer();
                mesher.GenerateMeshValues();
                mesher.GenerateMeshValuesWater();

                generationPhase = GenerationPhase.OPEN_FOR_MESH_GEN;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Meshify()
        {
            generationPhase = GenerationPhase.IS_GEN_MESH;

            Mesher.Meshify(chunkObj, terrainMeshValues);
            Mesher.Meshify(waterObj, waterMeshValues);

            busyChunks.Remove(this);
            generationPhase = GenerationPhase.IDLE;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Chunk[] GetChunkNeighbors()
        {
            Generation.chunkDictionary.TryGetValue(new Vector3Int(pos.x + Width, pos.y, pos.z), out Chunk rightChunk);
            Generation.chunkDictionary.TryGetValue(new Vector3Int(pos.x - Width, pos.y, pos.z), out Chunk leftChunk);
            Generation.chunkDictionary.TryGetValue(new Vector3Int(pos.x, pos.y, pos.z + Length), out Chunk frontChunk);
            Generation.chunkDictionary.TryGetValue(new Vector3Int(pos.x, pos.y, pos.z - Length), out Chunk backChunk);

            Chunk[] neighbors = { rightChunk, leftChunk, frontChunk, backChunk };
            return neighbors;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetBlock(int blockID, int x, int y, int z)
        {
            int[] blockArray = GetBlocksRelativeChunk(x, y, z);

            if (x >= 0) x = x % Chunk.Width;
            if (z >= 0) z = z % Chunk.Length;

            if (x < 0) x = Chunk.Width + x;
            if (z < 0) z = Chunk.Length + z;

            blockArray[Block.GetFlatIndex(x, y, z)] = blockID;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetBlock(int x, int y, int z)
        {
            int[] blockArray = GetBlocksRelativeChunk(x, y, z);

            if (x >= 0) x = x % Chunk.Width;
            if (z >= 0) z = z % Chunk.Length;

            if (x < 0) x = Chunk.Width + x;
            if (z < 0) z = Chunk.Length + z;

            return blockArray[Block.GetFlatIndex(x, y, z)];
        }

        private int[] GetBlocksRelativeChunk(int x, int y, int z)
        {
            Vector3Int possibleChunkPos = this.pos;

            if (x < 0) possibleChunkPos += Vector3Int.left * Chunk.Width;
            if (x >= Chunk.Width) possibleChunkPos += Vector3Int.right * Chunk.Width;

            if (z < 0) possibleChunkPos += Vector3Int.back * Chunk.Length;
            if (z >= Chunk.Length) possibleChunkPos += Vector3Int.forward * Chunk.Length;


            int[] blockArray = this.blocks;

            if (possibleChunkPos != this.pos)
            {
                Generation.chunkDictionary.TryGetValue(possibleChunkPos, out Chunk currentChunk);

                // If currentChunk is null create block buffer and add blocks to that!
                // We also need to check if currentChunk is generating terrain. If it is, and we add blocks directly to the chunk before painting,
                // the leaves will be painted over!
                if (currentChunk == null || currentChunk.generationPhase != GenerationPhase.IDLE)
                {
                    Generation.bufferDictionary.TryAdd(possibleChunkPos, new int[Chunk.Width * Chunk.Length * Chunk.Height]);
                    blockArray = Generation.bufferDictionary[possibleChunkPos];
                }
                else
                {
                    blockArray = currentChunk.blocks;
                }
            }

            return blockArray;
        }
    }
}
