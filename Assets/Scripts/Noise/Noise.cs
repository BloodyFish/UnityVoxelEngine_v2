using Unity.Mathematics;

public class Noise
{
    public static FastNoiseLite noise2D;
    public static FastNoiseLite noise3D;

    private static void Init(FastNoiseLite noise, int seed, float frequency, int octaves, float lacunarity, float gain)
    {
        noise = new FastNoiseLite(seed);
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        noise.SetFrequency(frequency);

        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        noise.SetFractalOctaves(octaves);
        noise.SetFractalLacunarity(lacunarity);
        noise.SetFractalGain(gain);
    }

    public static void Init2D(int seed, float frequency, int octaves, float lacunarity, float gain)
    {
        //Init(noise2D, seed, frequency, octaves, lacunarity, gain);
        noise2D = new FastNoiseLite(seed);
        noise2D.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        noise2D.SetFrequency(frequency);

        noise2D.SetFractalType(FastNoiseLite.FractalType.FBm);
        noise2D.SetFractalOctaves(octaves);
        noise2D.SetFractalLacunarity(lacunarity);
        noise2D.SetFractalGain(gain);
    }

    public static void Init3D(int seed, float frequency, int octaves, float lacunarity, float gain)
    {
        //Init(noise3D, seed, frequency, octaves, lacunarity, gain);
        noise3D = new FastNoiseLite(seed);
        noise3D.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        noise3D.SetFrequency(frequency);

        noise3D.SetFractalType(FastNoiseLite.FractalType.FBm);
        noise3D.SetFractalOctaves(octaves);
        noise3D.SetFractalLacunarity(lacunarity);
        noise3D.SetFractalGain(gain);
    }
}
