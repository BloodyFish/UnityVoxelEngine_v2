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
    private static Material mat = Resources.Load<Material>("ChunkMaterial");

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
    public static extern IntPtr GenerateChunkValues(int width, int length, int height, int xPos, int zPos);

    [DllImport("VoxelEngine_v2", EntryPoint = "DeleteChunkValues")]
    public static extern void DeleteChunkValues(IntPtr ptr);

    [DllImport("VoxelEngine_v2", EntryPoint = "GetUVs")]
    public static extern IntPtr GetUVs(float x, float y, float size);

    [DllImport("VoxelEngine_v2", EntryPoint = "DeleteUVs")]
    public static extern IntPtr DeleteUVs(IntPtr ptr);

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

            this.TerrainPaint();

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

                            IntPtr ptr = GetUVs(currentBlock.texCoord_right.x, currentBlock.texCoord_right.y, 16);
                            UVs.AddRange(GetResult(ptr));
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

                        IntPtr ptr = GetUVs(currentBlock.texCoord_right.x, currentBlock.texCoord_right.y, 16);
                        UVs.AddRange(GetResult(ptr));
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

                            IntPtr ptr = GetUVs(currentBlock.texCoord_left.x, currentBlock.texCoord_left.y, 16);
                            UVs.AddRange(GetResult(ptr));
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

                        IntPtr ptr = GetUVs(currentBlock.texCoord_left.x, currentBlock.texCoord_left.y, 16);
                        UVs.AddRange(GetResult(ptr));
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

                            IntPtr ptr = GetUVs(currentBlock.texCoord_front.x, currentBlock.texCoord_front.y, 16);
                            UVs.AddRange(GetResult(ptr));
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

                        IntPtr ptr = GetUVs(currentBlock.texCoord_front.x, currentBlock.texCoord_front.y, 16);
                        UVs.AddRange(GetResult(ptr));
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

                            IntPtr ptr = GetUVs(currentBlock.texCoord_back.x, currentBlock.texCoord_back.y, 16);
                            UVs.AddRange(GetResult(ptr));
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

                        IntPtr ptr = GetUVs(currentBlock.texCoord_back.x, currentBlock.texCoord_back.y, 16);
                        UVs.AddRange(GetResult(ptr));
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

                        IntPtr ptr = GetUVs(currentBlock.texCoord_top.x, currentBlock.texCoord_top.y, 16);
                        UVs.AddRange(GetResult(ptr));
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

                        IntPtr ptr = GetUVs(currentBlock.texCoord_bottom.x, currentBlock.texCoord_bottom.y, 16);
                        UVs.AddRange(GetResult(ptr));
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
               
                if(y >= height - UnityEngine.Random.Range(60, 75) && y <= height)
                {
                    blocks[index] = Block.SNOW;
                }
                else if (GetSlopeOfPoint(x, y, z) >= 1.25f)
                {
                    blocks[index] = Block.STONE;
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

    private float GetSlopeOfPoint(int x, int y, int z)
    {
        int y1 = y;
        int x1 = x;
        int z1 = z;

        int y2 = y;
        int x2 = x;
        int z2 = z;

        if (x > 0 && x <= width - 1) x1 = x - 1;
        if (x >= 0 && x < width - 1) x2 = x + 1;
        if (z > 0 && z <= length - 1) z1 = z - 1;
        if (z >= 0 && z < length - 1) z2 = z + 1;

        for (int i = 0; i < height - 1; i++)
        {
            if (blocks[GetFlatIndex(x1, i + 1, z)] == 0)
            {
                y1 = i;
                break;
            }
        }

        for (int i = 0; i < height - 1; i++)
        {
            if (blocks[GetFlatIndex(x2, i + 1, z)] == 0)
            {
                y2 = i;
                break;
            }
        }

        float slope1 = Math.Abs((float)(y2 - y1) / (float)(x2 - x1));


        for (int i = 0; i < height - 1; i++)
        {
            if (blocks[GetFlatIndex(x, i + 1, z1)] == 0)
            {
                y1 = i;
                break;
            }
        }

        for (int i = 0; i < height - 1; i++)
        {
            if (blocks[GetFlatIndex(x, i + 1, z2)] == 0)
            {
                y2 = i;
                break;
            }
        }

        float slope2 = Math.Abs((float)(y2 - y1) / (float)(z2 - z1));
        float slope = (slope1 + slope2) / 2;

        return slope;
    }

    private static Vector2[] GetResult(IntPtr ptr)
    {
        // In C++, it's all a list of 8 floats. We will need to put each set of 2 into a four vector2s (8/2 = 4) eventually.
        float[] result = new float[8];

        Marshal.Copy(ptr, result, 0, 8);
        DeleteUVs(ptr);

        Vector2[] uv_result = new Vector2[4];

        int count = 0;
        for (int i = 0; i < 4; i++)
        {
            Vector2 vector = new Vector2();
            vector.x = result[count++];
            vector.y = result[count++];

            uv_result[i] = vector;
        }

        return uv_result;
    }

    private int GetFlatIndex(int x, int y, int z)
    {
        int i = x + (z * width) + (y * width * length);
        return i;
    }
}
