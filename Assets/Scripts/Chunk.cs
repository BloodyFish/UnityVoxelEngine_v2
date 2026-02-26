using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using UnityEngine;

public class Chunk
{
    public int[] blocks;
    public Vector3Int pos;
    public int width, length, height;
    private const int SCALE = 256;
    private const int WATER_LEVEL = 63;

    public GameObject obj;
    private static Material mat = Resources.Load<Material>("ChunkMaterial");


    public Dictionary<float, float> continentalnessToHeight = new Dictionary<float, float>()
    {
        // Negative continentalness values will map to ocean and beach hights
        {-1f, 0f },
        {-0.5f, 5f},
        {-0.2f, WATER_LEVEL},
        {-0.1f, 65},

        // Positive continentalness will map to mountain and hilly hights
        {0f, 70f},
        {0.3f, 100f},
        {1f, SCALE }

    };

    // Constructor
    public Chunk(int width, int length, int height, Vector3Int pos)
    {
        blocks = new int[width* length * height];
        this.pos = pos;

        this.width = width;
        this.length = length;
        this.height = height;


        obj = new GameObject();
        obj.transform.position = pos;
        obj.AddComponent<MeshCollider>();
        obj.AddComponent<MeshFilter>();
        obj.AddComponent<MeshRenderer>();

        obj.GetComponent<Renderer>().material = mat;
    }


    [DllImport("VoxelEngine_v2", EntryPoint = "GenerateChunkValues")]
    public static extern IntPtr GenerateChunkValues(int width, int length, int height, int yOffset, int xPos, int zPos, IntPtr continentalness, IntPtr heightFromContinentalness, int splineLength);

    [DllImport("VoxelEngine_v2", EntryPoint = "DeleteChunkValues")]
    public static extern void DeleteChunkValues(IntPtr ptr);


