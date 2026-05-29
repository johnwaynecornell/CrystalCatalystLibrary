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

    void CrystalWindow_Windows::SetCursor(utf8_string_struct pixformat, P_ELEMENTS(void) pixdata, size_t pixdata_length, int32_t width, int32_t height, int32_t hot_x, int32_t hot_y) {
        if (!hwnd || !pixdata || !pixformat || height <= 0 || width <= 0) return;

        PixData proxy = Pixels_ConvertPixels(pixformat, "bgra:int8", pixdata, pixdata_length, width, height);
        P_ELEMENTS(void) m = proxy ? proxy.pix_data : pixdata;
        if (!m) return;

        // Use standard BITMAPINFOHEADER. Much safer for alpha cursors than an empty V5 header.
        BITMAPINFOHEADER bi = { 0 };
        bi.biSize = sizeof(BITMAPINFOHEADER);
        bi.biWidth = width;
        bi.biHeight = -height; // Negative means top-down
        bi.biPlanes = 1;
        bi.biBitCount = 32;
        bi.biCompression = BI_RGB; // 32-bit BI_RGB inherently implies BGRA layout

        void* bits = nullptr;
        HDC hdc = GetDC(nullptr);
        HBITMAP hBitmap = CreateDIBSection(hdc, (BITMAPINFO*)&bi, DIB_RGB_COLORS, &bits, nullptr, 0);
        ReleaseDC(nullptr, hdc);

        if (hBitmap && bits) {
            // BULLETPROOF ROW-BY-ROW COPY
            // Calculate the actual stride of the incoming memory based on the total length provided
            size_t expected_min_length = (size_t)width * height * 4;
            size_t incoming_stride = (pixdata_length >= expected_min_length) ? (pixdata_length / height) : (width * 4);
            size_t dest_stride = width * 4;

            uint8_t* src = static_cast<uint8_t*>(m);
            uint8_t* dst = static_cast<uint8_t*>(bits);

            for (int32_t y = 0; y < height; ++y) {
                // Copy exactly the width of the image, completely ignoring any padding at the end of the source row
                memcpy(dst + (y * dest_stride), src + (y * incoming_stride), dest_stride);
            }

            // Create a monochrome AND mask. For alpha-blended cursors, this should be all zeros (black).
            // Scanlines in a monochrome bitmap must be WORD-aligned (2 bytes).
            int mask_stride = ((width + 15) / 16) * 2;
            int mask_size = mask_stride * height;
            void* mask_bits = calloc(1, mask_size);
            HBITMAP hMask = CreateBitmap(width, height, 1, 1, mask_bits);
            free(mask_bits);

            if (hot_x < 0) hot_x = 0;
            if (hot_y < 0) hot_y = 0;

            ICONINFO ii = { 0 };
            ii.fIcon = FALSE;
            ii.xHotspot = (DWORD)hot_x;
            ii.yHotspot = (DWORD)hot_y;
            ii.hbmMask = hMask;
            ii.hbmColor = hBitmap;

            HCURSOR hCursor = CreateIconIndirect(&ii);
            if (hCursor) {
                 ::SetCursor(hCursor);
                if (current_hCursor && owns_cursor) DestroyCursor(current_hCursor);
                current_hCursor = hCursor;
                owns_cursor = true;
            }

            DeleteObject(hMask);
            DeleteObject(hBitmap);
        }
        if (proxy) proxy.free();
    }

    void CrystalWindow_Windows::SetStandardCursor(CrystalCursor cursor_enum) {
        if (!hwnd) return;
        LPCSTR idc;
        switch (cursor_enum) {
        case CRYSTAL_CURSOR_ARROW: idc = IDC_ARROW; break;
        case CRYSTAL_CURSOR_TEXT: idc = IDC_IBEAM; break;
        case CRYSTAL_CURSOR_WAIT: idc = IDC_WAIT; break;
        case CRYSTAL_CURSOR_CROSSHAIR: idc = IDC_CROSS; break;
        case CRYSTAL_CURSOR_MOVE: idc = IDC_SIZEALL; break;
        case CRYSTAL_CURSOR_NWSE_RESIZE: idc = IDC_SIZENWSE; break;
        case CRYSTAL_CURSOR_NESW_RESIZE: idc = IDC_SIZENESW; break;
        case CRYSTAL_CURSOR_WE_RESIZE: idc = IDC_SIZEWE; break;
        case CRYSTAL_CURSOR_NS_RESIZE: idc = IDC_SIZENS; break;
        case CRYSTAL_CURSOR_HAND: idc = IDC_HAND; break;
        case CRYSTAL_CURSOR_NOT_ALLOWED: idc = IDC_NO; break;
        default: idc = IDC_ARROW; break;
        }
        HCURSOR hCursor = LoadCursor(nullptr, idc);

        ::SetCursor(hCursor);
        if (current_hCursor && owns_cursor) DestroyCursor(current_hCursor);
        current_hCursor = hCursor;
        owns_cursor = false;
    }

    void CrystalWindow_Windows::SetIcon(utf8_string_struct pixformat, P_ELEMENTS(void) pixdata, size_t pixdata_length, int32_t width, int32_t height) {
        if (!hwnd || !pixdata || !pixformat) return;
        PixData proxy = Pixels_ConvertPixels(pixformat, "bgra:int8", pixdata, pixdata_length, width, height);
        P_ELEMENTS(void) m = proxy ? proxy.pix_data : pixdata;
        if (!m) return;

        BITMAPV5HEADER bi = { 0 };
        bi.bV5Size = sizeof(BITMAPV5HEADER);
        bi.bV5Width = width;
        bi.bV5Height = -height;
        bi.bV5Planes = 1;
        bi.bV5BitCount = 32;
        bi.bV5Compression = BI_BITFIELDS;
        bi.bV5RedMask = 0x00FF0000;
        bi.bV5GreenMask = 0x0000FF00;
        bi.bV5BlueMask = 0x000000FF;
        bi.bV5AlphaMask = 0xFF000000;

        void* bits = nullptr;
        HDC hdc = GetDC(nullptr);
        HBITMAP hBitmap = CreateDIBSection(hdc, (BITMAPINFO*)&bi, DIB_RGB_COLORS, &bits, nullptr, 0);
        ReleaseDC(nullptr, hdc);

        if (hBitmap && bits) {
            memcpy(bits, m, width * height * 4);
            
            int mask_stride = ((width + 15) / 16) * 2;
            int mask_size = mask_stride * height;
            void* mask_bits = calloc(1, mask_size);
            HBITMAP hMask = CreateBitmap(width, height, 1, 1, mask_bits);
            free(mask_bits);

            ICONINFO ii = { 0 };
            ii.fIcon = TRUE;
            ii.hbmMask = hMask;
            ii.hbmColor = hBitmap;
            HICON hIcon = CreateIconIndirect(&ii);

            if (hIcon) {
                SendMessage(hwnd, WM_SETICON, ICON_BIG, (LPARAM)hIcon);
                SendMessage(hwnd, WM_SETICON, ICON_SMALL, (LPARAM)hIcon);

                if (current_hIcon) DestroyIcon(current_hIcon);
                current_hIcon = hIcon;
            }
            
            DeleteObject(hMask);
            DeleteObject(hBitmap);
        }
        if (proxy) proxy.free();
    }

    void CrystalWindow_Windows::SetTitle(utf8_string_struct title) {
        if (!hwnd) return;
        SetWindowText(hwnd, title);
    }

    void CrystalWindow_Windows::GetTitle(P_OUT(utf8_string_struct) title) {
        if (!hwnd) { *title = ""; return; }
        int len = GetWindowTextLength(hwnd);
        if (len > 0) {
            std::string buffer(len, '\0');
            GetWindowText(hwnd, &buffer[0], len + 1);
            *title = buffer.c_str();
        }
        else {
            *title = "";
        }
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
        case WM_ERASEBKGND: {
                return 1; // Tell Windows we handled it, preventing automatic background clearing
        }
        case WM_SETCURSOR:
            if (LOWORD(lParam) == HTCLIENT)
            {
                if (wnd->current_hCursor) {
                    ::SetCursor(wnd->current_hCursor);
                    return TRUE;
                }
            }
            break;
        case WM_PAINT: {
                PAINTSTRUCT ps;
                HDC hdc = BeginPaint(hwnd, &ps);
                wnd->paint_hdc = hdc;

                if (wnd->callbacks.on_draw) {
                    wnd->callbacks.
                            on_draw(handle);
                }
                wnd->paint_hdc = nullptr;
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

        bool use_paint_dc = (paint_hdc != nullptr);
        HDC hdc = use_paint_dc ? paint_hdc : GetDC(hwnd);


        BITMAPINFO bmi;
        memset(&bmi, 0, sizeof(bmi));
        bmi.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
        bmi.bmiHeader.biWidth = width;
        bmi.bmiHeader.biHeight = -height; // Top-down image
        bmi.bmiHeader.biPlanes = 1;
        bmi.bmiHeader.biBitCount = 32; // 32 bits per pixel
        bmi.bmiHeader.biCompression = BI_RGB;

        StretchDIBits(hdc, 0, 0, width, height, 0, 0, width, height, m, &bmi, DIB_RGB_COLORS, SRCCOPY);

        if (proxy) proxy.free();

        if (!use_paint_dc) {
            ReleaseDC(hwnd, hdc);
        }
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

    CrystalWindow_Windows::~CrystalWindow_Windows() {
        if (current_hIcon) DestroyIcon(current_hIcon);
        if (current_hCursor && owns_cursor) DestroyCursor(current_hCursor);
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