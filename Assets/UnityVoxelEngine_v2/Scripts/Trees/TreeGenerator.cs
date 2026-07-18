using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace BloodyFish.UnityVoxelEngine.v2
{
    public class TreeGenerator
    {

        [BurstCompile]
        public static JobHandle PlantTrees(int2 chunkPos, ref NativeArray<short> blocks, ref Unity.Mathematics.Random random, JobHandle dependency, out TreeGenJob treeGenJob)
        {
            treeGenJob = new TreeGenJob()
            {
                minHeight = GenerationManager.defaultTree.minHeight,
                maxHeight = GenerationManager.defaultTree.maxHeight,
                trunkBlockID = GenerationManager.defaultTree.trunkBlock.blockID,
                leafBlockID = GenerationManager.defaultTree.leafBlock.blockID,
                canopyOverhang = GenerationManager.defaultTree.canopyOverhang,
                minCanopyHeight = GenerationManager.defaultTree.minCanopyHeight,
                maxCanopyHeight = GenerationManager.defaultTree.maxCanopyHeight,
                blocks = blocks,
                possibleBlocks = Block.possibleBlocks,
                chunkDictionary = GenerationManager.chunkDictionary,
                bufferDictionary = GenerationManager.bufferDictionary,
                random = random,
                chunkPos = chunkPos
            };
            
            int size = ChunkValues.WIDTH * ChunkValues.LENGTH * ChunkValues.HEIGHT;
            JobHandle treeJobHandle = treeGenJob.ScheduleParallel(size, GenerationManager.GetGoodBatchSize(size), dependency);
            return treeJobHandle;
        }

    }

    [BurstCompile]
    public struct TreeGenJob: IJobParallelForBatch
    {
        public int minHeight;
        public int maxHeight;

        public short trunkBlockID;
        public short leafBlockID;

        public int canopyOverhang;
        public int minCanopyHeight;
        public int maxCanopyHeight;

        [NativeDisableParallelForRestriction]
        public NativeArray<short> blocks;

        [ReadOnly]
        public NativeArray<BlockData> possibleBlocks;

        [NativeDisableContainerSafetyRestriction]
        public NativeParallelHashMap<int2, BlockBufferValues> bufferDictionary;

        [ReadOnly]
        [NativeDisableContainerSafetyRestriction]
        public NativeParallelHashMap<int2, ChunkValues> chunkDictionary;

        public Unity.Mathematics.Random random;

        [ReadOnly]
        public int2 chunkPos;

        public void Execute(int startIndex, int count)
        {
            for(int i = startIndex; i < startIndex + count; i++ ) 
            {
                // We can check every other block
                if (i % 2 == 0)
                {
                    int x = i % ChunkValues.WIDTH;
                    int z = (i / ChunkValues.WIDTH) % ChunkValues.LENGTH;
                    int y = (i / (ChunkValues.WIDTH * ChunkValues.LENGTH)) % ChunkValues.HEIGHT;

                    if (y < ChunkValues.HEIGHT - 1 && y > WorldGenConstants.WATER_LEVEL)
                    {
                        int3 blockPos = new int3(x, y, z);
                        int currentBlockID = Chunk.GetBlock(chunkPos, blockPos, blocks, bufferDictionary, chunkDictionary);

                        if (currentBlockID > 0)
                        {
                            BlockData currentBlock = possibleBlocks[currentBlockID - 1];

                            if (currentBlock.canGrowTree && random.NextInt(0, 1000) == 1 && !Block.GetNeighboringBlocks(x, y + 1, z, blocks).Contains(Block.WOOD))
                            {
                                // While we are here, we might as well make it so that trees growing on grass blocks replace that block with dirt
                                if (currentBlockID == Block.GRASS) Chunk.SetBlock(Block.DIRT, chunkPos, blockPos, blocks, bufferDictionary, chunkDictionary);

                                Tree.GenerateTrunk(minHeight, maxHeight, trunkBlockID, chunkPos, blockPos, blocks, bufferDictionary, chunkDictionary, ref random, out int height);
                                Tree.GenerateCanopy(canopyOverhang, minCanopyHeight, maxCanopyHeight, leafBlockID, chunkPos, new int3(x, y + height, z), blocks, bufferDictionary, chunkDictionary, ref random);
                            }
                        }
                    }
                }
            }
        }
    }
}
