using System;
using System.Linq;
using UnityEngine;

public class TreeGenerator
{
    public static void PlantTrees(Tree tree, int[] blocks, System.Random random)
    {
        for(int i = 0; i < Chunk.Width * Chunk.Length; i++)
        {
            int x = i % Chunk.Width;
            int z = (i / Chunk.Width) % Chunk.Length;

            for (int y = Chunk.Height - 1; y > Generation.WATER_LEVEL; y--)
            {
                int currentBlockID = blocks[Block.GetFlatIndex(x, y, z)];

                if (currentBlockID > 0)
                {
                    Block currentBlock = Block.possibleBlocks[currentBlockID - 1];

                    if (currentBlock.canGrowTree && random.Next(0, 150) == 1 && !Block.GetNeighboringBlocks(x, y + 1, z, blocks).Contains(Block.WOOD))
                    {
                        // While we are here, we might as well make it so that trees growing on grass blocks replace that block with dirt
                        if (currentBlockID == Block.GRASS) blocks[Block.GetFlatIndex(x, y, z)] = Block.DIRT;

                        tree.GenerateTrunk(new Vector3Int(x, y, z), blocks, random, out int height);
                        tree.GenerateCanopy(new Vector3Int(x, y + 1 + height, z), blocks, random);
                    }

                    break;
                }
            }
        }
    }
}
