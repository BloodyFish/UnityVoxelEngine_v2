#include "vertex.h"

extern "C"{
	VERTEX_API float* FrontFace(float x, float y, float z) {
		// TRIS: 0, 1, 2,
		//		 2, 3, 2
		float* verts = new float[12] {
			0.5f + x, -0.5f + y, 0.5f + z,
				0.5f + x, 0.5f + y, 0.5f + z,
				-0.5f + x, 0.5f + y, 0.5f + z,
				-0.5f + x, -0.5f + y, 0.5f + z
			};

		return verts;
	}
	VERTEX_API float* BackFace(float x, float y, float z) {
		// TRIS: 4, 5, 6
		//		 6, 7, 4
		float* verts = new float[12] {
			-0.5f + x, -0.5f + y, -0.5f + z,
				-0.5f + x, 0.5f + y, -0.5f + z,
				0.5f + x, 0.5f + y, -0.5f + z,
				0.5f + x, -0.5f + y, -0.5f + z
			};

		return verts;
	}
	VERTEX_API float* LeftFace(float x, float y, float z) {
		// TRIS: 8, 9, 10,
		//		 10, 11, 8

		float* verts = new float[12]{
			-0.5f + x, -0.5f + y, 0.5f + z,
			-0.5f + x, 0.5f + y, 0.5f + z,
			-0.5f + x, 0.5f + y, -0.5f + z,
			-0.5f + x, -0.5f + y, -0.5f + z
		};

		return verts;
	}
	VERTEX_API float* RightFace(float x, float y, float z) {
		// TRIS: 12, 13, 14,
		//		 14, 15, 12

		float* verts = new float[12]{
			0.5f + x, -0.5f + y, -0.5f + z,
			0.5f + x, 0.5f + y, -0.5f + z,
			0.5f + x, 0.5f + y, 0.5f + z,
			0.5f + x, -0.5f + y, 0.5f + z
		};

		return verts;
	}
	VERTEX_API float* TopFace(float x, float y, float z) {
		// TRIS: 16, 17, 18,
		//		 18, 19, 16

		float* verts = new float[12]{
			-0.5f + x, 0.5f + y, -0.5f + z,
			-0.5f + x, 0.5f + y, 0.5f + z,
			0.5f + x, 0.5f + y, 0.5f + z,
			0.5f + x, 0.5f + y, -0.5f + z
		};

		return verts;
	}
	VERTEX_API float* BottomFace(float x, float y, float z) {
		// TRIS: 20, 21, 22,
		//		 22, 23, 20

		float* verts = new float[12]{
			-0.5f + x, -0.5f + y, 0.5f + z,
			-0.5f + x, -0.5f + y, -0.5f + z,
			0.5f + x, -0.5f + y, -0.5f + z,
			0.5f + x, -0.5f + y, 0.5f + z
		};

		return verts;
	}

	VERTEX_API void DeleteVerts(float* ptr) {
		delete[] ptr;
	}
}