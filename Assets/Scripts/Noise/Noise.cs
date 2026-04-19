public class Noise
{
    public static FastNoiseLite noise2D = new FastNoiseLite();
    public static FastNoiseLite noise3D = new FastNoiseLite();

    private static void Init(FastNoiseLite noise,int seed, float frequency, int octaves, float lacunarity, float gain)
    {
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        noise.SetSeed(seed);
        noise.SetFrequency(frequency);

        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        noise.SetFractalOctaves(octaves);
        noise.SetFractalLacunarity(lacunarity);
        noise.SetFractalGain(gain);
    }

    public static void Init2D(int seed, float frequency, int octaves, float lacunarity, float gain)
    {
        Init(noise2D, seed, frequency, octaves, lacunarity, gain);
    }

    public static void Init3D(int seed, float frequency, int octaves, float lacunarity, float gain)
    {
        Init(noise3D, seed, frequency, octaves, lacunarity, gain);
    }
}
