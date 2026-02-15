#include "UV.h"

extern "C" {
	UV_API float* GetUVs(float x, float y, float size) {
		// The coordinates of our texture atlas are as follows:
		// TOP LEFT = (0, 1)
		// TOP RIGHT = (1, 1)
		// BOTTOM LEFT = (0, 0)
		// BOTTOM RIGHT = (1, 0)

		float textureStep = 1 / size;

		float x0 = textureStep * x;
		float y0 = textureStep * y;
		float x1 = textureStep * (x + 1);
		float y1 = textureStep * (y + 1);

		float* uvs = new float[8] {
			// BOTTOM LEFT
			x0, y0,

			// TOP LEFT
			x0, y1,

			// TOP RIGHT
			x1, y1,

			// BOTTOM RIGHT
			x1, y0
		};

		return uvs;
	}

	UV_API void DeleteUVs(float* ptr) {
		delete[] ptr;
	}
}