#include "Chunk.h"
#include "Noise.h"

extern "C" {
	CHUNK_API int* GenerateChunkValues(int width, int length, int height, int yOffset, int xPos, int zPos, float* continentalness, float* heightFromContinentalness, int splineLength) {
		int size = width * length * height;

		// Initialize array with 0s
		int* voxels = new int[size] {0};


		for (int x = 0; x < width; x++) {
			for (int z = 0; z < length; z++) {

				float noiseX = float(x + xPos);
				float noiseZ = float(z + zPos);

				float noiseVal_2D = Noise::noise2D.GetNoise(noiseX, noiseZ);

				// Get the length of our continentalness to height spline

				float h = 0;
				for (int i = 0; i < splineLength - 1; i++) {
					if (noiseVal_2D >= continentalness[i] && noiseVal_2D <= continentalness[i + 1]) {
						// Create equation for this certain section of the spline:
						float x1 = continentalness[i];
						float x2 = continentalness[i + 1];
						float y1 = heightFromContinentalness[i];
						float y2 = heightFromContinentalness[i + 1];

						// y = mx + b
						// b = y - mx

						float slope = (y2 - y1) / (x2 - x1);
						float b = y1 - slope * x1;

						h = slope * noiseVal_2D + b;
					}
				}


				for (int y = 0; y < h + yOffset; y++) {
					float noiseVal_3D = Noise::noise3D.GetNoise(noiseX, float(y), noiseZ);

					int i = x + (z * width) + (y * width * length);

					if (noiseVal_3D > 0) {
						voxels[i] = 1;
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