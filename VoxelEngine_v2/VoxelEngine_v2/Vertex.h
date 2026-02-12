#pragma once

#ifdef VERTEX_EXPORTS
#define VERTEX_API __declspec(dllexport)
#else
#define VERTEX_API __declspec(dllimport)
#endif


#include <vector>

extern "C" {
	VERTEX_API float* FrontFace(float x, float y, float z);
	VERTEX_API float* BackFace(float x, float y, float z);
	VERTEX_API float* LeftFace(float x, float y, float z);
	VERTEX_API float* RightFace(float x, float y, float z);
	VERTEX_API float* TopFace(float x, float y, float z);
	VERTEX_API float* BottomFace(float x, float y, float z);

	// Free array from memory
	VERTEX_API void DeleteVerts(float* ptr);
}
