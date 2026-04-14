using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;

namespace BloodyFish.UnityVoxelEngine.V2
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

        public bool isGenerating;
        public bool isMeshing = false;
        public readonly object _lock = new object();

        public Mesher mesher;
        public MeshValues terrainMeshValues;
        public MeshValues waterMeshValues;

        // Constructor
        public Chunk(Vector3Int pos)
        {
            blocks = new int[Width * Length * Height];
            this.pos = pos;

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
        }

        [DllImport("VoxelEngine_v2", EntryPoint = "GenerateChunkValues")]
        public static extern IntPtr GenerateChunkValues(int width, int length, int height, int yOffset, int xPos, int zPos, IntPtr continentalness, IntPtr heightFromContinentalness, int splineLength);

        [DllImport("VoxelEngine_v2", EntryPoint = "DeleteChunkValues")]
        public static extern void DeleteChunkValues(IntPtr ptr);


        public void Generate()
        {
            float[] continentalness = Generation.continentalness;
            float[] heightFromContinentalness = Generation.heightFromContinentalness;

            GCHandle continentalnessHandle = GCHandle.Alloc(continentalness, GCHandleType.Pinned);
            GCHandle heightFromContinentalnessHandle = GCHandle.Alloc(heightFromContinentalness, GCHandleType.Pinned);

            try
            {
                IntPtr continentalnessPointer = continentalnessHandle.AddrOfPinnedObject();
                IntPtr heightFromContinentalnessPointer = heightFromContinentalnessHandle.AddrOfPinnedObject();

                int size = Width * Length * Height;
                IntPtr ptr = GenerateChunkValues(Width, Length, Height, 0, pos.x, pos.z, continentalnessPointer, heightFromContinentalnessPointer, continentalness.Length);

                Marshal.Copy(ptr, blocks, 0, size);
                DeleteChunkValues(ptr);

                System.Random random = new System.Random(Generation.seed);
                TerrainPainter.Paint(blocks, random);
                TreeGenerator.PlantTrees(Generation.defaultTree, blocks, random);

                this.startMeshGen();
                Parallel.ForEach(GetChunkNeigbors(), n => {
                    n?.startMeshGen();
                });
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
            finally
            {
                if (continentalnessHandle.IsAllocated) { continentalnessHandle.Free(); }
                if (heightFromContinentalnessHandle.IsAllocated) { heightFromContinentalnessHandle.Free(); }
            }
        }

        public void CreateObj(ref GameObject obj, Transform parent, string name, Material mat)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void startMeshGen()
        {
            if (isMeshing) return;

            // We don't return if busyChunks contains our current chunk becuase that will cause holes
            if (!busyChunks.Contains(this)) busyChunks.Add(this);

            // This lock is non-static
            // This chunk can not call this code when it is called from a seperate method
            lock (_lock)
            {
                isGenerating = true;
                mesher.GenerateMeshValues();
                mesher.GenerateMeshValuesWater();
                isGenerating = false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Meshify()
        {
            isMeshing = true;

            Mesher.Meshify(chunkObj, terrainMeshValues);
            Mesher.Meshify(waterObj, waterMeshValues);

            busyChunks.Remove(this);
            isMeshing = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Chunk[] GetChunkNeigbors()
        {
            Generation.chunkDictionary.TryGetValue(new Vector3Int(pos.x + Width, pos.y, pos.z), out Chunk rightChunk);
            Generation.chunkDictionary.TryGetValue(new Vector3Int(pos.x - Width, pos.y, pos.z), out Chunk leftChunk);
            Generation.chunkDictionary.TryGetValue(new Vector3Int(pos.x, pos.y, pos.z + Length), out Chunk frontChunk);
            Generation.chunkDictionary.TryGetValue(new Vector3Int(pos.x, pos.y, pos.z - Length), out Chunk backChunk);

            Chunk[] neighbors = { rightChunk, leftChunk, frontChunk, backChunk };
            return neighbors;
        }
    }
}
