// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
//
// Created by johnw on 6/23/2024.
//

#include "CrystalApplication_Windows.h"

using namespace JWCEssentials;

namespace NewAge
{
    /*
        HINSTANCE hInstance = GetModuleHandle(nullptr);
        WNDCLASS wc = { 0 };
        wc.lpfnWndProc = WindowProc;
        wc.hInstance = hInstance;
        wc.lpszClassName = "CrystalWindow";
        RegisterClass(&wc);
    */

    LRESULT CALLBACK CrystalWindow_Windows_WindowProc(HWND hwnd, uint32_t  uMsg, WPARAM wParam, LPARAM lParam);

    CrystalApplication_Windows::~CrystalApplication_Windows() {
        OleUninitialize();
        CoUninitialize();
    }

    void CrystalApplication_Windows::Init() {
        CrystalApplication::Init();

        HRESULT hr = CoInitialize(nullptr);
        if (FAILED(hr)) {
            if (hr == RPC_E_CHANGED_MODE) {
                std::cerr << "CoInitialize: Thread is already in MTA mode. OLE clipboard and Drag & Drop functions may not work correctly. "
                             "Please ensure your main thread is in STA mode (e.g., use [STAThread] in .NET)." << std::endl;
            } else {
                HRESULT_IsError(hr, "CoInitialize");
            }
        }

        hr = OleInitialize(nullptr);
        if (FAILED(hr)) {
            if (hr == RPC_E_CHANGED_MODE) {
                std::cerr << "OleInitialize: Thread is already in MTA mode. OLE functions REQUIRE STA mode. "
                             "Clipboard and Drag & Drop will fail. Ensure your main thread is [STAThread]." << std::endl;
            } else {
                HRESULT_IsError(hr, "OleInitialize");
            }
        }

        hInstance = GetModuleHandle(nullptr);

        WNDCLASS wc = { 0 };
        wc.style = CS_HREDRAW | CS_VREDRAW | CS_OWNDC;
        wc.lpfnWndProc = CrystalWindow_Windows_WindowProc;
        wc.hInstance = hInstance;
        wc.lpszClassName = "CrystalWindow";

        this->wc = wc;

        if (RegisterClass(&this->wc) == 0) {
            DWORD err = GetLastError();
            if (err != ERROR_CLASS_ALREADY_EXISTS) {
                HRESULT_IsError(HRESULT_FROM_WIN32(err), "RegisterClass");
            }
        }


    }

    P_INSTANCE(WindowHandle) CrystalApplication_Windows::WindowCreate(int32_t width, int32_t height, utf8_string_struct title) {

        CrystalWindow_Windows* window = new CrystalWindow_Windows();
        memset(&window->callbacks, 0, sizeof(WindowCallbacks));  // Initialize callbacks to null

        window->hInstance = hInstance;
        window->gl_context = nullptr; // Set GL context later

        auto* window_handle = (P_INSTANCE(WindowHandle))malloc(sizeof(WindowHandle));
        window_handle->crystal_window = window;

        window->myHandle = window_handle;

        RECT rect = { 0, 0, width, height };
        AdjustWindowRectEx(&rect, WS_OVERLAPPEDWINDOW, FALSE, 0);

        HWND hwnd = CreateWindowEx(
                0,
                wc.lpszClassName,
                title,
                WS_OVERLAPPEDWINDOW,
                CW_USEDEFAULT, CW_USEDEFAULT, rect.right - rect.left, rect.bottom - rect.top,
                nullptr,
                nullptr,
                hInstance,
                window_handle
        );

        if (!hwnd) {
            HRESULT_IsError(HRESULT_FROM_WIN32(GetLastError()), "CreateWindowEx");
        }

        Application_WindowAdd(window_handle);
        window->SetStandardCursor(CRYSTAL_CURSOR_ARROW);

        return window_handle;
    }

    P_INSTANCE(WindowHandle) CrystalApplication_Windows::WindowCreate_Simple(int32_t width, int32_t height, utf8_string_struct title) {

        CrystalWindow_Windows* window = new CrystalWindow_Windows();
        memset(&window->callbacks, 0, sizeof(WindowCallbacks));  // Initialize callbacks to null

        window->hInstance = hInstance;
        window->gl_context = nullptr; // Set GL context later

        auto* window_handle = (P_INSTANCE(WindowHandle))malloc(sizeof(WindowHandle));
        window_handle->crystal_window = window;

        window->myHandle = window_handle;

        RECT rect = { 0, 0, width, height };
        AdjustWindowRectEx(&rect, WS_POPUP, FALSE, WS_EX_TOOLWINDOW);

        HWND hwnd = CreateWindowEx(
                WS_EX_TOOLWINDOW,
                wc.lpszClassName,
                title,
                WS_POPUP,
                -10000, -10000, rect.right - rect.left, rect.bottom - rect.top,
                nullptr,
                nullptr,
                hInstance,
                window_handle
        );

        if (!hwnd) {
            HRESULT_IsError(HRESULT_FROM_WIN32(GetLastError()), "CreateWindowEx");
        }

        Application_WindowAdd(window_handle);
        window->SetStandardCursor(CRYSTAL_CURSOR_ARROW);

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