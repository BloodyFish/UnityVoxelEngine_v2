#pragma once

#define TRIS_API __declspec(dllimport)

#include <vector>

extern "C" {
	__declspec(dllexport) TRIS_API int* FrontTris();
	__declspec(dllexport) TRIS_API int* BackTris();
	__declspec(dllexport) TRIS_API int* LeftTris();
	__declspec(dllexport) TRIS_API int* RightTris();
	__declspec(dllexport) TRIS_API int* TopTris();
	__declspec(dllexport) TRIS_API int* BottomTris();

	__declspec(dllexport) TRIS_API void DeleteTris(int* ptr);
}