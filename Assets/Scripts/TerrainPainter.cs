using System;
using System.Runtime.CompilerServices;

namespace BloodyFish.UnityVoxelEngine.V2
{
    public class TerrainPainter
    {
        public static void Paint(int[] blocks, Random random)
        {
            // Since .NET 4.8 doesn't support the thread safe version of System.Random (System.Random.Shared)
            // we have to make a new Random for every thread

            int index = 0;
            foreach (var i in blocks)
            {
                int x = index % Chunk.Width;
                int z = (index / Chunk.Width) % Chunk.Length;
                int y = (index / (Chunk.Width * Chunk.Length)) % Chunk.Height;

                PaintTerrain(random, i, x, y, z, index, blocks);
                FillWater(i, y, index, blocks);

                index++;
            }
        }

        private static void PaintTerrain(Random random, int i, int x, int y, int z, int index, int[] blocks)
        {
            if (i > 0)
            {
                if (y >= Generation.SCALE - random.Next(60, 75))
                {
                    blocks[index] = Block.SNOW;
                }

                else if (y >= Generation.SCALE - random.Next(90, 100) || Block.GetSlopeOfBlock(x, y, z, blocks) >= 2.5f) { blocks[index] = Block.STONE; }

                else if (y > (Generation.WATER_LEVEL + Generation.BEACH_HEIGHT) - random.Next(1, 3))
                {
                    if (blocks[Block.GetFlatIndex(x, y + 1, z)] == 0) { blocks[index] = Block.GRASS; }
                    else { blocks[index] = Block.DIRT; }
                }

                else
                {
                    blocks[index] = Block.SAND;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void FillWater(int i, int y, int index, int[] blocks)
        {
            if (y <= Generation.WATER_LEVEL && i == 0)
            {
                blocks[index] = -1;
            }
        }
    }
}