using System;
using System.Linq;
using UnityEngine;

namespace BloodyFish.UnityVoxelEngine.v2
{
    public class TreeGenerator
    {
        public static void PlantTrees(Tree tree, Chunk chunk, int[] blocks, System.Random random)
        {
            for (int i = 0; i < Chunk.Width * Chunk.Length; i++)
            {
                int x = i % Chunk.Width;
                int z = (i / Chunk.Width) % Chunk.Length;

                for (int y = Chunk.Height - 1; y > Generation.WATER_LEVEL; y--)
                {
                    int currentBlockID = chunk.GetBlock(x, y, z);

                    if (currentBlockID > 0)
                    {
                        Block currentBlock = Block.possibleBlocks[currentBlockID - 1];

                        if (currentBlock.canGrowTree && random.Next(0, 150) == 1 && !Block.GetNeighboringBlocks(x, y + 1, z, blocks).Contains(Block.WOOD))
                        {
                            // While we are here, we might as well make it so that trees growing on grass blocks replace that block with dirt
                            if (currentBlockID == Block.GRASS) chunk.SetBlock(Block.DIRT, x, y, z);

                            tree.GenerateTrunk(new Vector3Int(x, y, z), chunk, blocks, random, out int height);
                            tree.GenerateCanopy(new Vector3Int(x, y + 1 + height, z), chunk, blocks, random);
                        }

                        break;
                    }
                }
            }
        }
    }
}
