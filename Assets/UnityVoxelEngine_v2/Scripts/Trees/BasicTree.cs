using UnityEngine;

namespace BloodyFish.UnityVoxelEngine.v2
{
    [CreateAssetMenu(menuName = "Trees/BasicTree")]
    public class BasicTree : BloodyFish.UnityVoxelEngine.v2.Tree
    {
        public static void GenerateCanopy(Vector3Int pos, Chunk chunk, int[] blocks, Unity.Mathematics.Random random)
        {
            //int canopyHeight = random.NextInt(minCanopyHeight, maxCanopyHeight + 1);

            //int i = 0;
            //for (int y = 0; y < canopyHeight; y++)
            //{
            //    if (y == 0 || y == canopyHeight - 1) i = 1;
            //    else i = 0;

            //    for (int x = -(canopyOverhang - i); x <= (canopyOverhang - i); x++)
            //    {
            //        for (int z = -(canopyOverhang - i); z <= (canopyOverhang - i); z++)
            //        {
            //            /*if (x == -canopyOverhang && z == -canopyOverhang) continue;
            //            if (x == -canopyOverhang && z == canopyOverhang) continue;
            //            if (x == canopyOverhang && z == canopyOverhang) continue;
            //            if (x == canopyOverhang && z == -canopyOverhang) continue;*/

            //            if (Mathf.Abs(x) == canopyOverhang && Mathf.Abs(z) == canopyOverhang) continue;

            //            int m_x = x + pos.x;
            //            int m_y = y + pos.y;
            //            int m_z = z + pos.z;

            //            //if (m_x < 0 || m_x >= Chunk.Width || m_z < 0 || m_z >= Chunk.Length || m_y >= Chunk.Height) continue;
            //            if (m_y >= Chunk.Height) continue;

            //            if (chunk.GetBlock(m_x, m_y, m_z) == 0)
            //            {
            //                chunk.SetBlock(leafBlock.blockID, m_x, m_y, m_z);
            //            }
            //        }
            //    }
            //}
        }
    }
}
