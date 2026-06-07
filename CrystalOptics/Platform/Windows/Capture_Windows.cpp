// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include "CrystalOptics/CaptureAPI.h"
#include <windows.h>
#include <vector>

namespace CrystalOptics {

static bool capture_pix_free(uint8_t* p) { delete[] p; return true; }

static std::vector<HMONITOR> enumerate_monitors() {
    struct Ctx { std::vector<HMONITOR> list; };
    Ctx ctx;
    EnumDisplayMonitors(nullptr, nullptr, [](HMONITOR m, HDC, LPRECT, LPARAM lp) -> BOOL {
        reinterpret_cast<Ctx*>(lp)->list.push_back(m);
        return TRUE;
    }, reinterpret_cast<LPARAM>(&ctx));
    return ctx.list;
}

static PixData gdi_capture(int x, int y, int w, int h) {
    if (w <= 0 || h <= 0) return PixData{};
    HDC screen = GetDC(nullptr);
    HDC mem    = CreateCompatibleDC(screen);

    BITMAPINFOHEADER bi{};
    bi.biSize = sizeof(bi); bi.biWidth = w; bi.biHeight = -h;
    bi.biPlanes = 1; bi.biBitCount = 32; bi.biCompression = BI_RGB;

    void* bits = nullptr;
    HBITMAP bmp = CreateDIBSection(screen, reinterpret_cast<BITMAPINFO*>(&bi),
                                   DIB_RGB_COLORS, &bits, nullptr, 0);
    SelectObject(mem, bmp);
    BitBlt(mem, 0, 0, w, h, screen, x, y, SRCCOPY);

    size_t size = (size_t)w * h * 4;
    uint8_t* buf = new uint8_t[size];
    const uint8_t* src = static_cast<const uint8_t*>(bits);
    for (size_t i = 0; i < size; i += 4) {
        buf[i+0] = src[i+0]; buf[i+1] = src[i+1];
        buf[i+2] = src[i+2]; buf[i+3] = 0xFF;
    }

    DeleteObject(bmp); DeleteDC(mem); ReleaseDC(nullptr, screen);

    PixData r{};
    r.pix_format = utf8_string_struct("bgra:int8");
    r.pix_data = buf; r.pix_data_length = size;
    r.width = w; r.height = h;
    r.pix_data_free = capture_pix_free;
    return r;
}

int32_t Capture_GetDisplayCount() {
    return static_cast<int32_t>(enumerate_monitors().size());
}

DisplayInfo Capture_GetDisplayInfo(int32_t index) {
    DisplayInfo info{}; info.index = index;
    auto monitors = enumerate_monitors();
    if (index < 0 || index >= static_cast<int32_t>(monitors.size())) return info;
    MONITORINFO mi{}; mi.cbSize = sizeof(mi);
    if (GetMonitorInfo(monitors[index], &mi)) {
        info.x = mi.rcMonitor.left; info.y = mi.rcMonitor.top;
        info.width  = mi.rcMonitor.right  - mi.rcMonitor.left;
        info.height = mi.rcMonitor.bottom - mi.rcMonitor.top;
        info.is_primary = (mi.dwFlags & MONITORINFOF_PRIMARY) != 0;
    }
    return info;
}

PixData Capture_Desktop() {
    int w = GetSystemMetrics(SM_CXVIRTUALSCREEN);
    int h = GetSystemMetrics(SM_CYVIRTUALSCREEN);
    int x = GetSystemMetrics(SM_XVIRTUALSCREEN);
    int y = GetSystemMetrics(SM_YVIRTUALSCREEN);
    return gdi_capture(x, y, w, h);
}

PixData Capture_Display(int32_t index) {
    auto monitors = enumerate_monitors();
    if (index < 0 || index >= static_cast<int32_t>(monitors.size()))
        return Capture_Desktop();
    MONITORINFO mi{}; mi.cbSize = sizeof(mi);
    if (!GetMonitorInfo(monitors[index], &mi)) return Capture_Desktop();
    return gdi_capture(mi.rcMonitor.left, mi.rcMonitor.top,
                       mi.rcMonitor.right  - mi.rcMonitor.left,
                       mi.rcMonitor.bottom - mi.rcMonitor.top);
}

PixData Capture_ActiveWindow() {
    HWND hwnd = GetForegroundWindow();
    if (!hwnd) return PixData{};
    RECT r{}; GetWindowRect(hwnd, &r);
    return gdi_capture(r.left, r.top, r.right - r.left, r.bottom - r.top);
}

// No portal on Windows — delegate to the full desktop GDI capture.
PixData Capture_Portal() {
    return Capture_Desktop();
}

} // namespace CrystalOptics
