using System.Runtime.CompilerServices;
using BloodyFish.UnityVoxelEngine.v2;
using Unity.Burst;
using UnityEngine;


namespace BloodyFish.UnityVoxelEngine
{
    // We need a struct to represent Biomes
    // Structs are burst compatable
    public struct BiomeParameters
    {
        public short topBlockID;
        public short middleBlockID;
        public short stoneBlockID;
        public short snowBlockID;
        public short beachBlockID;

        public float minTemp, maxTemp;
        public float minPreciptation, maxPreciptation;
    }

    [CreateAssetMenu]
    public class Biome : ScriptableObject
    {
        public Block topBlock;
        public Block middleBlock;
        public Block stoneBlock;
        public Block snowBlock;
        public Block beachBlock;

        public float minTemp, maxTemp;
        public float minPreciptation, maxPreciptation;

        public static BiomeParameters CreateNewBiomeParameter(Biome biome)
        {            
            return new BiomeParameters
            {
                topBlockID = biome.topBlock.blockID,
                middleBlockID = biome.middleBlock.blockID,
                stoneBlockID = biome.stoneBlock.blockID,
                snowBlockID = biome.snowBlock.blockID,
                beachBlockID = biome.beachBlock.blockID,


                minTemp = biome.minTemp,
                maxTemp = biome.maxTemp,

                minPreciptation = biome.minPreciptation,
                maxPreciptation = biome.maxPreciptation
            };
        }

        [BurstCompile]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float AdjustBiomeNoiseVal(float normalizedNoiseVal, int a, int b)
        {
            // tranform to fit into a to b
            // a + (((x - xmin) * (b - a)) / (xmax - xmin))

            // our normalizedNoiseValue (-1 to 1) will be transformed to fit a to b
            float newNoiseVal = a + (((normalizedNoiseVal - (-1)) * (b - a)) / (1 - (-1)));

            return newNoiseVal;

        }
    }

}

