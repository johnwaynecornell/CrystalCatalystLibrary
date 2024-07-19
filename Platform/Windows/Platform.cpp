// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include <iostream>
#include "Platform.h"
#include "CrystalApplication_Windows.h"

P_INSTANCE(CrystalApplication) platform_initialize() {
    return new CrystalApplication_Windows();
}

void platform_uninitialize()
{

}

bool HRESULT_IsError(HRESULT Hr, std::string text)
{
    if (!FAILED(Hr)) return false;

    LPTSTR messageBuffer = nullptr;

    size_t size = FormatMessageA(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS,
                                 nullptr, Hr, MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), (LPTSTR)&messageBuffer, 0, nullptr);

    std::string message(messageBuffer, size);

    std::cout <<text << " ERROR HRESULT:" << message << std::endl;

    // Free the buffer allocated by FormatMessage
    LocalFree(messageBuffer);

    return true;
}

uint32_t map_windows_key_state(DWORD grfKeyState) {
    uint32_t key_state = 0;

    if (grfKeyState & MK_CONTROL) {
        key_state |= KEY_STATE_CONTROL;
    }
    if (grfKeyState & MK_SHIFT) {
        key_state |= KEY_STATE_SHIFT;
    }
    if (GetKeyState(VK_MENU) & 0x8000) { // Check if the ALT key is down
        key_state |= KEY_STATE_ALT;
    }

    return key_state;
}