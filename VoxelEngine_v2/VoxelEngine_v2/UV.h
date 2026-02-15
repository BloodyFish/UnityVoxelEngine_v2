#pragma once

#ifdef UV_EXPORTS
#define UV_API __declspec(dllexport)
#else 
#define UV_API __declspec(dllimport)
#endif

extern "C" {
	UV_API float* GetUVs(float x, float y, float size);
	UV_API void DeleteUVs(float* ptr);
}