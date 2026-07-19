using BloodyFish.UnityVoxelEngine.v2;
using System.Collections.Generic;
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
        public short treeStemBlockID;
        public short treeLeafBlockID;

        public float minTemp, maxTemp;
        public float minPreciptation, maxPreciptation;
        public int treeDensity;
        public NativeArray<TreeValues> treeValsArray;
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
        public int treeDenisty;

        public List<BloodyFish.UnityVoxelEngine.v2.Tree> trees;
        //public BloodyFish.UnityVoxelEngine.v2.Tree tree;


        public static BiomeParameters CreateNewBiomeParameter(Biome biome)
        {
            NativeArray<TreeValues> treeValsArray = new NativeArray<TreeValues>(biome.trees.Count, Allocator.Persistent);

            int index = 0;
            foreach(BloodyFish.UnityVoxelEngine.v2.Tree tree in biome.trees)
            {
                treeValsArray[index++] = new TreeValues
                {
                    trunkBlockID = tree.trunkBlock.blockID,
                    leafBlockID = tree.leafBlock.blockID,
                    minHeight = tree.minHeight,
                    maxHeight = tree.maxHeight,
                    canopyOverhang = tree.canopyOverhang,
                    minCanopyHeight = tree.minCanopyHeight,
                    maxCanopyHeight = tree.maxCanopyHeight
                };
            }

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
                maxPreciptation = biome.maxPreciptation,
                treeDensity = biome.treeDenisty,

                treeValsArray = treeValsArray,
            };
        }

        [BurstCompile]
        public static short GetBiome(int2 worldSpacePos, float3 seedOffset, int x, int y, int z, NoiseParameters temperatureParam, NoiseParameters precipitationParam, NativeArray<BiomeParameters> biomeParams)
        {
            short biomeID = 0;

            float temperatureNoise = Unity.Mathematics.math.remap(-1, 1, -15, 35, NoiseGen.GetNoise(worldSpacePos, seedOffset, x, y, z, temperatureParam));
            float precipitationNoise = Unity.Mathematics.math.remap(-1, 1, 0, 175, NoiseGen.GetNoise(worldSpacePos, seedOffset, x, y, z, precipitationParam));

            short i = 0;
            foreach (BiomeParameters biomeParam in biomeParams)
            {
                if ((temperatureNoise >= biomeParam.minTemp && temperatureNoise <= biomeParam.maxTemp)
                && (precipitationNoise >= biomeParam.minPreciptation && precipitationNoise <= biomeParam.maxPreciptation))
                {
                    biomeID = i;
                    break;
                }
                i++;
            }

            return biomeID;
        }
    }

}

