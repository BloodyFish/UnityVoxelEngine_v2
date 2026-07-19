using Unity.Mathematics;
using Unity.Burst;

namespace BloodyFish.UnityVoxelEngine.v2
{
    [System.Serializable]
    public struct NoiseParameters
    {
        public float frequency;
        public int octaves;
        public float lacunarity;
        public float gain;
    }

    public class NoiseGen
    {
        [BurstCompile]
        public static float GetNoise(int2 worldSpacePos, float3 seedOffset, int x, int z, NoiseParameters noiseParams)
        {
            float xOffset = worldSpacePos.x + seedOffset.x;
            float zOffset = worldSpacePos.y + seedOffset.z;

            float noiseX = x + xOffset;
            float noiseZ = z + zOffset;


            float noiseVal = 0;

            float frequency = noiseParams.frequency;
            float lacunarity = noiseParams.lacunarity;
            float octaves = noiseParams.octaves;
            float gain = noiseParams.gain;
            float amplitude = 1;


            float maxAmplitude = 0;
            for (int i = 0; i < octaves; i++)
            {
                noiseVal += Unity.Mathematics.noise.snoise(new float2(noiseX, noiseZ) * (frequency / 1000)) * amplitude;
                maxAmplitude += amplitude;
                frequency *= lacunarity;
                amplitude *= gain;
            }
            noiseVal /= maxAmplitude;
            return noiseVal;
        }


        [BurstCompile]
        public static float GetNoise(int2 worldSpacePos, float3 seedOffset, int x, int y, int z, NoiseParameters noiseParams)
        {
            float xOffset = worldSpacePos.x + seedOffset.x;
            float zOffset = worldSpacePos.y + seedOffset.z;

            float noiseX = x + xOffset;
            float noiseY = y + seedOffset.y;
            float noiseZ = z + zOffset;


            float noiseVal = 0;

            float frequency = noiseParams.frequency;
            float lacunarity = noiseParams.lacunarity;
            float octaves = noiseParams.octaves;
            float gain = noiseParams.gain;
            float amplitude = 1;


            float maxAmplitude = 0;
            for (int i = 0; i < octaves; i++)
            {
                noiseVal += Unity.Mathematics.noise.snoise(new float3(noiseX, noiseY, noiseZ) * (frequency / 1000)) * amplitude;
                maxAmplitude += amplitude;
                frequency *= lacunarity;
                amplitude *= gain;
            }
            noiseVal /= maxAmplitude;
            return noiseVal; 

        }
    }
}
