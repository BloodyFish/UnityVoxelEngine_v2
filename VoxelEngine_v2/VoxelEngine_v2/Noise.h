#pragma once

#ifdef NOISE_EXPORTS
#define NOISE_API __declspec(dllexport)
#else
#define NOISE_API __declspec(dllimport)
#endif

#include "FastNoiseLite.h"

class Noise {
	public:
		static FastNoiseLite noise2D;
		static FastNoiseLite noise3D;

		static void Init_2D(int seed, float frequency, int octaves, float lacunarity, float gain);
		static void Init_3D(int seed, float frequency, int octaves, float lacunarity, float gain);
};

extern "C" {
	NOISE_API void NoiseInit_2D(int seed, float frequency, int octaves, float lacunarity, float gain);
	NOISE_API void NoiseInit_3D(int seed, float frequency, int octaves, float lacunarity, float gain);
}