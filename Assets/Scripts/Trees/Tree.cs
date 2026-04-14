using UnityEngine;

namespace BloodyFish.UnityVoxelEngine.V2
{
   public class Tree : ScriptableObject
    {
        public Block trunkBlock;
        public Block leafBlock;

        public int minHeight;
        public int maxHeight;

        public int canopyOverhang;

        public int minCanopyHeight;
        public int maxCanopyHeight;

        public void GenerateTrunk(Vector3Int pos, int[] blocks, System.Random random, out int height)
        {
            height = random.Next(minHeight, maxHeight + 1);
            for (int h = 0; h < height; h++)
            {
                int index = Block.GetFlatIndex(pos.x, pos.y + 1 + h, pos.z);
                if (blocks[index] == 0) blocks[index] = trunkBlock.blockID;
            }
        }

        // Virtual so that different trees can generate different types of canopy
        public virtual void GenerateCanopy(Vector3Int pos, int[] blocks, System.Random random) { }
    }
}
