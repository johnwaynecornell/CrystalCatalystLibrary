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

void CrystalWindow_Windows::GL_Init() {
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

void CrystalWindow_Windows::PresentImage(utf8_string_const pixformat, P_ELEMENTS(void)  pixdata, size_t pixdata_length, int32_t width, int32_t height)
{
    if (!pixdata || !pixformat) return;

    HDC hdc = GetDC(hwnd);

    std::string pf = pixformat;

    int32_t colon = pf.find(':');
    if (colon == -1 || pf.substr(colon + 1) != "int8") {
        std::cerr << mod_header() << "PresentImage: pixel format '" << pf << "' not recognized. Recognized string is a combination of the characters R,G,B,A in any order followed by a colon and 'int8', such as BGRA:int8.";
        return;
    }

    int32_t channels[4] = {-1, -1, -1, -1};

    for (int32_t i = 0; i < colon; i++) {
        switch (pf[i]) {
            case 'R': case 'r': channels[0] = i; break;
            case 'G': case 'g': channels[1] = i; break;
            case 'B': case 'b': channels[2] = i; break;
            case 'A': case 'a': channels[3] = i; break;
            default:
                std::cerr << mod_header() << "CrystalWindow_PresentImage: unrecognized channel '" << pf[i] << "'. Recognized channels are R, G, B, A.";
                return;
        }
    }

    for (int32_t i = 0; i < 4; i++) {
        if (channels[i] == -1) {
            std::cerr << mod_header() << "CrystalWindow_PresentImage: missing channel. Recognized string is a combination of the characters R,G,B,A in any order followed by a colon and 'byte', such as BGRA:byte.";
            return;
        }
    }

    bool needs_conversion = !(channels[0] == 2 && channels[1] == 1 && channels[2] == 0 && channels[3] == 3);
    P_ELEMENTS(uint8_t)  bitmap_data;

    if (!needs_conversion) {
        bitmap_data = pixdata;
    } else {
        bitmap_data = new uint8_t[width * height * 4];
        for (int32_t y = 0; y < height; y++) {
            for (int32_t x = 0; x < width; x++) {
                int32_t src_index = (y * width + x) * 4;
                int32_t dst_index = src_index;

                bitmap_data[dst_index + 0] = pixdata[src_index + channels[2]]; // B
                bitmap_data[dst_index + 1] = pixdata[src_index + channels[1]]; // G
                bitmap_data[dst_index + 2] = pixdata[src_index + channels[0]]; // R
                bitmap_data[dst_index + 3] = pixdata[src_index + channels[3]]; // A
            }
        }
    }

    BITMAPINFO bmi;
    memset(&bmi, 0, sizeof(bmi));
    bmi.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
    bmi.bmiHeader.biWidth = width;
    bmi.bmiHeader.biHeight = -height; // Top-down image
    bmi.bmiHeader.biPlanes = 1;
    bmi.bmiHeader.biBitCount = 32; // 32 bits per pixel
    bmi.bmiHeader.biCompression = BI_RGB;

    StretchDIBits(hdc, 0, 0, width, height, 0, 0, width, height, bitmap_data, &bmi, DIB_RGB_COLORS, SRCCOPY);

    if (needs_conversion) {
        delete[] bitmap_data;
    }

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
