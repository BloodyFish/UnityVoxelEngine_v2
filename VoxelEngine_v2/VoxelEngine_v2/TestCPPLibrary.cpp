#include "TestCPPLibrary.h"

#include <random>

extern "C" {
	__declspec(dllexport) TESTFUNCDLL_API int PrintRandomNum() {
		return rand() % 10;
	}
}