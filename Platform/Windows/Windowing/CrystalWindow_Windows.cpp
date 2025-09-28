// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include "CrystalWindow_Windows.h"
#include <windows.h>
#include <windowsx.h>
#include <shlobj.h>
#include <ole2.h>
#include <strsafe.h>
#include <iostream>

//this is here for IDE compatability on Linux
#ifndef CALLBACK
#define CALLBACK
#endif

#ifdef mod_header
#undef mod_header
#endif

#define mod_header() "CrystalWindow_Windows:"

using namespace JWCEssentials;

namespace NewAge {


wchar_t ConvertKeyCodeToUnicode(uint32_t  keyCode) {
    BYTE keyboardState[256];
    GetKeyboardState(keyboardState);

    wchar_t unicodeChar[2];
    int32_t result = ToUnicode(keyCode, 0, keyboardState, unicodeChar, 2, 0);

    if (result > 0) {
        return unicodeChar[0];
    }
    return 0;
}

/*
    [LeftShift]    CrystalWindow_X11:KeyPress, unicodeChar = 65505
    [LeftCtrl]    CrystalWindow_X11:KeyPress, unicodeChar = 65507
    [LeftAlt]    CrystalWindow_X11:KeyPress, unicodeChar = 65513
 */


LRESULT CALLBACK WindowProc(HWND hwnd, uint32_t  uMsg, WPARAM wParam, LPARAM lParam);

void CrystalWindow_Windows::GLInit() {
    PIXELFORMATDESCRIPTOR pfd = {
            sizeof(PIXELFORMATDESCRIPTOR),   // size of this pfd
            1,                               // version number
            PFD_DRAW_TO_WINDOW |             // support window
            PFD_SUPPORT_OPENGL |             // support OpenGL
            PFD_DOUBLEBUFFER,                // double buffered
            PFD_TYPE_RGBA,                   // RGBA type
            24,                              // 24-bit color depth
            0, 0, 0, 0, 0, 0,                // color bits ignored
            0,                               // no alpha buffer
            0,                               // shift bit ignored
            0,                               // no accumulation buffer
            0, 0, 0, 0,                      // accum bits ignored
            32,                              // 32-bit z-buffer
            0,                               // no stencil buffer
            0,                               // no auxiliary buffer
            PFD_MAIN_PLANE,                  // main layer
            0,                               // reserved
            0, 0, 0                          // layer masks ignored
    };

    HDC hdc = GetDC(hwnd);
    int32_t iPixelFormat = ChoosePixelFormat(hdc, &pfd);
    SetPixelFormat(hdc, iPixelFormat, &pfd);

    gl_context = wglCreateContext(hdc);
    wglMakeCurrent(hdc, gl_context);
}

void CrystalWindow_Windows::Show(bool restore)
{
    ShowWindow(hwnd, restore ? SW_SHOWNORMAL : SW_SHOW);
}


void CrystalWindow_Windows::MouseCapture() {
    SetCapture(hwnd);
}

void CrystalWindow_Windows::MouseRelease() {
    ReleaseCapture();
}

#define WM_USER_POSTCREATE WM_USER + 1

LRESULT CALLBACK CrystalWindow_Windows_WindowProc(HWND hwnd, uint32_t  uMsg, WPARAM wParam, LPARAM lParam) {
    P_INSTANCE(WindowHandle) handle;
    
    P_INSTANCE(CrystalWindow_Windows) wnd;

    if (!wnd->received_first_message) { wnd->reset_uptime(); wnd->received_first_message = true; }

    if (uMsg == WM_CREATE)
    {
        CREATESTRUCT* pCreate = (CREATESTRUCT*)lParam;
        handle = (P_INSTANCE(WindowHandle) ) pCreate->lpCreateParams;

        ((P_INSTANCE(CrystalWindow_Windows) ) handle->crystal_window)->hwnd = hwnd;
        SetWindowLongPtr(hwnd, GWLP_USERDATA, (LONG_PTR)handle);
    } else  handle = (P_INSTANCE(WindowHandle) ) GetWindowLongPtr(hwnd, GWLP_USERDATA);

    if (!handle) {
        return
                DefWindowProc(hwnd, uMsg, wParam, lParam
                );
    }
    
    wnd = (P_INSTANCE(CrystalWindow_Windows) ) handle->crystal_window;
    
    switch (uMsg) {
        case WM_CREATE: {
            int32_t rc = DefWindowProc(hwnd, uMsg, wParam, lParam);
            if (rc == 0)
                PostMessage(hwnd, WM_USER_POSTCREATE, 0, 0);
            return rc;
        }
        case WM_USER_POSTCREATE: {
            //if (HRESULT_IsError(RegisterDragDrop(hwnd, reinterpret_cast<IDropTarget *>(handle->crystal_window)),
            //    "RegisterDragDrop")) return 1;

            return 0;
        }
        case WM_PAINT: {
                PAINTSTRUCT ps;
                HDC hdc = BeginPaint(hwnd, &ps);

                FillRect(hdc, &ps.rcPaint, (HBRUSH) (COLOR_WINDOW + 1));

                if (wnd->callbacks.on_draw) {
                    wnd->callbacks.
                            on_draw(handle);
                }
                /* TODO */ //test for finalized CreateWindowSimple
                wnd->ready = true;
                EndPaint(hwnd, &ps);
            }
            break;
        case WM_KEYDOWN:
            if (wnd->callbacks.on_key_down) {
                int32_t key = ConvertKeyCodeToUnicode((int) wParam);
                wnd->callbacks.on_key_down(handle, key);
            }
            break;
        case WM_KEYUP:
            if (wnd->callbacks.on_key_up) {
                int32_t key = ConvertKeyCodeToUnicode((int) wParam);
                wnd->callbacks.
                        on_key_up(handle,
                                  (int) key);
            }
            break;
        case WM_MOUSEMOVE:
            if (wnd->callbacks.on_mouse_move) {
                wnd->callbacks.
                        on_mouse_move(handle, GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam)
                );
            }
            break;
        case WM_LBUTTONDOWN:
            if (wnd->callbacks.on_mouse_down) {
                wnd->callbacks.
                        on_mouse_down(handle, MK_LBUTTON, GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam)
                );
            }
            break;
        case WM_LBUTTONUP:
            if (wnd->callbacks.on_mouse_up) {
                wnd->callbacks.
                        on_mouse_up(handle, MK_LBUTTON, GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam)
                );
            }
            break;
        case WM_SIZE:
            if (wnd->callbacks.on_resize) {
                wnd->callbacks.
                        on_resize(handle, LOWORD(lParam), HIWORD(lParam)
                );
            }
            break;
        case WM_SETFOCUS:
            if (wnd->callbacks.on_focus_in) {
                wnd->callbacks.
                        on_focus_in(handle);
            }
            break;
        case WM_KILLFOCUS:
            if (wnd->callbacks.on_focus_out) {
                wnd->callbacks.
                        on_focus_out(handle);
            }
            break;
        case WM_DESTROY:
            if (wnd->callbacks.on_close) {
                wnd->callbacks.
                        on_close(handle);
            }
            break;
        case WM_QUIT:
            break;
        default:
            //std::cout << "MESSAGE:" << uMsg << std::endl;
            return DefWindowProc(hwnd, uMsg, wParam, lParam);
    }
    return 0;
}

