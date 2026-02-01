#include "Tris.h"

extern "C" {
	__declspec(dllexport) TRIS_API int* FrontTris() {
		int* tris = new int[6]{
			0, 1, 2,
			2, 3, 0
		};

		return tris;
	}
	__declspec(dllexport) TRIS_API int* BackTris() {
		int* tris = new int[6]{
			4, 5, 6,
			6, 7, 4
		};

		return tris;
	}
	__declspec(dllexport) TRIS_API int* LeftTris() {
		int* tris = new int[6]{
			8, 9, 10,
			10, 11, 8
		};

		return tris;
	}
	__declspec(dllexport) TRIS_API int* RightTris() {
		int* tris = new int[6]{
			12, 13, 14,
			14, 15, 12
		};

		return tris;
	}
	__declspec(dllexport) TRIS_API int* TopTris() {
		int* tris = new int[6]{
			16, 17, 18,
			18, 19, 16
		};

		return tris;
	}

	__declspec(dllexport) TRIS_API int* BottomTris() {
		int* tris = new int[6]{
			20, 21, 22,
			22, 23, 20
		};

		return tris;
	}

	__declspec(dllexport) TRIS_API void DeleteTris(int* ptr) {
		delete[] ptr;
	}
}