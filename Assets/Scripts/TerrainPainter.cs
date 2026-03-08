public class TerrainPainter
{
    public static void Paint(int[] blocks)
    {
        int index = 0;
        foreach (var i in blocks)
        {
            if (i > 0)
            {
                int x = index % Chunk.Width;
                int z = (index / Chunk.Width) % Chunk.Length;
                int y = (index / (Chunk.Width * Chunk.Length)) % Chunk.Height;

                if (y >= Generation.SCALE - Generation.random.Next(60, 75))
                {
                    blocks[index] = Block.SNOW;
                }

                else if (y >= Generation.SCALE - Generation.random.Next(90, 100) || Block.GetSlopeOfBlock(x, y, z, blocks) >= 2.5f) { blocks[index] = Block.STONE; }

                else if (y > (Generation.waterLevel + Generation.beachHeight) - Generation.random.Next(1, 3))
                {
                    if (blocks[Chunk.GetFlatIndex(x, y + 1, z)] == 0) { blocks[index] = Block.GRASS; }
                    else { blocks[index] = Block.DIRT; }
                }

                else
                {
                    blocks[index] = Block.SAND;
                }
            }

            index++;
        }
    }
}