void CrystalWindow_Windows::PresentImage(utf8_string_struct pixformat, P_ELEMENTS(void)  pixdata, size_t pixdata_length, int32_t width, int32_t height)
{
    if (!pixdata || !pixformat) return;

    PixData proxy= Pixels_ConvertPixels(pixformat, "bgra:int8", pixdata, pixdata_length, width, height);
    /* TODO */ //Check error condition

    P_ELEMENTS(void) m = proxy ? proxy.pix_data : pixdata;

    HDC hdc = GetDC(hwnd);


    BITMAPINFO bmi;
    memset(&bmi, 0, sizeof(bmi));
    bmi.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
    bmi.bmiHeader.biWidth = width;
    bmi.bmiHeader.biHeight = -height; // Top-down image
    bmi.bmiHeader.biPlanes = 1;
    bmi.bmiHeader.biBitCount = 32; // 32 bits per pixel
    bmi.bmiHeader.biCompression = BI_RGB;

    StretchDIBits(hdc, 0, 0, width, height, 0, 0, width, height, m, &bmi, DIB_RGB_COLORS, SRCCOPY);

    if (proxy) proxy.pix_data_free(proxy.pix_data);
    
    ReleaseDC(hwnd, hdc);
}

void CrystalWindow_Windows::QueueRedraw()
{
    // Invalidate the window rectangle to force a redraw
    InvalidateRect(hwnd, nullptr, TRUE);
    UpdateWindow(hwnd);
}


CrystalWindow_Windows::CrystalWindow_Windows() : m_cRef(1)
{

}

HRESULT __stdcall CrystalWindow_Windows::QueryInterface(REFIID iid, P_ELEMENTS(void) * ppvObject)
{
    if(iid == IID_IUnknown)
    {
        AddRef();
        *ppvObject = static_cast<IDropTarget *>(this);
        return S_OK;
    } else if (iid == IID_IDropTarget)
    {
        AddRef();
        *ppvObject = static_cast<IDropTarget *>(this);
        return S_OK;

    }
    else if (iid == IID_IDropSource) {
        AddRef();
        *ppvObject = static_cast<IDropSource *>(this);
        return S_OK;
    } else
    {
        *ppvObject = 0;
        return E_NOINTERFACE;
    }
}

ULONG __stdcall CrystalWindow_Windows::AddRef()
{
    return InterlockedIncrement(&m_cRef);
}

ULONG __stdcall CrystalWindow_Windows::Release()
{
    if(InterlockedDecrement(&m_cRef) == 0)
    {
        delete this;
        return 0;
    }

    return m_cRef;
}
}