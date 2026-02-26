#pragma once

#ifdef CHUNK_EXPORTS
#define CHUNK_API __declspec(dllexport)
#else
#define CHUNK_API __declspec(dllimport)
#endif

extern "C" {
	CHUNK_API int* GenerateChunkValues(int width, int length, int height, int yOffset, int xPos, int zPos, float* continentalness, float* heightFromContinentalness, int splineLength);

	// Free array from memory
	CHUNK_API void DeleteChunkValues(int* ptr);
}
