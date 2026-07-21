using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace BloodyFish.UnityVoxelEngine.v2
{
    public struct MeshValues
    {
        public NativeList<float3> verts;
        public NativeList<int> tris;
        public NativeList<float2> UVs;
        public NativeList<Color32> colors;
    }

    public class Mesher
    {
        private readonly Chunk chunk;

        public Mesher(Chunk chunk)
        {
            this.chunk = chunk;
        }

        public static void Meshify(GameObject obj, MeshValues values)
        {
            Mesh mesh = new Mesh();

            Vector3[] vertArray = new Vector3[values.verts.Length];
            for(int i = 0; i < values.verts.Length; i++)
            {
                vertArray[i] = (Vector3)values.verts[i];
            }
            mesh.vertices = vertArray;


            int[] triArray = new int[values.tris.Length];
            for (int i = 0; i < values.tris.Length; i++)
            {
                triArray[i] = values.tris[i];
            }
            mesh.triangles = triArray;

            Vector2[] uvArray = new Vector2[values.UVs.Length];
            for (int i = 0; i < values.UVs.Length; i++)
            {
                uvArray[i] = (Vector2)values.UVs[i];
            }
            mesh.uv = uvArray;

            Color32[] colorArray = new Color32[values.colors.Length];
            for (int i = 0; i < values.colors.Length; i++)
            {
               colorArray[i] = values.colors[i];
            }
            mesh.colors32 = colorArray;


            mesh.Optimize();
            mesh.RecalculateNormals();

            obj.GetComponent<MeshFilter>().mesh = mesh;

            if (obj.GetComponent<MeshCollider>())
            {
                obj.GetComponent<MeshCollider>().sharedMesh = mesh;
            }
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GenerateRightFace(MeshValues values, BlockData currentBlock, int x, int y, int z)
        {
            int offset = values.verts.Length;
            Voxel_Verts.RightFace(ref values.verts, x, y, z);
            Voxel_Tris.GenerateTris(ref values.tris, offset);
            Voxel_UVs.GetUVs(ref values.UVs, currentBlock.texCoord_right.x, currentBlock.texCoord_right.y, 16);
            Voxel_Verts.AddVertexColor(ref values.colors, currentBlock.tint);
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GenerateLeftFace(MeshValues values, BlockData currentBlock, int x, int y, int z)
        {
            int offset = values.verts.Length;
            Voxel_Verts.LeftFace(ref values.verts, x, y, z);
            Voxel_Tris.GenerateTris(ref values.tris, offset);
            Voxel_UVs.GetUVs(ref values.UVs, currentBlock.texCoord_left.x, currentBlock.texCoord_left.y, 16);
            Voxel_Verts.AddVertexColor(ref values.colors, currentBlock.tint);
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GenerateFrontFace(MeshValues values, BlockData currentBlock, int x, int y, int z)
        {
            int offset = values.verts.Length;
            Voxel_Verts.FrontFace(ref values.verts, x, y, z);
            Voxel_Tris.GenerateTris(ref values.tris, offset);
            Voxel_UVs.GetUVs(ref values.UVs, currentBlock.texCoord_front.x, currentBlock.texCoord_front.y, 16);
            Voxel_Verts.AddVertexColor(ref values.colors, currentBlock.tint);
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GenerateBackFace(MeshValues values, BlockData currentBlock, int x, int y, int z)
        {
            int offset = values.verts.Length;
            Voxel_Verts.BackFace(ref values.verts, x, y, z);
            Voxel_Tris.GenerateTris(ref values.tris, offset);
            Voxel_UVs.GetUVs(ref values.UVs, currentBlock.texCoord_back.x, currentBlock.texCoord_back.y, 16);
            Voxel_Verts.AddVertexColor(ref values.colors, currentBlock.tint);
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GenerateTopFace(MeshValues values, BlockData currentBlock, int x, int y, int z)
        {
            int offset = values.verts.Length;
            Voxel_Verts.TopFace(ref values.verts, x, y, z);
            Voxel_Tris.GenerateTris(ref values.tris, offset);
            Voxel_UVs.GetUVs(ref values.UVs, currentBlock.texCoord_top.x, currentBlock.texCoord_top.y, 16);
            Voxel_Verts.AddVertexColor(ref values.colors, currentBlock.tint);
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void GenerateBottomFace(MeshValues values, BlockData currentBlock, int x, int y, int z)
        {
            int offset = values.verts.Length;
            Voxel_Verts.BottomFace(ref values.verts, x, y, z);
            Voxel_Tris.GenerateTris(ref values.tris, offset);
            Voxel_UVs.GetUVs(ref values.UVs, currentBlock.texCoord_bottom.x, currentBlock.texCoord_bottom.y, 16);
            Voxel_Verts.AddVertexColor(ref values.colors, currentBlock.tint);
        }

        [BurstCompile]
        public static ChunkValues GenerateMeshValues(ChunkValues chunkValues, NativeParallelHashMap<int2, ChunkValues> chunkDictionary, NativeArray<BlockData> possbleBlocks)
        {
            MeshValues values = chunkValues.terrainMeshValues;

            values.verts.Clear();
            values.tris.Clear();
            values.UVs.Clear();
            values.colors.Clear();

            for(int index = 0; index < chunkValues.blocks.Length; index++)
            {
                int i = chunkValues.blocks[index];
                if (i > 0)
                {
                    // The blocks in possibleBlocks should be sorted by their blockID.
                    // So a block with blockID 1 should be at index 0, a block with blockID 2 should be at index 1, and so on.
                    BlockData currentBlock = possbleBlocks[i - 1];

                    int x = index % ChunkValues.WIDTH;
                    int z = (index / ChunkValues.WIDTH) % ChunkValues.LENGTH;
                    int y = (index / (ChunkValues.WIDTH * ChunkValues.LENGTH)) % ChunkValues.HEIGHT;

                    int rightIndex, leftIndex, frontIndex, backIndex, topIndex, bottomIndex;

                    // WIDTH
                    if (x == ChunkValues.WIDTH - 1)
                    {
                        chunkDictionary.TryGetValue(new int2(chunkValues.pos.x + 1, chunkValues.pos.y), out ChunkValues adjacentChunk);

                        if (adjacentChunk.blocks.Length == 0)
                        {
                            GenerateRightFace(values, currentBlock, x, y, z);

                        }
                        else if (adjacentChunk.blocks.Length > 0)
                        {
                            int j = Block.GetFlatIndex(0, y, z);

                            int adjacentBlockID = adjacentChunk.blocks[j];
                            if (adjacentBlockID <= 0 || Block.CheckTransparentBlockPlacement(currentBlock, possbleBlocks[adjacentBlockID - 1]))
                            {
                                GenerateRightFace(values, currentBlock, x, y, z);
                            }
                        }
                    }
                    if (x >= 0 && x < ChunkValues.WIDTH - 1)
                    {
                        rightIndex = Block.GetFlatIndex(x + 1, y, z);

                        int adjacentBlockID = chunkValues.blocks[rightIndex];
                        if (adjacentBlockID <= 0 || Block.CheckTransparentBlockPlacement(currentBlock, possbleBlocks[adjacentBlockID - 1]))
                        {
                            GenerateRightFace(values, currentBlock, x, y, z);
                        }
                    }

                    if (x == 0)
                    {
                        chunkDictionary.TryGetValue(new int2(chunkValues.pos.x - 1, chunkValues.pos.y), out ChunkValues adjacentChunk);

                        if (adjacentChunk.blocks.Length == 0)
                        {
                            GenerateLeftFace(values, currentBlock, x, y, z);

                        }
                        else if (adjacentChunk.blocks.Length > 0)
                        {
                            int j = Block.GetFlatIndex(ChunkValues.WIDTH - 1, y, z);

                            int adjacentBlockID = adjacentChunk.blocks[j];
                            if (adjacentBlockID <= 0 || Block.CheckTransparentBlockPlacement(currentBlock, possbleBlocks[adjacentBlockID - 1]))
                            {
                                GenerateLeftFace(values, currentBlock, x, y, z);
                            }
                        }

                    }

                    if (x > 0 && x <= ChunkValues.WIDTH - 1)
                    {
                        leftIndex = Block.GetFlatIndex(x - 1, y, z);

                        int adjacentBlockID = chunkValues.blocks[leftIndex];
                        if (adjacentBlockID <= 0 || Block.CheckTransparentBlockPlacement(currentBlock, possbleBlocks[adjacentBlockID - 1]))
                        {
                            GenerateLeftFace(values, currentBlock, x, y, z);
                        }
                    }

                    // LENGTH
                    if (z == ChunkValues.LENGTH - 1)
                    {
                        chunkDictionary.TryGetValue(new int2(chunkValues.pos.x, chunkValues.pos.y + 1), out ChunkValues adjacentChunk);

                        if (adjacentChunk.blocks.Length == 0)
                        {
                            GenerateFrontFace(values, currentBlock, x, y, z);
                        }

                        else if (adjacentChunk.blocks.Length > 0)
                        {

                            int j = Block.GetFlatIndex(x, y, 0);

                            int adjacentBlockID = adjacentChunk.blocks[j];
                            if (adjacentBlockID <= 0 || Block.CheckTransparentBlockPlacement(currentBlock, possbleBlocks[adjacentBlockID - 1]))
                            {
                                GenerateFrontFace(values, currentBlock, x, y, z);
                            }
                        }

                    }

                    if (z >= 0 && z < ChunkValues.LENGTH - 1)
                    {
                        frontIndex = Block.GetFlatIndex(x, y, z + 1);

                        int adjacentBlockID = chunkValues.blocks[frontIndex];
                        if (adjacentBlockID <= 0 || Block.CheckTransparentBlockPlacement(currentBlock, possbleBlocks[adjacentBlockID - 1]))
                        {
                            GenerateFrontFace(values, currentBlock, x, y, z);
                        }
                    }

                    if (z == 0)
                    {
                        chunkDictionary.TryGetValue(new int2(chunkValues.pos.x, chunkValues.pos.y - 1), out ChunkValues adjacentChunk);

                        if (adjacentChunk.blocks.Length == 0)
                        {
                            GenerateBackFace(values, currentBlock, x, y, z);
                        }

                        else if (adjacentChunk.blocks.Length > 0)
                        {
                            int j = Block.GetFlatIndex(x, y, ChunkValues.LENGTH - 1);

                            int adjacentBlockID = adjacentChunk.blocks[j];
                            if (adjacentBlockID <= 0 || Block.CheckTransparentBlockPlacement(currentBlock, possbleBlocks[adjacentBlockID - 1]))
                            {
                                GenerateBackFace(values, currentBlock, x, y, z);

                            }
                        }

                    }

                    if (z > 0 && z <= ChunkValues.LENGTH - 1)
                    {
                        backIndex = Block.GetFlatIndex(x, y, z - 1);

                        int adjacentBlockID = chunkValues.blocks[backIndex];
                        if (adjacentBlockID <= 0 || Block.CheckTransparentBlockPlacement(currentBlock, possbleBlocks[adjacentBlockID - 1]))
                        {
                            GenerateBackFace(values, currentBlock, x, y, z);

                        }
                    }

                    // HEIGHT
                    if (y >= 0 && y < ChunkValues.HEIGHT - 1)
                    {
                        topIndex = Block.GetFlatIndex(x, y + 1, z);

                        int adjacentBlockID = chunkValues.blocks[topIndex];
                        if (adjacentBlockID <= 0 || Block.CheckTransparentBlockPlacement(currentBlock, possbleBlocks[adjacentBlockID - 1]))
                        {
                            GenerateTopFace(values, currentBlock, x, y, z);

                        }
                    }

                    if (y > 0 && y <= ChunkValues.HEIGHT - 1)
                    {
                        bottomIndex = Block.GetFlatIndex(x, y - 1, z);

                        int adjacentBlockID = chunkValues.blocks[bottomIndex];
                        if (adjacentBlockID <= 0 || Block.CheckTransparentBlockPlacement(currentBlock, possbleBlocks[adjacentBlockID - 1]))
                        {
                            GenerateBottomFace(values, currentBlock, x, y, z);
                        }
                    }
                }
            }

            chunkValues.terrainMeshValues = values;
            return chunkValues;
        }

        [BurstCompile]
        public static ChunkValues GenerateMeshValuesWater(ChunkValues chunkValues, NativeParallelHashMap<int2, ChunkValues> chunkDictionary)
        {
            MeshValues values = chunkValues.waterMeshValues;

            values.verts.Clear();
            values.tris.Clear();
            values.UVs.Clear();

            for(int index = 0; index < chunkValues.blocks.Length; index++)
            {
                int i = chunkValues.blocks[index];
                if (i < 0)
                {
                    int x = index % ChunkValues.WIDTH;
                    int z = (index / ChunkValues.WIDTH) % ChunkValues.LENGTH;
                    int y = (index / (ChunkValues.WIDTH * ChunkValues.LENGTH)) % ChunkValues.HEIGHT;

                    int rightIndex, leftIndex, frontIndex, backIndex, topIndex, bottomIndex;

                    // WIDTH
                    if (x == ChunkValues.WIDTH - 1)
                    {
                        if (chunkDictionary.TryGetValue(new int2(chunkValues.pos.x + 1, chunkValues.pos.y), out ChunkValues adjacentChunk))
                        {
                            if(adjacentChunk.blocks.Length > 0)
                            {
                                int j = Block.GetFlatIndex(0, y, z);
                                if (adjacentChunk.blocks[j] == 0)
                                {
                                    int offset = values.verts.Length;
                                    Voxel_Verts_Water.RightFace(ref values.verts, x, y, z);
                                    Voxel_Tris.GenerateTris(ref values.tris, offset);
                                    Voxel_UVs.GetUVs(ref values.UVs, 0, 0, 1);
                                }
                            }
                        }
                    }
                    if (x >= 0 && x < ChunkValues.WIDTH - 1)
                    {
                        rightIndex = Block.GetFlatIndex(x + 1, y, z);

                        if (chunkValues.blocks[rightIndex] == 0)
                        {
                            int offset = values.verts.Length;
                            Voxel_Verts_Water.RightFace(ref values.verts, x, y, z);
                            Voxel_Tris.GenerateTris(ref values.tris, offset);
                            Voxel_UVs.GetUVs(ref values.UVs, 0, 0, 1);
                        }
                    }

                    if (x == 0)
                    {
                        if (chunkDictionary.TryGetValue(new int2(chunkValues.pos.x - 1, chunkValues.pos.y), out ChunkValues adjacentChunk))
                        {
                            if(adjacentChunk.blocks.Length > 0)
                            {

                                int j = Block.GetFlatIndex(ChunkValues.WIDTH - 1, y, z);
                                if (adjacentChunk.blocks[j] == 0)
                                {
                                    int offset = values.verts.Length;
                                    Voxel_Verts_Water.LeftFace(ref values.verts, x, y, z);
                                    Voxel_Tris.GenerateTris(ref values.tris, offset);
                                    Voxel_UVs.GetUVs(ref values.UVs, 0, 0, 1);
                                }
                            }
                        }
                    }

                    if (x > 0 && x <= ChunkValues.WIDTH - 1)
                    {
                        leftIndex = Block.GetFlatIndex(x - 1, y, z);

                        if (chunkValues.blocks[leftIndex] == 0)
                        {
                            int offset = values.verts.Length;
                            Voxel_Verts_Water.LeftFace(ref values.verts, x, y, z);
                            Voxel_Tris.GenerateTris(ref values.tris, offset);
                            Voxel_UVs.GetUVs(ref values.UVs, 0, 0, 1);
                        }
                    }

                    // LENGTH
                    if (z == ChunkValues.LENGTH - 1)
                    {
                        if (chunkDictionary.TryGetValue(new int2(chunkValues.pos.x, chunkValues.pos.y + 1), out ChunkValues adjacentChunk))
                        {
                            if(adjacentChunk.blocks.Length > 0)
                            {
                                int j = Block.GetFlatIndex(x, y, 0);
                                if (adjacentChunk.blocks[j] == 0)
                                {
                                    int offset = values.verts.Length;
                                    Voxel_Verts_Water.FrontFace(ref values.verts, x, y, z);
                                    Voxel_Tris.GenerateTris(ref values.tris, offset);
                                    Voxel_UVs.GetUVs(ref values.UVs, 0, 0, 1);
                                }
                            }

                        }
                    }

                    if (z >= 0 && z < ChunkValues.LENGTH - 1)
                    {
                        frontIndex = Block.GetFlatIndex(x, y, z + 1);

                        if (chunkValues.blocks[frontIndex] == 0)
                        {
                            int offset = values.verts.Length;
                            Voxel_Verts_Water.FrontFace(ref values.verts, x, y, z);
                            Voxel_Tris.GenerateTris(ref values.tris, offset);
                            Voxel_UVs.GetUVs(ref values.UVs, 0, 0, 1);
                        }
                    }

                    if (z == 0)
                    {
                        if (chunkDictionary.TryGetValue(new int2(chunkValues.pos.x, chunkValues.pos.y - 1), out ChunkValues adjacentChunk))
                        {
                            if(adjacentChunk.blocks.Length > 0)
                            {
                                int j = Block.GetFlatIndex(x, y, ChunkValues.LENGTH - 1);
                                if (adjacentChunk.blocks[j] == 0)
                                {
                                    int offset = values.verts.Length;
                                    Voxel_Verts_Water.BackFace(ref values.verts, x, y, z);
                                    Voxel_Tris.GenerateTris(ref values.tris, offset);
                                    Voxel_UVs.GetUVs(ref values.UVs, 0, 0, 1);
                                }
                            }
                        }
                    }

                    if (z > 0 && z <= ChunkValues.LENGTH - 1)
                    {
                        backIndex = Block.GetFlatIndex(x, y, z - 1);

                        if (chunkValues.blocks[backIndex] == 0)
                        {
                            int offset = values.verts.Length;
                            Voxel_Verts_Water.BackFace(ref values.verts, x, y, z);
                            Voxel_Tris.GenerateTris(ref values.tris, offset);
                            Voxel_UVs.GetUVs(ref values.UVs, 0, 0, 1);
                        }
                    }

                    // HEIGHT
                    if (y >= 0 && y < ChunkValues.HEIGHT)
                    {
                        topIndex = Block.GetFlatIndex(x, y + 1, z);

                        // Don't draw face ONLY if there's a water block on top
                        // We want to draw faces when there's a solid block above due to the offset of water
                        if (chunkValues.blocks[topIndex] == 0 || chunkValues.blocks[topIndex] > 0 && y == WorldGenConstants.WATER_LEVEL)
                        {
                            int offset = values.verts.Length;
                            Voxel_Verts_Water.TopFace(ref values.verts, x, y, z);
                            Voxel_Tris.GenerateTris(ref values.tris, offset);
                            Voxel_UVs.GetUVs(ref values.UVs, 0, 0, 1);
                        }
                    }

                    if (y > 0 && y <= ChunkValues.HEIGHT)
                    {
                        bottomIndex = Block.GetFlatIndex(x, y - 1, z);

                        if (chunkValues.blocks[bottomIndex] == 0)
                        {
                            int offset = values.verts.Length;
                            Voxel_Verts_Water.BottomFace(ref values.verts, x, y, z);
                            Voxel_Tris.GenerateTris(ref values.tris, offset);
                            Voxel_UVs.GetUVs(ref values.UVs, 0, 0, 1);
                        }
                    }
                }
            }

            chunkValues.waterMeshValues = values;
            return chunkValues;
        }
    }
}