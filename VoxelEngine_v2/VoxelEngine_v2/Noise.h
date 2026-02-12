#pragma once

#ifdef NOISE_EXPORTS
#define NOISE_API __declspec(dllexport)
#else
#define NOISE_API __declspec(dllimport)
#endif

#include "FastNoiseLite.h"

class Noise {
	public:
		static FastNoiseLite noise;
		static void Init(int seed, float frequency, int octaves, float lacunarity, float gain);
};

extern "C" {
	NOISE_API void NoiseInit(int seed, float frequency, int octaves, float lacunarity, float gain);
}