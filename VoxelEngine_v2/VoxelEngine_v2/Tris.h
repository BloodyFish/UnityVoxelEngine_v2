#pragma once

#ifdef TRIS_EXPORTS
#define TRIS_API __declspec(dllexport)
#else
#define TRIS_API __declspec(dllimport)
#endif

#include <vector>

extern "C" {
	TRIS_API int* CreateTris(int offset);

	// Free array from memory
	TRIS_API void DeleteTris(int* ptr);
}