    public void  Generate()
    {
        float[] continentalness = continentalnessToHeight.Keys.ToArray();
        float[] heightFromContinentalness = continentalnessToHeight.Values.ToArray();

        GCHandle continentalnessHandle = GCHandle.Alloc(continentalness, GCHandleType.Pinned);
        GCHandle heightFromContinentalnessHandle = GCHandle.Alloc(heightFromContinentalness, GCHandleType.Pinned);

        try
        {
            IntPtr continentalnessPointer = continentalnessHandle.AddrOfPinnedObject();
            IntPtr heightFromContinentalnessPointer = heightFromContinentalnessHandle.AddrOfPinnedObject();

            int size = width * length * height;
            IntPtr ptr = GenerateChunkValues(width, length, height, 0, pos.x, pos.z, continentalnessPointer, heightFromContinentalnessPointer, continentalness.Length);
            int[] result = new int[size];

            Marshal.Copy(ptr, result, 0, size);

            DeleteChunkValues(ptr);

            blocks = result;

            this.TerrainPaint();
            this.Meshify();


            Generation.chunkDictionary.TryGetValue(new Vector3Int(pos.x + width, pos.y, pos.z), out Chunk rightChunk);
            Generation.chunkDictionary.TryGetValue(new Vector3Int(pos.x - width, pos.y, pos.z), out Chunk leftChunk);
            Generation.chunkDictionary.TryGetValue(new Vector3Int(pos.x, pos.y, pos.z + length), out Chunk frontChunk);
            Generation.chunkDictionary.TryGetValue(new Vector3Int(pos.x, pos.y, pos.z - length), out Chunk backChunk);

            rightChunk?.Meshify();
            leftChunk?.Meshify();
            frontChunk?.Meshify();
            backChunk?.Meshify();

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

    public void Meshify()
    {
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Vector2> UVs = new List<Vector2>();

        int index = 0;
        foreach (var i in blocks)
        {
            if (i > 0)
            {
                /*
                //Generate a random texture coordinate (FOR TESTING)
                int textureCoord_x = UnityEngine.Random.Range(0, 16);
                int textureCoord_y = UnityEngine.Random.Range(0, 16);
                Vector2 textureCoord = new Vector2(textureCoord_x, textureCoord_y);
                */

                // The blocks in possibleBlocks should be sorted by their blockID.
                // So a block with blockID 1 should be at index 0, a block with blockID 2 should be at index 1, and so on.
                Block currentBlock = Block.possibleBlocks[i - 1];

                int x = index % width;
                int z = (index / width) % length;
                int y = (index / (width * length)) % height;

                int rightIndex, leftIndex, frontIndex, backIndex, topIndex, bottomIndex;

                // WIDTH
                if (x == width - 1)
                {
                    if (Generation.chunkDictionary.TryGetValue(new Vector3Int(pos.x + width, pos.y, pos.z), out Chunk adjacentChunk))
                    {
                        int j = GetFlatIndex(0, y, z);
                        if (adjacentChunk.blocks[j] <= 0)
                        {
                            int offset = verts.Count;
                            Voxel_Verts.RightFace(verts, x, y, z);
                            Voxel_Tris.GenerateTris(tris, offset);

                            UVs.AddRange(GetUVs(currentBlock.texCoord_right.x, currentBlock.texCoord_right.y, 16));
                        }
                    }
                }
                if (x >= 0 && x < width - 1)
                {
                    rightIndex = GetFlatIndex(x + 1, y, z);

                    if (blocks[rightIndex] <= 0)
                    {
                        int offset = verts.Count;
                        Voxel_Verts.RightFace(verts, x, y, z);
                        Voxel_Tris.GenerateTris(tris, offset);

                        UVs.AddRange(GetUVs(currentBlock.texCoord_right.x, currentBlock.texCoord_right.y, 16));
                    }
                }

                if (x == 0)
                {
                    if (Generation.chunkDictionary.TryGetValue(new Vector3Int(pos.x - width, pos.y, pos.z), out Chunk adjacentChunk))
                    {
                        int j = GetFlatIndex(width - 1, y, z);
                        if (adjacentChunk.blocks[j] <= 0)
                        {
                            int offset = verts.Count;
                            Voxel_Verts.LeftFace(verts, x, y, z);
                            Voxel_Tris.GenerateTris(tris, offset);

                            UVs.AddRange(GetUVs(currentBlock.texCoord_left.x, currentBlock.texCoord_left.y, 16));
                        }
                    }
                }

                if (x > 0 && x <= width - 1)
                {
                    leftIndex = GetFlatIndex(x - 1, y, z);

                    if (blocks[leftIndex] <= 0)
                    {
                        int offset = verts.Count;
                        Voxel_Verts.LeftFace(verts, x, y, z);
                        Voxel_Tris.GenerateTris(tris, offset);

                        UVs.AddRange(GetUVs(currentBlock.texCoord_left.x, currentBlock.texCoord_left.y, 16));
                    }
                }

                // LENGTH
                if (z == length - 1)
                {
                    if (Generation.chunkDictionary.TryGetValue(new Vector3Int(pos.x, pos.y, pos.z + length), out Chunk adjacentChunk))
                    {
                        int j = GetFlatIndex(x, y, 0);
                        if (adjacentChunk.blocks[j] <= 0)
                        {
                            int offset = verts.Count;
                            Voxel_Verts.FrontFace(verts, x, y, z);
                            Voxel_Tris.GenerateTris(tris, offset);

                            UVs.AddRange(GetUVs(currentBlock.texCoord_front.x, currentBlock.texCoord_front.y, 16));
                        }
                    }
                }

                if (z >= 0 && z < length - 1)
                {
                    frontIndex = GetFlatIndex(x, y, z + 1);

                    if (blocks[frontIndex] <= 0)
                    {
                        int offset = verts.Count;
                        Voxel_Verts.FrontFace(verts, x, y, z);
                        Voxel_Tris.GenerateTris(tris, offset);

                        UVs.AddRange(GetUVs(currentBlock.texCoord_front.x, currentBlock.texCoord_front.y, 16));
                    }
                }

                if (z == 0)
                {
                    if (Generation.chunkDictionary.TryGetValue(new Vector3Int(pos.x, pos.y, pos.z - length), out Chunk adjacentChunk))
                    {
                        int j = GetFlatIndex(x, y, length - 1);
                        if (adjacentChunk.blocks[j] <= 0)
                        {
                            int offset = verts.Count;
                            Voxel_Verts.BackFace(verts, x, y, z);
                            Voxel_Tris.GenerateTris(tris, offset);

                            UVs.AddRange(GetUVs(currentBlock.texCoord_back.x, currentBlock.texCoord_back.y, 16));
                        }
                    }
                }

                if (z > 0 && z <= length - 1)
                {
                    backIndex = GetFlatIndex(x, y, z - 1);

                    if (blocks[backIndex] <= 0)
                    {
                        int offset = verts.Count;
                        Voxel_Verts.BackFace(verts, x, y, z);
                        Voxel_Tris.GenerateTris(tris, offset);

                        UVs.AddRange(GetUVs(currentBlock.texCoord_back.x, currentBlock.texCoord_back.y, 16));
                    }
                }

                // HEIGHT
                if (y >= 0 && y < height)
                {
                    topIndex = GetFlatIndex(x, y + 1, z);

                    if (blocks[topIndex] <= 0)
                    {
                        int offset = verts.Count;
                        Voxel_Verts.TopFace(verts, x, y, z);
                        Voxel_Tris.GenerateTris(tris, offset);

                        UVs.AddRange(GetUVs(currentBlock.texCoord_top.x, currentBlock.texCoord_top.y, 16));
                    }
                }

                if (y > 0 && y <= height)
                {
                    bottomIndex = GetFlatIndex(x, y - 1, z);

                    if (blocks[bottomIndex] <= 0)
                    {
                        int offset = verts.Count;
                        Voxel_Verts.BottomFace(verts, x, y, z);
                        Voxel_Tris.GenerateTris(tris, offset);

                        UVs.AddRange(GetUVs(currentBlock.texCoord_top.x, currentBlock.texCoord_top.y, 16));
                    }
                }
            }

            index++;
        }


        Mesh mesh = new Mesh();
        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.uv = UVs.ToArray();
        mesh.Optimize();
        mesh.RecalculateNormals();

        obj.GetComponent<MeshFilter>().mesh = mesh;
        obj.GetComponent<MeshCollider>().sharedMesh = mesh;
    }


    private void TerrainPaint()
    {
        int index = 0;
        foreach (var i in blocks)
        {
            if (i > 0)
            {
                int x = index % width;
                int z = (index / width) % length;
                int y = (index / (width * length)) % height;

                if (y >= SCALE - Generation.random.Next(60, 75) && y <= SCALE)
                {
                    blocks[index] = Block.SNOW;
                }
                /*else if (GetSlopeOfPoint(x, y, z) >= 1f)
                {
                    blocks[index] = Block.STONE;
                }*/
                else if (y <= WATER_LEVEL + 1)
                {
                    blocks[index] = Block.SAND;
                }
                else if (blocks[GetFlatIndex(x, y + 1, z)] == 0)
                {
                    blocks[index] = Block.GRASS;
                }
                else
                {
                    blocks[index] = Block.DIRT;
                }
            }

            index++;
        }
    }

    // THIS METHOD IS EXTREMELY INNEFICIENT
    private float GetSlopeOfPoint(int x, int y, int z)
    {
        int y1_x = y;
        int y1_z = y;
        int x1 = x;
        int z1 = z;

        int y2_x = y;
        int y2_z = y;
        int x2 = x;
        int z2 = z;

        if (x > 0 && x <= width - 1) x1 = x - 1;
        if (x >= 0 && x < width - 1) x2 = x + 1;
        if (z > 0 && z <= length - 1) z1 = z - 1;
        if (z >= 0 && z < length - 1) z2 = z + 1;

        bool y1Found_x = false;
        bool y2Found_x = false;

        bool y1Found_z = false;
        bool y2Found_z = false;

        for (int i = 0; i < height - 1; i++)
        {
            if (!y1Found_z && blocks[GetFlatIndex(x1, i + 1, z)] == 0)
            {
                y1_x = i;
                y1Found_x = true;
            }

            if (!y2Found_x && blocks[GetFlatIndex(x2, i + 1, z)] == 0)
            {
                y2_x = i;
                y2Found_x = true;
            }

            if (!y1Found_z && blocks[GetFlatIndex(x, i + 1, z1)] == 0)
            {
                y1_z = i;
                y1Found_z = true;
            }

            if (!y2Found_z && blocks[GetFlatIndex(x, i + 1, z2)] == 0)
            {
                y2_z = i;
                y2Found_z = true;
            }

            if (y1Found_x && y2Found_x && y1Found_z && y2Found_z) { break; }
        }

        float slope1 = Math.Abs((float)(y2_x - y1_x) / (float)(x2 - x1));
        float slope2 = Math.Abs((float)(y2_z - y1_z) / (float)(z2 - z1));

        float slope = (slope1 + slope2) / 2;

        return slope;
    }


    List<Vector2> GetUVs(float x, float y, float size)
    {
        // The coordinates of our texture atlas are as follows:
        // TOP LEFT = (0, 1)
        // TOP RIGHT = (1, 1)
        // BOTTOM LEFT = (0, 0)
        // BOTTOM RIGHT = (1, 0)

        float textureStep = 1 / size;

        float x0 = textureStep * x;
        float y0 = textureStep * y;
        float x1 = textureStep * (x + 1);
        float y1 = textureStep * (y + 1);

        List<Vector2> uvs = new List<Vector2> {
			// BOTTOM LEFT
			new Vector2(x0, y0),

			// TOP LEFT
			new Vector2(x0, y1),

			// TOP RIGHT
			new Vector2(x1, y1),

			// BOTTOM RIGHT
			new Vector2(x1, y0)
        };

        return uvs;
    }

    // Tells the compiler to inline this method
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetFlatIndex(int x, int y, int z)
    {
        int i = x + (z * width) + (y * width * length);
        return i;
    }
}
