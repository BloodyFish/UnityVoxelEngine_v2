#include "Chunk.h"
#include "Noise.h"

extern "C" {
	CHUNK_API int* GenerateChunkValues(int width, int length, int height, int xPos, int zPos) {
		int size = width * length * height;
		int* voxels = new int[size];


		for (int x = 0; x < width; x++) {
			for (int z = 0; z < length; z++) {
				// We must normalize the noise value first (make it go from 0 to 1 instead of -1 to 1)
				float normal_noise = abs(powf((Noise::noise.GetNoise(float(x + xPos), float(z + zPos)) + 1) / 2, 4));
				int h = int(normal_noise * height);

				for (int y = 0; y < height; y++) {
					int i = x + (z * width) + (y * width * length);
					if (y < h) {
						voxels[i] = 1;
					}
					else {
						voxels[i] = 0;
					}
				}
			}
		}

		return voxels;
	}

	CHUNK_API void DeleteChunkValues(int* ptr) {
		delete[] ptr;
	}
}