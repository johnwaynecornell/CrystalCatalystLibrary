// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
//
// Created by johnw on 6/23/2024.
//

#include "CrystalApplication_Windows.h"

using namespace JWCEssentials;

namespace NewAge {


/*
    HINSTANCE hInstance = GetModuleHandle(nullptr);
    WNDCLASS wc = { 0 };
    wc.lpfnWndProc = WindowProc;
    wc.hInstance = hInstance;
    wc.lpszClassName = "CrystalWindow";
    RegisterClass(&wc);
*/

LRESULT CALLBACK CrystalWindow_Windows_WindowProc(HWND hwnd, uint32_t  uMsg, WPARAM wParam, LPARAM lParam);

void CrystalApplication_Windows::Init() {
    CrystalApplication::Init();

    HRESULT_IsError(CoInitialize(nullptr), "CoInitialize");
    HRESULT_IsError(OleInitialize(nullptr), "OleInitialize");

    hInstance = GetModuleHandle(nullptr);

    WNDCLASS wc = { 0 };
    wc.lpfnWndProc = CrystalWindow_Windows_WindowProc;
    wc.hInstance = hInstance;
    wc.lpszClassName = "CrystalWindow";

    this->wc = wc;

    RegisterClass(&this->wc);


}

P_INSTANCE(WindowHandle) CrystalApplication_Windows::WindowCreate(int32_t width, int32_t height, utf8_string_struct title) {

    auto* window = new CrystalWindow_Windows();
    memset(&window->callbacks, 0, sizeof(WindowCallbacks));  // Initialize callbacks to null

    window->hInstance = hInstance;
    window->gl_context = nullptr; // Set GL context later

    auto* window_handle = (P_INSTANCE(WindowHandle))malloc(sizeof(WindowHandle));
    window_handle->crystal_window = window;

    window->myHandle = window_handle;

    HWND hwnd = CreateWindowEx(
            0,
            wc.lpszClassName,
            title,
            WS_OVERLAPPEDWINDOW,
            CW_USEDEFAULT, CW_USEDEFAULT, width, height,
            nullptr,
            nullptr,
            hInstance,
            window_handle
    );

    Application_WindowAdd(window_handle);

    return window_handle;
}

void CrystalApplication_Windows::DispatchCycle()
{
    MSG msg;

    while (PeekMessage(&msg, nullptr, 0, 0, PM_REMOVE)) {
        TranslateMessage(&msg);
        DispatchMessage(&msg);
    }
}

bool CrystalApplication_Windows::HasMessage()
{
    MSG msg;
    if (PeekMessage(&msg, nullptr, 0, 0, PM_NOREMOVE)) return true;  // Approximately 60 FPS
    Sleep(16);
    return false;
}
}