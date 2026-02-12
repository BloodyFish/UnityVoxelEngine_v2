#include "Noise.h"
#include "FastNoiseLite.h"

FastNoiseLite Noise::noise;

void Noise::Init(int seed, float frequency, int octaves, float lacunarity, float gain) {
	noise.SetNoiseType(FastNoiseLite::NoiseType_OpenSimplex2);
	noise.SetSeed(seed);
	noise.SetFrequency(frequency);

	noise.SetFractalType(FastNoiseLite::FractalType_FBm);
	noise.SetFractalOctaves(octaves);
	noise.SetFractalLacunarity(lacunarity);
	noise.SetFractalGain(gain);
}

extern "C" {
	NOISE_API void NoiseInit(int seed, float frequency, int octaves, float lacunarity, float gain) {
		Noise::Init(seed, frequency, octaves, lacunarity, gain);
	}
}