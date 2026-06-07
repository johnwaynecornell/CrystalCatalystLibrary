// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include "CrystalOptics/CaptureAPI.h"

#include <X11/Xlib.h>
#include <X11/Xutil.h>

#ifdef HAVE_XRANDR
#include <X11/extensions/Xrandr.h>
#endif

#include <algorithm>
#include <cstdlib>
#include <cstring>

namespace CrystalOptics {

// ─── helpers ────────────────────────────────────────────────────────────────

static bool capture_pix_free(uint8_t* p) { delete[] p; return true; }

// Suppress X errors for operations that may not be supported by the server
// (virtual displays, compositors that block framebuffer reads, etc.)
static bool s_x_error = false;
static int x_error_suppress(Display*, XErrorEvent*) {
    s_x_error = true; return 0;
}

static XImage* safe_get_image(Display* dpy, Window root,
                              int x, int y, unsigned w, unsigned h) {
    s_x_error = false;
    auto old = XSetErrorHandler(x_error_suppress);
    XImage* img = XGetImage(dpy, root, x, y, w, h, AllPlanes, ZPixmap);
    XSync(dpy, False);
    XSetErrorHandler(old);
    if (s_x_error && img) { XDestroyImage(img); img = nullptr; }
    return img;
}

static PixData ximage_to_pixdata(XImage* img) {
    if (!img) return PixData{};
    int w = img->width, h = img->height;
    size_t size = (size_t)w * h * 4;
    uint8_t* buf = new uint8_t[size];

    if (img->bits_per_pixel == 32) {
        int bs = __builtin_ctz((unsigned)img->blue_mask);
        int gs = __builtin_ctz((unsigned)img->green_mask);
        int rs = __builtin_ctz((unsigned)img->red_mask);
        for (int y = 0; y < h; y++) {
            const uint32_t* src = reinterpret_cast<const uint32_t*>(
                img->data + (size_t)y * img->bytes_per_line);
            uint8_t* dst = buf + (size_t)y * w * 4;
            for (int x = 0; x < w; x++, dst += 4) {
                uint32_t px = src[x];
                dst[0] = (uint8_t)((px & img->blue_mask)  >> bs);
                dst[1] = (uint8_t)((px & img->green_mask) >> gs);
                dst[2] = (uint8_t)((px & img->red_mask)   >> rs);
                dst[3] = 0xFF;
            }
        }
    } else {
        int bs = __builtin_ctz((unsigned)img->blue_mask);
        int gs = __builtin_ctz((unsigned)img->green_mask);
        int rs = __builtin_ctz((unsigned)img->red_mask);
        uint8_t* dst = buf;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++, dst += 4) {
                unsigned long px = XGetPixel(img, x, y);
                dst[0] = (uint8_t)((px & img->blue_mask)  >> bs);
                dst[1] = (uint8_t)((px & img->green_mask) >> gs);
                dst[2] = (uint8_t)((px & img->red_mask)   >> rs);
                dst[3] = 0xFF;
            }
    }

    PixData r{};
    r.pix_format     = utf8_string_struct("bgra:int8");
    r.pix_data       = buf;
    r.pix_data_length = size;
    r.width          = w;
    r.height         = h;
    r.pix_data_free  = capture_pix_free;
    return r;
}

static PixData capture_rect(Display* dpy, Window root,
                            int root_w, int root_h,
                            int x, int y, int w, int h) {
    // Clamp to actual root window bounds
    x = std::clamp(x, 0, root_w - 1);
    y = std::clamp(y, 0, root_h - 1);
    w = std::min(w, root_w - x);
    h = std::min(h, root_h - y);
    if (w <= 0 || h <= 0) return PixData{};
    XImage* img = safe_get_image(dpy, root, x, y, (unsigned)w, (unsigned)h);
    PixData r = ximage_to_pixdata(img);
    if (img) XDestroyImage(img);
    return r;
}

// ─── detect Wayland ─────────────────────────────────────────────────────────

static bool is_wayland_session() {
    const char* wd = std::getenv("WAYLAND_DISPLAY");
    const char* st = std::getenv("XDG_SESSION_TYPE");
    if (wd && wd[0]) return true;
    if (st && std::string(st) == "wayland") return true;
    return false;
}

// ─── exports ────────────────────────────────────────────────────────────────

int32_t Capture_GetDisplayCount() {
    Display* dpy = XOpenDisplay(nullptr);
    if (!dpy) return 1;
    int count = 1;
#ifdef HAVE_XRANDR
    Window root = DefaultRootWindow(dpy);
    int n = 0;
    XRRMonitorInfo* m = XRRGetMonitors(dpy, root, True, &n);
    if (m) { count = n; XRRFreeMonitors(m); }
#endif
    XCloseDisplay(dpy);
    return count;
}

