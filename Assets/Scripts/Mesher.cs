using System.Collections.Generic;
using UnityEngine;

public struct MeshValues
{
    public List<Vector3> verts;
    public List<int> tris;
    public List<Vector2> UVs;
}

public class Mesher
{
    private Chunk chunk;

    public Mesher(Chunk chunk)
    {
        this.chunk = chunk;
    }

    public static void Meshify(GameObject obj, MeshValues values)
    {
        Mesh mesh = new Mesh();
        mesh.vertices = values.verts.ToArray();
        mesh.triangles = values.tris.ToArray();
        mesh.uv = values.UVs.ToArray();

        mesh.Optimize();
        mesh.RecalculateNormals();

        obj.GetComponent<MeshFilter>().mesh = mesh;
        obj.GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    public void GenerateMeshValues()
    {
        MeshValues values = chunk.terrainMeshValues;

        values.verts.Clear();
        values.tris.Clear();
        values.UVs.Clear();

        int index = 0;
        foreach (var i in chunk.blocks)
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

                int x = index % Chunk.Width;
                int z = (index / Chunk.Width) % Chunk.Length;
                int y = (index / (Chunk.Width * Chunk.Length)) % Chunk.Height;

                int rightIndex, leftIndex, frontIndex, backIndex, topIndex, bottomIndex;

                // Width
                if (x == Chunk.Width - 1)
                {
                    if (Generation.chunkDictionary.TryGetValue(new Vector3Int(chunk.pos.x + Chunk.Width, chunk.pos.y, chunk.pos.z), out Chunk adjacentChunk))
                    {
                        int j = Chunk.GetFlatIndex(0, y, z);
                        if (adjacentChunk.blocks[j] <= 0)
                        {
                            int offset = values.verts.Count;
                            Voxel_Verts.RightFace(values.verts, x, y, z);
                            Voxel_Tris.GenerateTris(values.tris, offset);
                            Voxel_UVs.GetUVs(values.UVs, currentBlock.texCoord_right.x, currentBlock.texCoord_right.y, 16);
                        }
                    }
                }
                if (x >= 0 && x < Chunk.Width - 1)
                {
                    rightIndex = Chunk.GetFlatIndex(x + 1, y, z);

                    if (chunk.blocks[rightIndex] <= 0)
                    {
                        int offset = values.verts.Count;
                        Voxel_Verts.RightFace(values.verts, x, y, z);
                        Voxel_Tris.GenerateTris(values.tris, offset);
                        Voxel_UVs.GetUVs(values.UVs, currentBlock.texCoord_right.x, currentBlock.texCoord_right.y, 16);
                    }
                }

                if (x == 0)
                {
                    if (Generation.chunkDictionary.TryGetValue(new Vector3Int(chunk.pos.x - Chunk.Width, chunk.pos.y, chunk.pos.z), out Chunk adjacentChunk))
                    {
                        int j = Chunk.GetFlatIndex(Chunk.Width - 1, y, z);
                        if (adjacentChunk.blocks[j] <= 0)
                        {
                            int offset = values.verts.Count;
                            Voxel_Verts.LeftFace(values.verts, x, y, z);
                            Voxel_Tris.GenerateTris(values.tris, offset);
                            Voxel_UVs.GetUVs(values.UVs, currentBlock.texCoord_left.x, currentBlock.texCoord_left.y, 16);
                        }
                    }
                }

                if (x > 0 && x <= Chunk.Width - 1)
                {
                    leftIndex = Chunk.GetFlatIndex(x - 1, y, z);

                    if (chunk.blocks[leftIndex] <= 0)
                    {
                        int offset = values.verts.Count;
                        Voxel_Verts.LeftFace(values.verts, x, y, z);
                        Voxel_Tris.GenerateTris(values.tris, offset);
                        Voxel_UVs.GetUVs(values.UVs, currentBlock.texCoord_left.x, currentBlock.texCoord_left.y, 16);
                    }
                }

                // LENGTH
                if (z == Chunk.Length - 1)
                {
                    if (Generation.chunkDictionary.TryGetValue(new Vector3Int(chunk.pos.x, chunk.pos.y, chunk.pos.z + Chunk.Length), out Chunk adjacentChunk))
                    {
                        int j = Chunk.GetFlatIndex(x, y, 0);
                        if (adjacentChunk.blocks[j] <= 0)
                        {
                            int offset = values.verts.Count;
                            Voxel_Verts.FrontFace(values.verts, x, y, z);
                            Voxel_Tris.GenerateTris(values.tris, offset);
                            Voxel_UVs.GetUVs(values.UVs, currentBlock.texCoord_front.x, currentBlock.texCoord_front.y, 16);
                        }
                    }
                }

                if (z >= 0 && z < Chunk.Length - 1)
                {
                    frontIndex = Chunk.GetFlatIndex(x, y, z + 1);

                    if (chunk.blocks[frontIndex] <= 0)
                    {
                        int offset = values.verts.Count;
                        Voxel_Verts.FrontFace(values.verts, x, y, z);
                        Voxel_Tris.GenerateTris(values.tris, offset);
                        Voxel_UVs.GetUVs(values.UVs, currentBlock.texCoord_front.x, currentBlock.texCoord_front.y, 16);
                    }
                }

                if (z == 0)
                {
                    if (Generation.chunkDictionary.TryGetValue(new Vector3Int(chunk.pos.x, chunk.pos.y, chunk.pos.z - Chunk.Length), out Chunk adjacentChunk))
                    {
                        int j = Chunk.GetFlatIndex(x, y, Chunk.Length - 1);
                        if (adjacentChunk.blocks[j] <= 0)
                        {
                            int offset = values.verts.Count;
                            Voxel_Verts.BackFace(values.verts, x, y, z);
                            Voxel_Tris.GenerateTris(values.tris, offset);
                            Voxel_UVs.GetUVs(values.UVs, currentBlock.texCoord_back.x, currentBlock.texCoord_back.y, 16);
                        }
                    }
                }

                if (z > 0 && z <= Chunk.Length - 1)
                {
                    backIndex = Chunk.GetFlatIndex(x, y, z - 1);

                    if (chunk.blocks[backIndex] <= 0)
                    {
                        int offset = values.verts.Count;
                        Voxel_Verts.BackFace(values.verts, x, y, z);
                        Voxel_Tris.GenerateTris(values.tris, offset);
                        Voxel_UVs.GetUVs(values.UVs, currentBlock.texCoord_back.x, currentBlock.texCoord_back.y, 16);
                    }
                }

                // HEIGHT
                if (y >= 0 && y < Chunk.Height)
                {
                    topIndex = Chunk.GetFlatIndex(x, y + 1, z);

                    if (chunk.blocks[topIndex] <= 0)
                    {
                        int offset = values.verts.Count;
                        Voxel_Verts.TopFace(values.verts, x, y, z);
                        Voxel_Tris.GenerateTris(values.tris, offset);
                        Voxel_UVs.GetUVs(values.UVs, currentBlock.texCoord_top.x, currentBlock.texCoord_top.y, 16);
                    }
                }

                if (y > 0 && y <= Chunk.Height)
                {
                    bottomIndex = Chunk.GetFlatIndex(x, y - 1, z);

                    if (chunk.blocks[bottomIndex] <= 0)
                    {
                        int offset = values.verts.Count;
                        Voxel_Verts.BottomFace(values.verts, x, y, z);
                        Voxel_Tris.GenerateTris(values.tris, offset);
                        Voxel_UVs.GetUVs(values.UVs, currentBlock.texCoord_bottom.x, currentBlock.texCoord_bottom.y, 16);
                    }
                }
            }

            index++;
        }
    }
    public void GenerateMeshValuesWater()
    {
        MeshValues values = chunk.waterMeshValues;

        values.verts.Clear();
        values.tris.Clear();
        values.UVs.Clear();

        int index = 0;
        foreach (var i in chunk.blocks)
        {
            if (i < 0)
            {
                int x = index % Chunk.Width;
                int z = (index / Chunk.Width) % Chunk.Length;
                int y = (index / (Chunk.Width * Chunk.Length)) % Chunk.Height;

                int rightIndex, leftIndex, frontIndex, backIndex, topIndex, bottomIndex;

                // Width
                if (x == Chunk.Width - 1)
                {
                    if (Generation.chunkDictionary.TryGetValue(new Vector3Int(chunk.pos.x + Chunk.Width, chunk.pos.y, chunk.pos.z), out Chunk adjacentChunk))
                    {
                        int j = Chunk.GetFlatIndex(0, y, z);
                        if (adjacentChunk.blocks[j] == 0)
                        {
                            int offset = values.verts.Count;
                            Voxel_Verts_Water.RightFace(values.verts, x, y, z);
                            Voxel_Tris.GenerateTris(values.tris, offset);
                            Voxel_UVs.GetUVs(values.UVs, 0, 0, 1);
                        }
                    }
                }
                if (x >= 0 && x < Chunk.Width - 1)
                {
                    rightIndex = Chunk.GetFlatIndex(x + 1, y, z);

                    if (chunk.blocks[rightIndex] == 0)
                    {
                        int offset = values.verts.Count;
                        Voxel_Verts_Water.RightFace(values.verts, x, y, z);
                        Voxel_Tris.GenerateTris(values.tris, offset);
                        Voxel_UVs.GetUVs(values.UVs, 0, 0, 1);
                    }
                }

                if (x == 0)
                {
                    if (Generation.chunkDictionary.TryGetValue(new Vector3Int(chunk.pos.x - Chunk.Width, chunk.pos.y, chunk.pos.z), out Chunk adjacentChunk))
                    {
                        int j = Chunk.GetFlatIndex(Chunk.Width - 1, y, z);
                        if (adjacentChunk.blocks[j] == 0)
                        {
                            int offset = values.verts.Count;
                            Voxel_Verts_Water.LeftFace(values.verts, x, y, z);
                            Voxel_Tris.GenerateTris(values.tris, offset);
                            Voxel_UVs.GetUVs(values.UVs, 0, 0, 1);
                        }
                    }
                }

                if (x > 0 && x <= Chunk.Width - 1)
                {
                    leftIndex = Chunk.GetFlatIndex(x - 1, y, z);

                    if (chunk.blocks[leftIndex] == 0)
                    {
                        int offset = values.verts.Count;
                        Voxel_Verts_Water.LeftFace(values.verts, x, y, z);
                        Voxel_Tris.GenerateTris(values.tris, offset);
                        Voxel_UVs.GetUVs(values.UVs, 0, 0, 1);
                    }
                }

                // LENGTH
                if (z == Chunk.Length - 1)
                {
                    if (Generation.chunkDictionary.TryGetValue(new Vector3Int(chunk.pos.x, chunk.pos.y, chunk.pos.z + Chunk.Length), out Chunk adjacentChunk))
                    {
                        int j = Chunk.GetFlatIndex(x, y, 0);
                        if (adjacentChunk.blocks[j] == 0)
                        {
                            int offset = values.verts.Count;
                            Voxel_Verts_Water.FrontFace(values.verts, x, y, z);
                            Voxel_Tris.GenerateTris(values.tris, offset);
                            Voxel_UVs.GetUVs(values.UVs, 0, 0, 1);
                        }
                    }
                }

                if (z >= 0 && z < Chunk.Length - 1)
                {
                    frontIndex = Chunk.GetFlatIndex(x, y, z + 1);

                    if (chunk.blocks[frontIndex] == 0)
                    {
                        int offset = values.verts.Count;
                        Voxel_Verts_Water.FrontFace(values.verts, x, y, z);
                        Voxel_Tris.GenerateTris(values.tris, offset);
                        Voxel_UVs.GetUVs(values.UVs, 0, 0, 1);
                    }
                }

                if (z == 0)
                {
                    if (Generation.chunkDictionary.TryGetValue(new Vector3Int(chunk.pos.x, chunk.pos.y, chunk.pos.z - Chunk.Length), out Chunk adjacentChunk))
                    {
                        int j = Chunk.GetFlatIndex(x, y, Chunk.Length - 1);
                        if (adjacentChunk.blocks[j] == 0)
                        {
                            int offset = values.verts.Count;
                            Voxel_Verts_Water.BackFace(values.verts, x, y, z);
                            Voxel_Tris.GenerateTris(values.tris, offset);
                            Voxel_UVs.GetUVs(values.UVs, 0, 0, 1);
                        }
                    }
                }

                if (z > 0 && z <= Chunk.Length - 1)
                {
                    backIndex = Chunk.GetFlatIndex(x, y, z - 1);

                    if (chunk.blocks[backIndex] == 0)
                    {
                        int offset = values.verts.Count;
                        Voxel_Verts_Water.BackFace(values.verts, x, y, z);
                        Voxel_Tris.GenerateTris(values.tris, offset);
                        Voxel_UVs.GetUVs(values.UVs, 0, 0, 1);
                    }
                }

                // HEIGHT
                if (y >= 0 && y < Chunk.Height)
                {
                    topIndex = Chunk.GetFlatIndex(x, y + 1, z);

                    // Don't draw face ONLY if there's a water block on top
                    // We want to draw faces when there's a solid block above due to the offset of water
                    if (chunk.blocks[topIndex] == 0 || chunk.blocks[topIndex] > 0 && y == Generation.WATER_LEVEL)
                    {
                        int offset = values.verts.Count;
                        Voxel_Verts_Water.TopFace(values.verts, x, y, z);
                        Voxel_Tris.GenerateTris(values.tris, offset);
                        Voxel_UVs.GetUVs(values.UVs, 0, 0, 1);
                    }
                }

                if (y > 0 && y <= Chunk.Height)
                {
                    bottomIndex = Chunk.GetFlatIndex(x, y - 1, z);

                    if (chunk.blocks[bottomIndex] == 0)
                    {
                        int offset = values.verts.Count;
                        Voxel_Verts_Water.BottomFace(values.verts, x, y, z);
                        Voxel_Tris.GenerateTris(values.tris, offset);
                        Voxel_UVs.GetUVs(values.UVs, 0, 0, 1);
                    }
                }
            }

            index++;
        }
    }
}