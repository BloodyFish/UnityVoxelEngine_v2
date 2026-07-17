using BloodyFish.UnityVoxelEngine.v2;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
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
        public static BiomeParameters GetBiome(float noiseX, float noiseY, float noiseZ, NoiseParameters temperatureParam, NoiseParameters precipitationParam, NativeArray<BiomeParameters> biomeParams)
        {
            BiomeParameters biome = biomeParams[0];

            float temperatureNoise = Unity.Mathematics.math.remap(-1, 1, -15, 35, NoiseGen.GetNoise(noiseX, noiseY, noiseZ, temperatureParam));
            float precipitationNoise = Unity.Mathematics.math.remap(-1, 1, 0, 175, NoiseGen.GetNoise(noiseX, noiseY, noiseZ, precipitationParam));

            foreach (BiomeParameters biomeParam in biomeParams)
            {
                if ((temperatureNoise >= biomeParam.minTemp && temperatureNoise <= biomeParam.maxTemp)
                && (precipitationNoise >= biomeParam.minPreciptation && precipitationNoise <= biomeParam.maxPreciptation))
                {
                    biome = biomeParam;
                    break;
                }
            }


            return biome;
        }
    }

}

