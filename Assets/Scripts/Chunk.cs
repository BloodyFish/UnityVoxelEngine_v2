using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class Chunk
{
    public int[] blocks;
    public Vector3Int pos;
    public int width, length, height;

    public GameObject obj;
    
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

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.mainTexture = Resources.Load<Texture2D>("terrain");
        mat.SetFloat("_Smoothness", 0);

        obj.GetComponent<Renderer>().material = mat;
    }


    [DllImport("VoxelEngine_v2", EntryPoint = "GenerateChunkValues")]
    public static extern IntPtr GenerateChunkValues(int width, int length, int height, int xPos, int zPos);

    [DllImport("VoxelEngine_v2", EntryPoint = "DeleteChunkValues")]
    public static extern void DeleteChunkValues(IntPtr ptr);

    public void Generate()
    {
        try
        {
            int size = width * length * height;
            IntPtr ptr = GenerateChunkValues(width, length, height, pos.x, pos.z);
            int[] result = new int[size];

            Marshal.Copy(ptr, result, 0, size);

            DeleteChunkValues(ptr);

            blocks = result;

            Generation.chunkDictionary.TryGetValue(new Vector3Int(pos.x + width, pos.y, pos.z), out Chunk rightChunk);
            Generation.chunkDictionary.TryGetValue(new Vector3Int(pos.x - width, pos.y, pos.z), out Chunk leftChunk);
            Generation.chunkDictionary.TryGetValue(new Vector3Int(pos.x, pos.y, pos.z + length), out Chunk frontChunk);
            Generation.chunkDictionary.TryGetValue(new Vector3Int(pos.x, pos.y, pos.z - length), out Chunk backChunk);

            this.Meshify();
            rightChunk?.Meshify();
            leftChunk?.Meshify();
            frontChunk?.Meshify();
            backChunk?.Meshify();

        }
        catch(Exception e)
        { 
            Debug.Log(e);
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
                //Generate a random texture coordinate (FOR TESTING)
                int textureCoord_x = UnityEngine.Random.Range(0, 16);
                int textureCoord_y = UnityEngine.Random.Range(0, 16);
                Vector2 textureCoord = new Vector2(textureCoord_x, textureCoord_y);
            
                int x = index % width;
                int z = (index / width) % length;
                int y = (index / (width * length)) % height;

                int rightIndex, leftIndex, frontIndex, backIndex, topIndex, bottomIndex;
                
                // WIDTH
                if(x == width - 1)
                {
                    if(Generation.chunkDictionary.TryGetValue(new Vector3Int(pos.x + width, pos.y, pos.z), out Chunk adjacentChunk))
                    {
                        int j = GetFlatIndex(0, y, z);
                        if(adjacentChunk.blocks[j] <= 0)
                        {
                            int offset = verts.Count;
                            verts.AddRange(Voxel_Verts.GetRightFace(x, y, z));
                            tris.AddRange(Voxel_Tris.GenerateTris(offset));
                            UVs.AddRange(GetUVs(textureCoord));
                        }
                    }
                }
                if(x >= 0 && x < width - 1)
                { 
                    rightIndex = GetFlatIndex(x + 1, y, z);

                    if (blocks[rightIndex] <= 0)
                    {
                        int offset = verts.Count;
                        verts.AddRange(Voxel_Verts.GetRightFace(x, y, z));
                        tris.AddRange(Voxel_Tris.GenerateTris(offset));
                        UVs.AddRange(GetUVs(textureCoord));
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
                            verts.AddRange(Voxel_Verts.GetLeftFace(x, y, z));
                            tris.AddRange(Voxel_Tris.GenerateTris(offset));
                            UVs.AddRange(GetUVs(textureCoord));
                        }
                    }
                }

                if (x > 0 && x <= width - 1)
                {
                    leftIndex = GetFlatIndex(x - 1, y, z);

                    if (blocks[leftIndex] <= 0)
                    {
                        int offset = verts.Count;
                        verts.AddRange(Voxel_Verts.GetLeftFace(x, y, z));
                        tris.AddRange(Voxel_Tris.GenerateTris(offset));
                        UVs.AddRange(GetUVs(textureCoord));
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
                            verts.AddRange(Voxel_Verts.GetFrontFace(x, y, z));
                            tris.AddRange(Voxel_Tris.GenerateTris(offset));
                            UVs.AddRange(GetUVs(textureCoord));
                        }
                    }
                }

                if (z >= 0 && z < length - 1)
                {
                    frontIndex = GetFlatIndex(x, y, z + 1);

                    if (blocks[frontIndex] <= 0)
                    {
                        int offset = verts.Count;
                        verts.AddRange(Voxel_Verts.GetFrontFace(x, y, z));
                        tris.AddRange(Voxel_Tris.GenerateTris(offset));
                        UVs.AddRange(GetUVs(textureCoord));
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
                            verts.AddRange(Voxel_Verts.GetBackFace(x, y, z));
                            tris.AddRange(Voxel_Tris.GenerateTris(offset));
                            UVs.AddRange(GetUVs(textureCoord));
                        }
                    }
                }

                if (z > 0 && z <= length - 1)
                {
                    backIndex = GetFlatIndex(x, y, z - 1);

                    if (blocks[backIndex] <= 0)
                    {
                        int offset = verts.Count;
                        verts.AddRange(Voxel_Verts.GetBackFace(x, y, z));
                        tris.AddRange(Voxel_Tris.GenerateTris(offset));
                        UVs.AddRange(GetUVs(textureCoord));
                    }
                }

                // HEIGHT
                if(y >= 0 && y < height)
                {
                    topIndex = GetFlatIndex(x, y + 1, z);

                    if (blocks[topIndex] <= 0)
                    {
                        int offset = verts.Count;
                        verts.AddRange(Voxel_Verts.GetTopFace(x, y, z));
                        tris.AddRange(Voxel_Tris.GenerateTris(offset));
                        UVs.AddRange(GetUVs(textureCoord));
                    }
                }

                if (y > 0 && y <= height)
                {
                    bottomIndex = GetFlatIndex(x, y - 1, z);

                    if (blocks[bottomIndex] <= 0)
                    {
                        int offset = verts.Count;
                        verts.AddRange(Voxel_Verts.GetBottomFace(x, y, z));
                        tris.AddRange(Voxel_Tris.GenerateTris(offset));
                        UVs.AddRange(GetUVs(textureCoord));
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

    private List<Vector2> GetUVs(Vector2 textureCoord)
    {
        // The coordinates of our texture atlas are as follows:
        Vector2 topLeft = new Vector2(0, 1);
        Vector2 topRight = new Vector2(1, 1);
        Vector2 bottomLeft = new Vector2(0, 0);
        Vector2 bottomRight = new Vector2(1, 0);


        // Our texture atlas is 16x16 textures wide
        float textureStep = 1f / 16f;


        List<Vector2> UVs = new List<Vector2>();
        float x = textureStep * (textureCoord.x);
        float y = textureStep * (textureCoord.y);
        float x1 = textureStep * (textureCoord.x + 1);
        float y1 = textureStep * (textureCoord.y + 1);


        // BOTTOM LEFT
        UVs.Add(new Vector2(x, y));

        // TOP LEFT
        UVs.Add(new Vector2(x, y1));

        // TOP RIGHT
        UVs.Add(new Vector2(x1, y1));

        // BOTTOM RIGHT
        UVs.Add(new Vector2(x1, y));


        return UVs;
    }

    private int GetFlatIndex(int x, int y, int z)
    {
        int i = x + (z * width) + (y * width * length);
        return i;
    }
}
