using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace BloodyFish.UnityVoxelEngine.V2
{
    public struct MeshValues
    {
        public List<Vector3> verts;
        public List<int> tris;
        public List<Vector2> UVs;
        public List<Color32> colors;
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
            mesh.colors32 = values.colors.ToArray();

            mesh.Optimize();
            mesh.RecalculateNormals();

            obj.GetComponent<MeshFilter>().mesh = mesh;
            obj.GetComponent<MeshCollider>().sharedMesh = mesh;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GenerateRightFace(MeshValues values, Block currentBlock, int x, int y, int z)
        {
            int offset = values.verts.Count;
            Voxel_Verts.RightFace(values.verts, x, y, z);
            Voxel_Tris.GenerateTris(values.tris, offset);
            Voxel_UVs.GetUVs(values.UVs, currentBlock.texCoord_right.x, currentBlock.texCoord_right.y, 16);
            Voxel_Verts.AddVertexColor(values.colors, currentBlock.tint);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GenerateLeftFace(MeshValues values, Block currentBlock, int x, int y, int z)
        {
            int offset = values.verts.Count;
            Voxel_Verts.LeftFace(values.verts, x, y, z);
            Voxel_Tris.GenerateTris(values.tris, offset);
            Voxel_UVs.GetUVs(values.UVs, currentBlock.texCoord_left.x, currentBlock.texCoord_left.y, 16);
            Voxel_Verts.AddVertexColor(values.colors, currentBlock.tint);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GenerateFrontFace(MeshValues values, Block currentBlock, int x, int y, int z)
        {
            int offset = values.verts.Count;
            Voxel_Verts.FrontFace(values.verts, x, y, z);
            Voxel_Tris.GenerateTris(values.tris, offset);
            Voxel_UVs.GetUVs(values.UVs, currentBlock.texCoord_front.x, currentBlock.texCoord_front.y, 16);
            Voxel_Verts.AddVertexColor(values.colors, currentBlock.tint);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GenerateBackFace(MeshValues values, Block currentBlock, int x, int y, int z)
        {
            int offset = values.verts.Count;
            Voxel_Verts.BackFace(values.verts, x, y, z);
            Voxel_Tris.GenerateTris(values.tris, offset);
            Voxel_UVs.GetUVs(values.UVs, currentBlock.texCoord_back.x, currentBlock.texCoord_back.y, 16);
            Voxel_Verts.AddVertexColor(values.colors, currentBlock.tint);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GenerateTopFace(MeshValues values, Block currentBlock, int x, int y, int z)
        {
            int offset = values.verts.Count;
            Voxel_Verts.TopFace(values.verts, x, y, z);
            Voxel_Tris.GenerateTris(values.tris, offset);
            Voxel_UVs.GetUVs(values.UVs, currentBlock.texCoord_top.x, currentBlock.texCoord_top.y, 16);
            Voxel_Verts.AddVertexColor(values.colors, currentBlock.tint);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void GenerateBottomFace(MeshValues values, Block currentBlock, int x, int y, int z)
        {
            int offset = values.verts.Count;
            Voxel_Verts.BottomFace(values.verts, x, y, z);
            Voxel_Tris.GenerateTris(values.tris, offset);
            Voxel_UVs.GetUVs(values.UVs, currentBlock.texCoord_bottom.x, currentBlock.texCoord_bottom.y, 16);
            Voxel_Verts.AddVertexColor(values.colors, currentBlock.tint);
        }

        public void GenerateMeshValues()
        {
            MeshValues values = chunk.terrainMeshValues;

            values.verts.Clear();
            values.tris.Clear();
            values.UVs.Clear();
            values.colors.Clear();

            int index = 0;
            foreach (var i in chunk.blocks)
            {
                if (i > 0)
                {
                    // The blocks in possibleBlocks should be sorted by their blockID.
                    // So a block with blockID 1 should be at index 0, a block with blockID 2 should be at index 1, and so on.
                    Block currentBlock = Block.GetBlockFromID(i);

                    int x = index % Chunk.Width;
                    int z = (index / Chunk.Width) % Chunk.Length;
                    int y = (index / (Chunk.Width * Chunk.Length)) % Chunk.Height;

                    int rightIndex, leftIndex, frontIndex, backIndex, topIndex, bottomIndex;

                    // Width
                    if (x == Chunk.Width - 1)
                    {
                        if (Generation.chunkDictionary.TryGetValue(new Vector3Int(chunk.pos.x + Chunk.Width, chunk.pos.y, chunk.pos.z), out Chunk adjacentChunk))
                        {
                            int j = Block.GetFlatIndex(0, y, z);

                            int adjacentBlockID = adjacentChunk.blocks[j];
                            if (adjacentBlockID <= 0 || Block.CheckTransparentBlockPlacement(currentBlock, Block.GetBlockFromID(adjacentBlockID)))
                            {
                                GenerateRightFace(values, currentBlock, x, y, z);
                            }
                        }

                    }
                    if (x >= 0 && x < Chunk.Width - 1)
                    {
                        rightIndex = Block.GetFlatIndex(x + 1, y, z);

                        int adjacentBlockID = chunk.blocks[rightIndex];
                        if (adjacentBlockID <= 0 || Block.CheckTransparentBlockPlacement(currentBlock, Block.GetBlockFromID(adjacentBlockID)))
                        {
                            GenerateRightFace(values, currentBlock, x, y, z);
                        }
                    }

                    if (x == 0)
                    {
                        if (Generation.chunkDictionary.TryGetValue(new Vector3Int(chunk.pos.x - Chunk.Width, chunk.pos.y, chunk.pos.z), out Chunk adjacentChunk))
                        {
                            int j = Block.GetFlatIndex(Chunk.Width - 1, y, z);

                            int adjacentBlockID = adjacentChunk.blocks[j];
                            if (adjacentBlockID <= 0 || Block.CheckTransparentBlockPlacement(currentBlock, Block.GetBlockFromID(adjacentBlockID)))
                            {
                                GenerateLeftFace(values, currentBlock, x, y, z);
                            }
                        }
                    }

                    if (x > 0 && x <= Chunk.Width - 1)
                    {
                        leftIndex = Block.GetFlatIndex(x - 1, y, z);

                        int adjacentBlockID = chunk.blocks[leftIndex];
                        if (adjacentBlockID <= 0 || Block.CheckTransparentBlockPlacement(currentBlock, Block.GetBlockFromID(adjacentBlockID)))
                        {
                            GenerateLeftFace(values, currentBlock, x, y, z);
                        }
                    }

                    // LENGTH
                    if (z == Chunk.Length - 1)
                    {
                        if (Generation.chunkDictionary.TryGetValue(new Vector3Int(chunk.pos.x, chunk.pos.y, chunk.pos.z + Chunk.Length), out Chunk adjacentChunk))
                        {
                            int j = Block.GetFlatIndex(x, y, 0);

                            int adjacentBlockID = adjacentChunk.blocks[j];
                            if (adjacentBlockID <= 0 || Block.CheckTransparentBlockPlacement(currentBlock, Block.GetBlockFromID(adjacentBlockID)))
                            {
                                GenerateFrontFace(values, currentBlock, x, y, z);
                            }
                        }
                    }

                    if (z >= 0 && z < Chunk.Length - 1)
                    {
                        frontIndex = Block.GetFlatIndex(x, y, z + 1);

                        int adjacentBlockID = chunk.blocks[frontIndex];
                        if (adjacentBlockID <= 0 || Block.CheckTransparentBlockPlacement(currentBlock, Block.GetBlockFromID(adjacentBlockID)))
                        {
                            GenerateFrontFace(values, currentBlock, x, y, z);
                        }
                    }

                    if (z == 0)
                    {
                        if (Generation.chunkDictionary.TryGetValue(new Vector3Int(chunk.pos.x, chunk.pos.y, chunk.pos.z - Chunk.Length), out Chunk adjacentChunk))
                        {
                            int j = Block.GetFlatIndex(x, y, Chunk.Length - 1);

                            int adjacentBlockID = adjacentChunk.blocks[j];
                            if (adjacentBlockID <= 0 || Block.CheckTransparentBlockPlacement(currentBlock, Block.GetBlockFromID(adjacentBlockID)))
                            {
                                GenerateBackFace(values, currentBlock, x, y, z);

                            }
                        }
                    }

                    if (z > 0 && z <= Chunk.Length - 1)
                    {
                        backIndex = Block.GetFlatIndex(x, y, z - 1);

                        int adjacentBlockID = chunk.blocks[backIndex];
                        if (adjacentBlockID <= 0 || Block.CheckTransparentBlockPlacement(currentBlock, Block.GetBlockFromID(adjacentBlockID)))
                        {
                            GenerateBackFace(values, currentBlock, x, y, z);

                        }
                    }

                    // HEIGHT
                    if (y >= 0 && y < Chunk.Height - 1)
                    {
                        topIndex = Block.GetFlatIndex(x, y + 1, z);

                        int adjacentBlockID = chunk.blocks[topIndex];
                        if (adjacentBlockID <= 0 || Block.CheckTransparentBlockPlacement(currentBlock, Block.GetBlockFromID(adjacentBlockID)))
                        {
                            GenerateTopFace(values, currentBlock, x, y, z);

                        }
                    }

                    if (y > 0 && y <= Chunk.Height - 1)
                    {
                        bottomIndex = Block.GetFlatIndex(x, y - 1, z);

                        int adjacentBlockID = chunk.blocks[bottomIndex];
                        if (adjacentBlockID <= 0 || Block.CheckTransparentBlockPlacement(currentBlock, Block.GetBlockFromID(adjacentBlockID)))
                        {
                            GenerateBottomFace(values, currentBlock, x, y, z);
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
                            int j = Block.GetFlatIndex(0, y, z);
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
                        rightIndex = Block.GetFlatIndex(x + 1, y, z);

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
                            int j = Block.GetFlatIndex(Chunk.Width - 1, y, z);
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
                        leftIndex = Block.GetFlatIndex(x - 1, y, z);

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
                            int j = Block.GetFlatIndex(x, y, 0);
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
                        frontIndex = Block.GetFlatIndex(x, y, z + 1);

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
                            int j = Block.GetFlatIndex(x, y, Chunk.Length - 1);
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
                        backIndex = Block.GetFlatIndex(x, y, z - 1);

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
                        topIndex = Block.GetFlatIndex(x, y + 1, z);

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
                        bottomIndex = Block.GetFlatIndex(x, y - 1, z);

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
}