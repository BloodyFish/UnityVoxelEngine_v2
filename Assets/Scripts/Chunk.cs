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
        obj.GetComponent<Renderer>().material = new Material(Shader.Find("Universal Render Pipeline/Lit"));

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

        int index = 0;
        foreach (var i in blocks)
        {
            if (i > 0)
            {
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
                    }
                }
            }

            index++;
        }

        Mesh mesh = new Mesh();
        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        obj.GetComponent<MeshFilter>().mesh = mesh;
        obj.GetComponent<MeshCollider>().sharedMesh = mesh;

    }

    private int GetFlatIndex(int x, int y, int z)
    {
        int i = x + (z * width) + (y * width * length);
        return i;
    }
}
