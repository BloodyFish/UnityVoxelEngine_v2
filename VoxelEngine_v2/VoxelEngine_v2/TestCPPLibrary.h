#pragma once

#define TESTFUNCDLL_API __declspec(dllimport)

extern "C" {
	__declspec(dllexport) TESTFUNCDLL_API int PrintRandomNum();
}