#pragma once

#ifdef CHUNK_EXPORTS
#define CHUNK_API __declspec(dllexport)
#else
#define CHUNK_API __declspec(dllimport)
#endif

extern "C" {
	CHUNK_API int* GenerateChunkValues(int width, int length, int height, int xPos, int zPos);

	// Free array from memory
	CHUNK_API void DeleteChunkValues(int* ptr);
}
