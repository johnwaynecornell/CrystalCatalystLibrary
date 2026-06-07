// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include "CrystalCatalystLibrary/Windowing/ScreenCapture.h"
#include "../Platform.h"

#include <X11/Xlib.h>
#include <X11/Xutil.h>

#ifdef HAVE_XRANDR
#include <X11/extensions/Xrandr.h>
#endif

#include <algorithm>
#include <cstring>

namespace NewAge {

static bool capture_pix_free(uint8_t* p) {
    delete[] p;
    return true;
}

// X error suppression for graceful failure when XGetImage is unsupported
// (e.g., virtual/compositing displays that don't allow framebuffer reads).
static bool s_x_error = false;
static int x_error_suppress(Display*, XErrorEvent*) {
    s_x_error = true;
    return 0;
}

static XImage* safe_get_image(Display* dpy, Drawable d,
                              int x, int y, unsigned w, unsigned h) {
    s_x_error = false;
    auto old = XSetErrorHandler(x_error_suppress);
    XImage* img = XGetImage(dpy, d, x, y, w, h, AllPlanes, ZPixmap);
    XSync(dpy, False);
    XSetErrorHandler(old);
    if (s_x_error) {
        if (img) { XDestroyImage(img); img = nullptr; }
    }
    return img;
}

// Convert an XImage to PixData (bgra:int8). Caller owns the returned PixData.
static PixData ximage_to_pixdata(XImage* img) {
    if (!img) return PixData{};

    int w = img->width;
    int h = img->height;
    size_t size = (size_t)w * h * 4;
    uint8_t* buf = new uint8_t[size];

    if (img->bits_per_pixel == 32) {
        // Fast path — direct channel extraction using colour masks
        int blue_shift  = __builtin_ctz((unsigned)img->blue_mask);
        int green_shift = __builtin_ctz((unsigned)img->green_mask);
        int red_shift   = __builtin_ctz((unsigned)img->red_mask);

        for (int y = 0; y < h; y++) {
            const uint32_t* src = reinterpret_cast<const uint32_t*>(
                img->data + (size_t)y * img->bytes_per_line);
            uint8_t* dst = buf + (size_t)y * w * 4;
            for (int x = 0; x < w; x++, dst += 4) {
                uint32_t pixel = src[x];
                dst[0] = static_cast<uint8_t>((pixel & img->blue_mask)  >> blue_shift);
                dst[1] = static_cast<uint8_t>((pixel & img->green_mask) >> green_shift);
                dst[2] = static_cast<uint8_t>((pixel & img->red_mask)   >> red_shift);
                dst[3] = 0xFF;
            }
        }
    } else {
        // Fallback for unusual depths
        int blue_shift  = __builtin_ctz((unsigned)img->blue_mask);
        int green_shift = __builtin_ctz((unsigned)img->green_mask);
        int red_shift   = __builtin_ctz((unsigned)img->red_mask);
        uint8_t* dst = buf;
        for (int y = 0; y < h; y++) {
            for (int x = 0; x < w; x++, dst += 4) {
                unsigned long pixel = XGetPixel(img, x, y);
                dst[0] = static_cast<uint8_t>((pixel & img->blue_mask)  >> blue_shift);
                dst[1] = static_cast<uint8_t>((pixel & img->green_mask) >> green_shift);
                dst[2] = static_cast<uint8_t>((pixel & img->red_mask)   >> red_shift);
                dst[3] = 0xFF;
            }
        }
    }

    PixData result{};
    result.pix_format    = utf8_string_struct("bgra:int8");
    result.pix_data      = buf;
    result.pix_data_length = size;
    result.width         = w;
    result.height        = h;
    result.pix_data_free = capture_pix_free;
    return result;
}

// ─────────────────────────────────────────────────────────────────────────────

int32_t CrystalWindow_GetDisplayCount() {
    Display* dpy = XOpenDisplay(nullptr);
    if (!dpy) return 1;

    int count = 1;

#ifdef HAVE_XRANDR
    Window root = DefaultRootWindow(dpy);
    int nmonitors = 0;
    XRRMonitorInfo* monitors = XRRGetMonitors(dpy, root, True, &nmonitors);
    if (monitors) {
        count = nmonitors;
        XRRFreeMonitors(monitors);
    }
#endif

    XCloseDisplay(dpy);
    return count;
}

DisplayInfo CrystalWindow_GetDisplayInfo(int32_t display_index) {
    DisplayInfo info{};
    info.index = display_index;

    Display* dpy = XOpenDisplay(nullptr);
    if (!dpy) return info;

    int screen = DefaultScreen(dpy);

#ifdef HAVE_XRANDR
    Window root = RootWindow(dpy, screen);
    int nmonitors = 0;
    XRRMonitorInfo* monitors = XRRGetMonitors(dpy, root, True, &nmonitors);
    if (monitors && display_index >= 0 && display_index < nmonitors) {
        info.x          = monitors[display_index].x;
        info.y          = monitors[display_index].y;
        info.width      = monitors[display_index].width;
        info.height     = monitors[display_index].height;
        info.is_primary = (monitors[display_index].primary != 0);
        XRRFreeMonitors(monitors);
        XCloseDisplay(dpy);
        return info;
    }
    if (monitors) XRRFreeMonitors(monitors);
#endif

    // Fallback: single display
    if (display_index == 0) {
        info.width      = DisplayWidth(dpy, screen);
        info.height     = DisplayHeight(dpy, screen);
        info.is_primary = true;
    }
    XCloseDisplay(dpy);
    return info;
}

PixData CrystalWindow_CaptureDisplay(int32_t display_index) {
    Display* dpy = XOpenDisplay(nullptr);
    if (!dpy) return PixData{};

    int screen = DefaultScreen(dpy);
    Window root = RootWindow(dpy, screen);

    int root_w = DisplayWidth(dpy, screen);
    int root_h = DisplayHeight(dpy, screen);

    int cap_x = 0, cap_y = 0;
    int cap_w = root_w;
    int cap_h = root_h;

#ifdef HAVE_XRANDR
    int nmonitors = 0;
    XRRMonitorInfo* monitors = XRRGetMonitors(dpy, root, True, &nmonitors);
    if (monitors && display_index >= 0 && display_index < nmonitors) {
        cap_x = monitors[display_index].x;
        cap_y = monitors[display_index].y;
        cap_w = monitors[display_index].width;
        cap_h = monitors[display_index].height;
    }
    if (monitors) XRRFreeMonitors(monitors);

    // Clip to the root window's actual framebuffer — XRandR virtual monitors
    // may extend beyond what XGetImage can reach in some environments.
    cap_x = std::clamp(cap_x, 0, root_w - 1);
    cap_y = std::clamp(cap_y, 0, root_h - 1);
    cap_w = std::min(cap_w, root_w - cap_x);
    cap_h = std::min(cap_h, root_h - cap_y);
#endif

    if (cap_w <= 0 || cap_h <= 0) {
        XCloseDisplay(dpy);
        return PixData{};
    }

    XImage* img = safe_get_image(dpy, root, cap_x, cap_y, cap_w, cap_h);
    PixData result = ximage_to_pixdata(img);
    if (img) XDestroyImage(img);
    XCloseDisplay(dpy);
    return result;
}

PixData CrystalWindow_CaptureActiveWindow() {
    Display* dpy = XOpenDisplay(nullptr);
    if (!dpy) return PixData{};

    Window focused;
    int revert_to;
    XGetInputFocus(dpy, &focused, &revert_to);

    if (focused == None || focused == PointerRoot) {
        XCloseDisplay(dpy);
        return PixData{};
    }

    XWindowAttributes attrs;
    if (!XGetWindowAttributes(dpy, focused, &attrs)) {
        XCloseDisplay(dpy);
        return PixData{};
    }

    // Translate window-local (0,0) to root/screen coordinates
    Window root = DefaultRootWindow(dpy);
    int abs_x = 0, abs_y = 0;
    Window child;
    XTranslateCoordinates(dpy, focused, root, 0, 0, &abs_x, &abs_y, &child);

    int screen = DefaultScreen(dpy);
    int screen_w = DisplayWidth(dpy, screen);
    int screen_h = DisplayHeight(dpy, screen);

    int cap_x = std::max(0, abs_x);
    int cap_y = std::max(0, abs_y);
    int cap_w = std::min(attrs.width,  screen_w - cap_x);
    int cap_h = std::min(attrs.height, screen_h - cap_y);

    if (cap_w <= 0 || cap_h <= 0) {
        XCloseDisplay(dpy);
        return PixData{};
    }

    XImage* img = safe_get_image(dpy, root, cap_x, cap_y, cap_w, cap_h);
    PixData result = ximage_to_pixdata(img);
    if (img) XDestroyImage(img);
    XCloseDisplay(dpy);
    return result;
}

} // namespace NewAge
