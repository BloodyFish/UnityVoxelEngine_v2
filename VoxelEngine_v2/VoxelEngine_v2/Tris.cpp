#include "Tris.h"

extern "C" {
	TRIS_API int* CreateTris(int offset) {
		int* tris = new int[6]{
			0 + offset, 1 + offset, 2 + offset,
			2 + offset, 3 + offset, 0 + offset
		};

		return tris;
	}

	TRIS_API void DeleteTris(int* ptr) {
		delete[] ptr;
	}
}