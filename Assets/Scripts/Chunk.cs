using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;

public class Chunk
{
    // Whenever we add or remove from this list within a thread, we need to use a lock!
    public static List<Chunk> busyChunks = new List<Chunk>();

    public readonly int[] blocks;
    public Vector3Int pos;

    public static int Width { get; set; }
    public static int Length { get; set; }
    public static int Height { get; set; }


    public GameObject obj;
    public GameObject water;

    private static readonly Material mat = Resources.Load<Material>("ChunkMaterial");
    private static readonly Material waterMat = Resources.Load<Material>("WaterMaterial");

    public Task generationTask;
    public bool isMeshing = false;

    public Mesher mesher;
    public MeshValues terrainMeshValues;
    public MeshValues waterMeshValues;

    // Constructor
    public Chunk(Vector3Int pos)
    {
        blocks = new int[Width* Length * Height];
        this.pos = pos;

        mesher = new Mesher(this);
        terrainMeshValues = new MeshValues()
        {
            verts = new List<Vector3>(),
            tris = new List<int>(),
            UVs = new List<Vector2>()
        };

        waterMeshValues = new MeshValues()
        {
            verts = new List<Vector3>(),
            tris = new List<int>(),
            UVs = new List<Vector2>()
        };
    }

    [DllImport("VoxelEngine_v2", EntryPoint = "GenerateChunkValues")]
    public static extern IntPtr GenerateChunkValues(int width, int length, int height, int yOffset, int xPos, int zPos, IntPtr continentalness, IntPtr heightFromContinentalness, int splineLength);

    [DllImport("VoxelEngine_v2", EntryPoint = "DeleteChunkValues")]
    public static extern void DeleteChunkValues(IntPtr ptr);


    public void Generate()
    {
        obj = new GameObject("Chunk");
        obj.transform.position = pos;
        obj.AddComponent<MeshCollider>();
        obj.AddComponent<MeshFilter>();
        obj.AddComponent<MeshRenderer>();
        obj.GetComponent<Renderer>().material = mat;


        // INITIALIZE WATER SUB-CHUNK
        water = new GameObject("Water");
        water.transform.position = pos;
        water.AddComponent<MeshCollider>();
        water.AddComponent<MeshFilter>();
        water.AddComponent<MeshRenderer>();
        water.GetComponent<Renderer>().material = waterMat;


        water.transform.parent = obj.transform;


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


            TerrainPainter.Paint(blocks);

            startMeshGen();

            foreach(Chunk n in GetChunkNeighbors())
            {
                n?.startMeshGen();
            }
        }
        catch(Exception e)
        { 
            Debug.Log(e);
        }
        finally
        {
            if (continentalnessHandle.IsAllocated) { continentalnessHandle.Free(); }
            if (heightFromContinentalnessHandle.IsAllocated) { heightFromContinentalnessHandle.Free(); }
        }
    }

    public void startMeshGen()
    {
        if (isMeshing) return;

        // We don't return if busyChunks contains our current chunk becuase that will cause holes
        if(!busyChunks.Contains(this)) busyChunks.Add(this);

        generationTask = Task.Run(() => 
        {
            mesher.GenerateMeshValues();
            mesher.GenerateMeshValuesWater();
        });
    }

    public void Meshify()
    {
        isMeshing = true;
        Mesher.Meshify(obj, terrainMeshValues);
        Mesher.Meshify(water, waterMeshValues);
        
        busyChunks.Remove(this);
        isMeshing = false;
    }

    private Chunk[] GetChunkNeighbors()
    {
        Generation.chunkDictionary.TryGetValue(new Vector3Int(pos.x + Width, pos.y, pos.z), out Chunk rightChunk);
        Generation.chunkDictionary.TryGetValue(new Vector3Int(pos.x - Width, pos.y, pos.z), out Chunk leftChunk);
        Generation.chunkDictionary.TryGetValue(new Vector3Int(pos.x, pos.y, pos.z + Length), out Chunk frontChunk);
        Generation.chunkDictionary.TryGetValue(new Vector3Int(pos.x, pos.y, pos.z - Length), out Chunk backChunk);

        Chunk[] neighbors = { rightChunk, leftChunk, frontChunk, backChunk };
        return neighbors;
    }

    // Tells the compiler to inline this method
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetFlatIndex(int x, int y, int z)
    {
        int i = x + (z * Width) + (y * Width * Length);
        return i;
    }
}