DisplayInfo Capture_GetDisplayInfo(int32_t index) {
    DisplayInfo info{}; info.index = index;
    Display* dpy = XOpenDisplay(nullptr);
    if (!dpy) return info;
    int screen = DefaultScreen(dpy);
#ifdef HAVE_XRANDR
    Window root = RootWindow(dpy, screen);
    int n = 0;
    XRRMonitorInfo* m = XRRGetMonitors(dpy, root, True, &n);
    if (m && index >= 0 && index < n) {
        info.x = m[index].x; info.y = m[index].y;
        info.width = m[index].width; info.height = m[index].height;
        info.is_primary = (m[index].primary != 0);
        XRRFreeMonitors(m);
        XCloseDisplay(dpy);
        return info;
    }
    if (m) XRRFreeMonitors(m);
#endif
    if (index == 0) {
        info.width = DisplayWidth(dpy, screen);
        info.height = DisplayHeight(dpy, screen);
        info.is_primary = true;
    }
    XCloseDisplay(dpy);
    return info;
}

PixData Capture_Desktop() {
    if (is_wayland_session()) {
        // Wayland: try the XDG portal first.
        PixData r = Capture_Portal();
        if (r) return r;
        // Portal unavailable — attempt XWayland fallback.
        if (!std::getenv("DISPLAY")) return PixData{};
    }

    Display* dpy = XOpenDisplay(nullptr);
    if (!dpy) return PixData{};

    int screen = DefaultScreen(dpy);
    Window root = RootWindow(dpy, screen);
    int w = DisplayWidth(dpy, screen);
    int h = DisplayHeight(dpy, screen);

    PixData r = capture_rect(dpy, root, w, h, 0, 0, w, h);
    XCloseDisplay(dpy);
    return r;
}

PixData Capture_Display(int32_t index) {
    if (is_wayland_session()) {
        // Portal captures the full screen; use it and note display selection is not supported.
        PixData r = Capture_Portal();
        if (r) return r;
        if (!std::getenv("DISPLAY")) return PixData{};
    }

    Display* dpy = XOpenDisplay(nullptr);
    if (!dpy) return PixData{};

    int screen = DefaultScreen(dpy);
    Window root = RootWindow(dpy, screen);
    int root_w = DisplayWidth(dpy, screen);
    int root_h = DisplayHeight(dpy, screen);

    int x = 0, y = 0, w = root_w, h = root_h;

#ifdef HAVE_XRANDR
    int n = 0;
    XRRMonitorInfo* m = XRRGetMonitors(dpy, root, True, &n);
    if (m && index >= 0 && index < n) {
        x = m[index].x; y = m[index].y;
        w = m[index].width; h = m[index].height;
    }
    if (m) XRRFreeMonitors(m);
#endif

    PixData r = capture_rect(dpy, root, root_w, root_h, x, y, w, h);
    // If clamped to nothing, fall back to full desktop
    if (!r) r = capture_rect(dpy, root, root_w, root_h, 0, 0, root_w, root_h);
    XCloseDisplay(dpy);
    return r;
}

PixData Capture_ActiveWindow() {
    if (is_wayland_session()) {
        PixData r = Capture_Portal();
        if (r) return r;
        if (!std::getenv("DISPLAY")) return PixData{};
    }

    Display* dpy = XOpenDisplay(nullptr);
    if (!dpy) return PixData{};

    Window focused; int revert;
    XGetInputFocus(dpy, &focused, &revert);
    if (focused == None || focused == PointerRoot) {
        XCloseDisplay(dpy); return PixData{};
    }

    XWindowAttributes attrs;
    if (!XGetWindowAttributes(dpy, focused, &attrs)) {
        XCloseDisplay(dpy); return PixData{};
    }

    Window root = DefaultRootWindow(dpy);
    int abs_x = 0, abs_y = 0; Window child;
    XTranslateCoordinates(dpy, focused, root, 0, 0, &abs_x, &abs_y, &child);

    int screen = DefaultScreen(dpy);
    int root_w = DisplayWidth(dpy, screen);
    int root_h = DisplayHeight(dpy, screen);

    PixData r = capture_rect(dpy, root, root_w, root_h,
                             abs_x, abs_y, attrs.width, attrs.height);
    XCloseDisplay(dpy);
    return r;
}

} // namespace CrystalOptics
