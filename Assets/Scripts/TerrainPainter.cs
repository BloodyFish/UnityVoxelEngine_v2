using System;
using System.Runtime.CompilerServices;

namespace BloodyFish.UnityVoxelEngine.v2
{
    public class TerrainPainter
    {
        public static void Paint(Chunk chunk, int[] blocks, Random random)
        {

            int index = 0;
            foreach (var i in blocks)
            {
                int x = index % Chunk.Width;
                int z = (index / Chunk.Width) % Chunk.Length;
                int y = (index / (Chunk.Width * Chunk.Length)) % Chunk.Height;

                PaintTerrain(random, i, x, y, z, blocks, chunk);
                FillWater(i, x, y, z, chunk);

                index++;
            }
        }

        private static void PaintTerrain(Random random, int i, int x, int y, int z, int[] blocks, Chunk chunk)
        {
            if (i > 0)
            {
                if (y >= Generation.SCALE - random.Next(60, 75))
                {
                    chunk.SetBlock(Block.SNOW, x, y, z);
                }

                //else if (y >= Generation.SCALE - random.Next(90, 100) || Block.GetSlopeOfBlock(x, y, z, chunk) >= 1f) { Block.SetBlock(Block.STONE, x, y, z, chunk); }
                else if (y >= Generation.SCALE - random.Next(90, 100)){ chunk.SetBlock(Block.STONE, x, y, z); }

                else if (y > (Generation.WATER_LEVEL + Generation.BEACH_HEIGHT) - random.Next(1, 3))
                {
                    if (chunk.GetBlock(x, y + 1, z) == 0) { chunk.SetBlock(Block.GRASS, x, y, z); }
                    else { chunk.SetBlock(Block.DIRT, x, y, z); }
                }

                else
                {
                    chunk.SetBlock(Block.SAND, x, y, z);
                }

            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void FillWater(int i, int x, int y, int z, Chunk chunk)
        {
            if (y <= Generation.WATER_LEVEL && i == 0)
            {
               chunk.SetBlock(-1, x, y, z);
            }
        }
    }
}