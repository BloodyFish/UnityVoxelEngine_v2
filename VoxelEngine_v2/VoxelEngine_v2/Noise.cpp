#include "Noise.h"
#include "FastNoiseLite.h"

FastNoiseLite Noise::noise2D;
FastNoiseLite Noise::noise3D;

void Noise::Init_2D(int seed, float frequency, int octaves, float lacunarity, float gain) {
	noise2D.SetNoiseType(FastNoiseLite::NoiseType_OpenSimplex2);
	noise2D.SetSeed(seed);
	noise2D.SetFrequency(frequency);

	noise2D.SetFractalType(FastNoiseLite::FractalType_FBm);
	noise2D.SetFractalOctaves(octaves);
	noise2D.SetFractalLacunarity(lacunarity);
	noise2D.SetFractalGain(gain);
}

void Noise::Init_3D(int seed, float frequency, int octaves, float lacunarity, float gain) {
	noise3D.SetNoiseType(FastNoiseLite::NoiseType_OpenSimplex2);
	noise3D.SetSeed(seed);
	noise3D.SetFrequency(frequency);

	noise3D.SetFractalType(FastNoiseLite::FractalType_FBm);
	noise3D.SetFractalOctaves(octaves);
	noise3D.SetFractalLacunarity(lacunarity);
	noise3D.SetFractalGain(gain);
}

extern "C" {
	NOISE_API void NoiseInit_2D(int seed, float frequency, int octaves, float lacunarity, float gain) {
		Noise::Init_2D(seed, frequency, octaves, lacunarity, gain);
	}

	NOISE_API void NoiseInit_3D(int seed, float frequency, int octaves, float lacunarity, float gain) {
		Noise::Init_3D(seed, frequency, octaves, lacunarity, gain);
	}
}