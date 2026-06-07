// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include "CrystalCatalystLibrary/Windowing/ScreenCapture.h"
#include "../Platform.h"

// Windows screen capture uses GDI (BitBlt) for compatibility.
// EnumDisplayMonitors is used for display enumeration.

namespace NewAge {

static bool capture_pix_free(uint8_t* p) {
    delete[] p;
    return true;
}

// ─────────────────────────────────────────────────────────────────────────────

struct MonitorEnumContext {
    std::vector<HMONITOR> monitors;
};

static BOOL CALLBACK MonitorEnumProc(HMONITOR hmon, HDC, LPRECT, LPARAM lParam) {
    auto* ctx = reinterpret_cast<MonitorEnumContext*>(lParam);
    ctx->monitors.push_back(hmon);
    return TRUE;
}

static std::vector<HMONITOR> enumerate_monitors() {
    MonitorEnumContext ctx;
    EnumDisplayMonitors(nullptr, nullptr, MonitorEnumProc,
        reinterpret_cast<LPARAM>(&ctx));
    return ctx.monitors;
}

static PixData capture_rect(int x, int y, int w, int h) {
    if (w <= 0 || h <= 0) return PixData{};

    HDC screen_dc = GetDC(nullptr);
    HDC mem_dc    = CreateCompatibleDC(screen_dc);

    BITMAPINFOHEADER bi{};
    bi.biSize        = sizeof(BITMAPINFOHEADER);
    bi.biWidth       = w;
    bi.biHeight      = -h;  // top-down
    bi.biPlanes      = 1;
    bi.biBitCount    = 32;
    bi.biCompression = BI_RGB;

    void* bits = nullptr;
    HBITMAP bmp = CreateDIBSection(screen_dc, reinterpret_cast<BITMAPINFO*>(&bi),
                                   DIB_RGB_COLORS, &bits, nullptr, 0);

    HBITMAP old_bmp = static_cast<HBITMAP>(SelectObject(mem_dc, bmp));
    BitBlt(mem_dc, 0, 0, w, h, screen_dc, x, y, SRCCOPY);
    SelectObject(mem_dc, old_bmp);

    // DIBSection gives us BGRA (actually BGRX — alpha channel is 0).
    // Set alpha to 0xFF and copy to a managed buffer.
    size_t size = (size_t)w * h * 4;
    uint8_t* buf = new uint8_t[size];
    const uint8_t* src = static_cast<const uint8_t*>(bits);
    for (size_t i = 0; i < size; i += 4) {
        buf[i + 0] = src[i + 0]; // B
        buf[i + 1] = src[i + 1]; // G
        buf[i + 2] = src[i + 2]; // R
        buf[i + 3] = 0xFF;       // A
    }

    DeleteObject(bmp);
    DeleteDC(mem_dc);
    ReleaseDC(nullptr, screen_dc);

    PixData result{};
    result.pix_format      = utf8_string_struct("bgra:int8");
    result.pix_data        = buf;
    result.pix_data_length = size;
    result.width           = w;
    result.height          = h;
    result.pix_data_free   = capture_pix_free;
    return result;
}

// ─────────────────────────────────────────────────────────────────────────────

int32_t CrystalWindow_GetDisplayCount() {
    return static_cast<int32_t>(enumerate_monitors().size());
}

DisplayInfo CrystalWindow_GetDisplayInfo(int32_t display_index) {
    DisplayInfo info{};
    info.index = display_index;

    auto monitors = enumerate_monitors();
    if (display_index < 0 || display_index >= static_cast<int32_t>(monitors.size()))
        return info;

    MONITORINFO mi{};
    mi.cbSize = sizeof(MONITORINFO);
    if (GetMonitorInfo(monitors[display_index], &mi)) {
        info.x          = mi.rcMonitor.left;
        info.y          = mi.rcMonitor.top;
        info.width      = mi.rcMonitor.right  - mi.rcMonitor.left;
        info.height     = mi.rcMonitor.bottom - mi.rcMonitor.top;
        info.is_primary = (mi.dwFlags & MONITORINFOF_PRIMARY) != 0;
    }
    return info;
}

PixData CrystalWindow_CaptureDisplay(int32_t display_index) {
    auto monitors = enumerate_monitors();
    if (display_index < 0 || display_index >= static_cast<int32_t>(monitors.size()))
        return PixData{};

    MONITORINFO mi{};
    mi.cbSize = sizeof(MONITORINFO);
    if (!GetMonitorInfo(monitors[display_index], &mi)) return PixData{};

    int x = mi.rcMonitor.left;
    int y = mi.rcMonitor.top;
    int w = mi.rcMonitor.right  - mi.rcMonitor.left;
    int h = mi.rcMonitor.bottom - mi.rcMonitor.top;
    return capture_rect(x, y, w, h);
}

PixData CrystalWindow_CaptureActiveWindow() {
    HWND hwnd = GetForegroundWindow();
    if (!hwnd) return PixData{};

    RECT r{};
    if (!GetWindowRect(hwnd, &r)) return PixData{};

    int x = r.left;
    int y = r.top;
    int w = r.right  - r.left;
    int h = r.bottom - r.top;
    return capture_rect(x, y, w, h);
}

} // namespace NewAge
