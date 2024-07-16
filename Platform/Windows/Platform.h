#ifndef PLATFORM_WINDOWS_H
#define PLATFORM_WINDOWS_H

#include <windows.h>
#include <GL/gl.h>

#include "../../Platform.h"
#include "../../CrystalCatalystLibrary.h"

P_INSTANCE(CrystalApplication) platform_initialize();

bool HRESULT_IsError(HRESULT Hr, std::string text);
uint32_t map_windows_key_state(DWORD grfKeyState);

#endif //PLATFORM_WINDOWS_H
