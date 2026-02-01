#pragma once

#define VERTEX_API __declspec(dllimport)

#include <vector>

extern "C" {
	__declspec(dllexport) VERTEX_API float* FrontFace(float x, float y, float z);
	__declspec(dllexport) VERTEX_API float* BackFace(float x, float y, float z);
	__declspec(dllexport) VERTEX_API float* LeftFace(float x, float y, float z);
	__declspec(dllexport) VERTEX_API float* RightFace(float x, float y, float z);
	__declspec(dllexport) VERTEX_API float* TopFace(float x, float y, float z);
	__declspec(dllexport) VERTEX_API float* BottomFace(float x, float y, float z);

	__declspec(dllexport) VERTEX_API void DeleteVerts(float* ptr);
}
