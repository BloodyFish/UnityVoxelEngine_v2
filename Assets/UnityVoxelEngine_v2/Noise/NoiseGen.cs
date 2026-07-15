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
        public static float GetNoise(float noiseX, float noiseZ, NoiseParameters noiseParams)
        {
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
        public static float GetNoise(float noiseX, float noiseY, float noiseZ, NoiseParameters noiseParams)
        {
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
