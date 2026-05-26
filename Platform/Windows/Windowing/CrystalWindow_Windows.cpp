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

namespace NewAge
{
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

    static void DispatchMouseButtonDown(P_INSTANCE(WindowHandle) handle, P_INSTANCE(CrystalWindow_Windows) wnd, int32_t button, int32_t x, int32_t y) {
        if (wnd->callbacks.on_mouse_down) {
            wnd->callbacks.on_mouse_down(handle, button, x, y);
        }
    }

    static void DispatchMouseButtonUp(P_INSTANCE(WindowHandle) handle, P_INSTANCE(CrystalWindow_Windows) wnd, int32_t button, int32_t x, int32_t y) {
        if (wnd->callbacks.on_mouse_up) {
            wnd->callbacks.on_mouse_up(handle, button, x, y);
        }
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

    void CrystalWindow_Windows::Close()
    {
        if (is_closed) { return; }
        is_closed = true;

        if (!hwnd) {return; }

        HWND closing_hwnd = hwnd;

        if (callbacks.on_close) {
            callbacks.on_close(myHandle);
        }

        RevokeDragDrop(closing_hwnd);

        if (GetCapture() == closing_hwnd) {
            ReleaseCapture();
        }

        if (gl_context) {
            wglMakeCurrent(nullptr, nullptr);
            wglDeleteContext(gl_context);
            gl_context = nullptr;
        }

        SetWindowLongPtr(closing_hwnd, GWLP_USERDATA, 0);

        hwnd = nullptr;
        ready = false;

        DestroyWindow(closing_hwnd);
    }

    void CrystalWindow_Windows::PostClose()
    {
        if (!hwnd) return;
        PostMessage(hwnd, WM_CLOSE, 0, 0);
    }


    void CrystalWindow_Windows::MouseCapture() {
        SetCapture(hwnd);
    }

    void CrystalWindow_Windows::MouseRelease() {
        ReleaseCapture();
    }

    void CrystalWindow_Windows::SetSize(int32_t width, int32_t height) {
        if (!hwnd) return;
        RECT rect = { 0, 0, width, height };
        DWORD style = GetWindowLong(hwnd, GWL_STYLE);
        DWORD exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        AdjustWindowRectEx(&rect, style, FALSE, exStyle);
        SetWindowPos(hwnd, nullptr, 0, 0, rect.right - rect.left, rect.bottom - rect.top, SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE);
    }

    void CrystalWindow_Windows::GetSize(int32_t& width, int32_t& height) {
        if (!hwnd) { width = 0; height = 0; return; }
        RECT rect;
        GetClientRect(hwnd, &rect);
        width = rect.right - rect.left;
        height = rect.bottom - rect.top;
    }

    void CrystalWindow_Windows::SetLocation(int32_t x, int32_t y) {
        if (!hwnd) return;
        RECT rect = { x, y, x, y };
        DWORD style = GetWindowLong(hwnd, GWL_STYLE);
        DWORD exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        AdjustWindowRectEx(&rect, style, FALSE, exStyle);
        SetWindowPos(hwnd, nullptr, rect.left, rect.top, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
    }

    void CrystalWindow_Windows::GetLocation(int32_t& x, int32_t& y) {
        if (!hwnd) { x = 0; y = 0; return; }
        POINT pt = { 0, 0 };
        ClientToScreen(hwnd, &pt);
        x = pt.x;
        y = pt.y;
    }
    
    

#define WM_USER_POSTCREATE WM_USER + 1

    static POINT ScreenPointFromLParam(HWND hwnd, LPARAM lParam)
    {
        POINT pt;
        pt.x = GET_X_LPARAM(lParam);
        pt.y = GET_Y_LPARAM(lParam);
        ScreenToClient(hwnd, &pt);
        return pt;
    }

    LRESULT CALLBACK CrystalWindow_Windows_WindowProc(HWND hwnd, uint32_t  uMsg, WPARAM wParam, LPARAM lParam) {
        P_INSTANCE(WindowHandle) handle = nullptr;

        if (uMsg == WM_NCCREATE)
        {
            auto* pCreate = (CREATESTRUCT*)lParam;
            handle = (P_INSTANCE(WindowHandle) ) pCreate->lpCreateParams;
            SetWindowLongPtr(hwnd, GWLP_USERDATA, (LONG_PTR)handle);
        } else  handle = (P_INSTANCE(WindowHandle) ) GetWindowLongPtr(hwnd, GWLP_USERDATA);

        if (!handle) {
            return DefWindowProc(hwnd, uMsg, wParam, lParam);
        }

        auto* wnd = (P_INSTANCE(CrystalWindow_Windows) ) handle->crystal_window;

        if (!wnd->received_first_message) { wnd->reset_uptime(); wnd->received_first_message = true; }

        if (uMsg == WM_CREATE)
        {
            wnd->hwnd = hwnd;
        }

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

                wnd->ready = true;
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
            DispatchMouseButtonDown(handle, wnd, CRYSTAL_MOUSE_BUTTON_LEFT, GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam));
            break;
        case WM_LBUTTONUP:
            DispatchMouseButtonUp(handle, wnd, CRYSTAL_MOUSE_BUTTON_LEFT, GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam));
            break;
        case WM_MBUTTONDOWN:
            DispatchMouseButtonDown(handle, wnd, CRYSTAL_MOUSE_BUTTON_MIDDLE, GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam));
            break;
        case WM_MBUTTONUP:
            DispatchMouseButtonUp(handle, wnd, CRYSTAL_MOUSE_BUTTON_MIDDLE, GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam));
            break;
        case WM_RBUTTONDOWN:
            DispatchMouseButtonDown(handle, wnd, CRYSTAL_MOUSE_BUTTON_RIGHT, GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam));
            break;
        case WM_RBUTTONUP:
            DispatchMouseButtonUp(handle, wnd, CRYSTAL_MOUSE_BUTTON_RIGHT, GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam));
            break;
        case WM_MOUSEWHEEL: {
                POINT pt = ScreenPointFromLParam(hwnd, lParam);
                int32_t button = GET_WHEEL_DELTA_WPARAM(wParam) > 0
                    ? CRYSTAL_MOUSE_BUTTON_WHEEL_UP
                    : CRYSTAL_MOUSE_BUTTON_WHEEL_DOWN;

                DispatchMouseButtonDown(handle, wnd, button, pt.x, pt.y);
                DispatchMouseButtonUp(handle, wnd, button, pt.x, pt.y);
                break;
        }
        case WM_MOUSEHWHEEL: {
                POINT pt = ScreenPointFromLParam(hwnd, lParam);
                int32_t button = GET_WHEEL_DELTA_WPARAM(wParam) < 0
                    ? CRYSTAL_MOUSE_BUTTON_WHEEL_LEFT
                    : CRYSTAL_MOUSE_BUTTON_WHEEL_RIGHT;

                DispatchMouseButtonDown(handle, wnd, button, pt.x, pt.y);
                DispatchMouseButtonUp(handle, wnd, button, pt.x, pt.y);
                break;
        }
        case WM_XBUTTONDOWN: {
                int32_t button = GET_XBUTTON_WPARAM(wParam) == XBUTTON1
                    ? CRYSTAL_MOUSE_BUTTON_X1
                    : CRYSTAL_MOUSE_BUTTON_X2;

                DispatchMouseButtonDown(handle, wnd, button, GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam));
                return TRUE;
        }
        case WM_XBUTTONUP: {
                int32_t button = GET_XBUTTON_WPARAM(wParam) == XBUTTON1
                    ? CRYSTAL_MOUSE_BUTTON_X1
                    : CRYSTAL_MOUSE_BUTTON_X2;

                DispatchMouseButtonUp(handle, wnd, button, GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam));
                return TRUE;
        }
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
        case WM_CLOSE:
            wnd->Close();
            return 0;

        case WM_DESTROY:
            wnd->hwnd = nullptr;
            return 0;

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