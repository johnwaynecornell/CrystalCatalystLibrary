// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include "../CrystalApplication_X11.h"
#include "CrystalWindow_X11.h"
#include "DragProvide_X11.h"
#include <unistd.h>
#include <iostream>

#include <GL/gl.h>
#include <GL/glxext.h>
#include <cstring>
#include <vector>
#include <cstdio>

#include "DragDrop_X11.h"

#ifdef mod_header
#undef mod_header
#endif

#define mod_header() "CrystalWindow_X11:"

#include <iomanip>
#include <X11/XKBlib.h>
#include <X11/keysym.h>
#include <X11/cursorfont.h>
#include <X11/Xatom.h>
#include <X11/Xcursor/Xcursor.h>

using namespace JWCEssentials;

namespace NewAge {
    CrystalWindow_X11::CrystalWindow_X11() {}

    CrystalWindow_X11::~CrystalWindow_X11() {
        if (drag_provide) delete drag_provide;
        if (gl_context) {
            glXMakeCurrent(display, None, nullptr);
            glXDestroyContext(display, gl_context);
        }
        if (gl_visual_info) XFree(gl_visual_info);
        if (gl_colormap) XFreeColormap(display, gl_colormap);
    }

    void CrystalWindow_X11::Show(bool restore) {
        XRaiseWindow(display, window);
    }

    void CrystalWindow_X11::Close(){
        if (is_closed) return;
        is_closed = true;

        if (callbacks.on_close) {
            callbacks.on_close(myHandle);
        }

        XDestroyWindow(display, window);
    }

    void CrystalWindow_X11::PostClose() {
        XEvent event;
        memset(&event, 0, sizeof(event));
        event.type = ClientMessage;
        event.xclient.display = display;
        event.xclient.window = window;

        event.xclient.format = 32;

        event.xclient.data.l[0] = AppX11->atoms.window._delete;
        event.xclient.data.l[1] = 0;
        event.xclient.data.l[2] = 0;
        event.xclient.data.l[3] = 0;
        event.xclient.data.l[4] = 0;

        XSendEvent(display, window, False, NoEventMask, &event);
        XFlush(display);
    }

    void CrystalWindow_X11::SetSize(int32_t width, int32_t height) {
        XResizeWindow(display, window, (unsigned int)width, (unsigned int)height);
        XFlush(display);
    }

    void CrystalWindow_X11::GetSize(int32_t& width, int32_t& height) {
        XWindowAttributes gwa;
        XGetWindowAttributes(display, window, &gwa);
        width = gwa.width;
        height = gwa.height;
    }

    void CrystalWindow_X11::SetLocation(int32_t x, int32_t y) {
        XMoveWindow(display, window, x, y);
        XFlush(display);
    }

    void CrystalWindow_X11::GetLocation(int32_t& x, int32_t& y) {
        Window child;
        XTranslateCoordinates(display, window, DefaultRootWindow(display), 0, 0, &x, &y, &child);
    }

    void CrystalWindow_X11::SetCursor(utf8_string_struct pixformat, P_ELEMENTS(void) pixdata, size_t pixdata_length, int32_t width, int32_t height, int32_t hot_x, int32_t hot_y) {
        if (!pixdata || !pixformat) return;
        PixData proxy = Pixels_ConvertPixels(pixformat, "bgra:int8", pixdata, pixdata_length, width, height);
        if (!proxy && proxy.pix_format.c_str && strcmp(proxy.pix_format.c_str, "error") == 0) return;

        XcursorImage* image = XcursorImageCreate(width, height);
        if (!image) {
            if (proxy) proxy.free();
            return;
        }

        image->xhot = hot_x;
        image->yhot = hot_y;

        uint32_t* src = (uint32_t*) (proxy ? proxy.pix_data : pixdata);
        for (int i = 0; i < width * height; i++) {
            image->pixels[i] = src[i];
        }

        Cursor cursor = XcursorImageLoadCursor(display, image);
        XDefineCursor(display, window, cursor);
        XFreeCursor(display, cursor);
        XcursorImageDestroy(image);
        XFlush(display);
        if (proxy) proxy.free();
    }

    void CrystalWindow_X11::SetStandardCursor(CrystalCursor cursor_enum) {
        unsigned int shape;
        switch (cursor_enum) {
            case CRYSTAL_CURSOR_ARROW: shape = XC_left_ptr; break;
            case CRYSTAL_CURSOR_TEXT: shape = XC_xterm; break;
            case CRYSTAL_CURSOR_WAIT: shape = XC_watch; break;
            case CRYSTAL_CURSOR_CROSSHAIR: shape = XC_crosshair; break;
            case CRYSTAL_CURSOR_MOVE: shape = XC_fleur; break;
            case CRYSTAL_CURSOR_NWSE_RESIZE: shape = XC_bottom_right_corner; break;
            case CRYSTAL_CURSOR_NESW_RESIZE: shape = XC_bottom_left_corner; break;
            case CRYSTAL_CURSOR_WE_RESIZE: shape = XC_sb_h_double_arrow; break;
            case CRYSTAL_CURSOR_NS_RESIZE: shape = XC_sb_v_double_arrow; break;
            case CRYSTAL_CURSOR_HAND: shape = XC_hand2; break;
            case CRYSTAL_CURSOR_NOT_ALLOWED: shape = XC_circle; break;
            default: shape = XC_left_ptr; break;
        }
        Cursor cursor = XCreateFontCursor(display, shape);
        XDefineCursor(display, window, cursor);
        XFreeCursor(display, cursor);
        XFlush(display);
    }

    void CrystalWindow_X11::SetIcon(utf8_string_struct pixformat, P_ELEMENTS(void) pixdata, size_t pixdata_length, int32_t width, int32_t height) {
        if (!pixdata || !pixformat || width <= 0 || height <= 0) return;
        if (!AppX11) return;

        PixData proxy = Pixels_ConvertPixels(pixformat, "bgra:int8", pixdata, pixdata_length, width, height);
        if (!proxy && proxy.pix_format.c_str && strcmp(proxy.pix_format.c_str, "error") == 0) return;

        int num_pixels = width * height;
        std::vector<unsigned long> icon_data;
        icon_data.resize(2 + num_pixels);
        icon_data[0] = (unsigned long)width;
        icon_data[1] = (unsigned long)height;

        uint32_t* src = (uint32_t*) (proxy ? proxy.pix_data : pixdata);
        for (int i = 0; i < num_pixels; i++) {
            uint32_t bgra = src[i];
            uint32_t b = (bgra >> 0) & 0xFF;
            uint32_t g = (bgra >> 8) & 0xFF;
            uint32_t r = (bgra >> 16) & 0xFF;
            uint32_t a = (bgra >> 24) & 0xFF;
            uint32_t argb = (a << 24) | (r << 16) | (g << 8) | b;
            icon_data[i + 2] = (unsigned long)argb;
        }

        XChangeProperty(display, window, AppX11->atoms.ewmh.net_wm_icon, XA_CARDINAL, 32, PropModeReplace, (unsigned char*)icon_data.data(), icon_data.size());

        XFlush(display);
        if (proxy) proxy.free();

    }

    void CrystalWindow_X11::SetTitle(utf8_string_struct title) {
        XStoreName(display, window, title);
        XFlush(display);
    }

    void CrystalWindow_X11::GetTitle(P_OUT(utf8_string_struct) title) {
        char* name = nullptr;
        if (XFetchName(display, window, &name)) {
            *title = name;
            XFree(name);
        } else {
            *title = "";
        }
    }

    // Function to convert X11 keycode to Unicode
    int32_t ConvertKeyCodeToUnicode(P_INSTANCE(XEvent)  event) {
        char buffer[4];
        KeySym keySym;
        XLookupString(&event->xkey, buffer, sizeof(buffer), &keySym, nullptr);

        // Check if the keysym is a valid Unicode code point
        if (keySym >= 0x20 && keySym <= 0x10FFFF) {
            return static_cast<int>(keySym);
        }
        return 0;
    }

    /*
    void handle_xevent(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(XEvent)  event) {
        if (!window_handle) {
            std::cerr << "Invalid window_handle in handle_xevent" << std::endl;
            return;
        }

        auto* callbacks = get_callbacks(window_handle);

        switch (event->type) {
            case Expose:
                if (callbacks->on_draw) {
                    callbacks->on_draw(window_handle);
                }
                break;
            case KeyPress:
                if (callbacks->on_key_down) {
                    callbacks->on_key_down(window_handle, event->xkey.keycode);
                }
                break;
            case KeyRelease:
                if (callbacks->on_key_up) {
                    callbacks->on_key_up(window_handle, event->xkey.keycode);
                }
                break;
            case MotionNotify:
                if (callbacks->on_mouse_move) {
                    callbacks->on_mouse_move(window_handle, event->xmotion.x, event->xmotion.y);
                }
                break;
            case ButtonPress:
                if (callbacks->on_mouse_down) {
                    callbacks->on_mouse_down(window_handle, event->xbutton.button, event->xbutton.x, event->xbutton.y);
                }
                break;
            case ButtonRelease:
                if (callbacks->on_mouse_up) {
                    callbacks->on_mouse_up(window_handle, event->xbutton.button, event->xbutton.x, event->xbutton.y);
                }
                break;
            case ConfigureNotify:
                if (callbacks->on_resize) {
                    callbacks->on_resize(window_handle, event->xconfigure.width, event->xconfigure.height);
                }
                break;
            case FocusIn:
                if (callbacks->on_focus_in) {
                    callbacks->on_focus_in(window_handle);
                }
                break;
            case FocusOut:
                if (callbacks->on_focus_out) {
                    callbacks->on_focus_out(window_handle);
                }
                break;
            case ClientMessage:
                if (event->xclient.message_type == XInternAtom(window_handle->window->display, "XdndEnter", False)) {
                    if (callbacks->on_drag_enter) {
                        DragDropData data = { .type = DRAG_DROP_TEXT, .data.text = "Example" };  // Placeholder
                        callbacks->on_drag_enter(window_handle, &data);
                    }
                } else if (event->xclient.message_type == XInternAtom(window_handle->window->display, "XdndPosition", False)) {
                    if (callbacks->on_drag_motion) {
                        DragDropData data = { .type = DRAG_DROP_TEXT, .data.text = "Example" };  // Placeholder
                        callbacks->on_drag_motion(window_handle, &data);
                    }
                } else if (event->xclient.message_type == XInternAtom(window_handle->window->display, "XdndLeave", False)) {
                    if (callbacks->on_drag_leave) {
                        DragDropData data = { .type = DRAG_DROP_TEXT, .data.text = "Example" };  // Placeholder
                        callbacks->on_drag_leave(window_handle, &data);
                    }
                } else if (event->xclient.message_type == XInternAtom(window_handle->window->display, "XdndDrop", False)) {
                    if (callbacks->on_drag_drop) {
                        DragDropData data = { .type = DRAG_DROP_TEXT, .data.text = "Example" };  // Placeholder
                        callbacks->on_drag_drop(window_handle, &data);
                    }
                }
                break;
        }
    }*/

// CrystalWindow_X11.cpp
#include "CrystalWindow_X11.h"
// #include whatever defines AppX11->atoms.window.take_focus / .protocols

Time CrystalWindow_X11::get_user_time(XEvent* ev) {
    if (!ev) return last_user_time;

    switch (ev->type) {
        // Keyboard
        case KeyPress:
        case KeyRelease:
            return (last_user_time = ev->xkey.time);

        // Pointer
        case ButtonPress:
        case ButtonRelease:
            return (last_user_time = ev->xbutton.time);
        case MotionNotify:
            return (last_user_time = ev->xmotion.time);
        case EnterNotify:
        case LeaveNotify:
            return (last_user_time = ev->xcrossing.time);

            /*
        // Focus (XFocusChangeEvent has time)
        case FocusIn:
        case FocusOut:
            return (last_user_time = ev->xfocus.);
        */
        // Property/Selection (have time fields)
        case PropertyNotify:
            return (last_user_time = ev->xproperty.time);
        case SelectionRequest:
            return (last_user_time = ev->xselectionrequest.time);
        case SelectionNotify:
            return (last_user_time = ev->xselection.time);

        // Client messages: handle WM_TAKE_FOCUS (timestamp in data.l[1])
        case ClientMessage: {
            // Guard if you don’t keep these atoms:
            // if (!AppX11) break;
            Atom msg = ev->xclient.message_type;
            long a0  = ev->xclient.data.l[0];
            // Replace these with your actual atom references:
            //   AppX11->atoms.window.protocols
            //   AppX11->atoms.window.take_focus

            if (AppX11 && msg == AppX11->atoms.window.protocols) {
                if ((Atom)a0 == AppX11->atoms.window.take_focus) {
                    Time t = static_cast<Time>(ev->xclient.data.l[1]);
                    if (t != CurrentTime) last_user_time = t;
                    return last_user_time;
                }
            }
            break;
        }

        // No timestamps in these; fall through to return last known
        case Expose:
        case ConfigureNotify:
        case MapNotify:
        case UnmapNotify:
        case VisibilityNotify:
        case CreateNotify:
        case DestroyNotify:
        default:
            break;
    }
    return last_user_time;
}



    bool CrystalWindow_X11::handle_xevent(P_INSTANCE(XEvent)  event) {
        if (!received_first_message) { reset_uptime(); received_first_message = true; }

        get_user_time(event);


        if (drag_provide && drag_provide->dragging && drag_provide->handle_message(event)) return true;
        if (handle_drop_xevents(event)) return true;
        if (handle_selection_request(event)) return true;

        Atom NET_ACTIVE = XInternAtom(display, "_NET_ACTIVE_WINDOW", False);

        auto name = [&](Atom a){ if (a==None) return std::string("<None>");
            char* n = XGetAtomName(event->xproperty.display, a);
            std::string s = n ? n : "<null>"; if (n) XFree(n); return s; };

        int32_t unicodeChar;
        switch (event->type) {
            case Expose:
                if (callbacks.on_draw) {
                    callbacks.on_draw(myHandle);
                }

                draw_queued = false;
                ready = true;
                return true;
            case KeyPress:
                if (callbacks.on_key_down) {
                    unicodeChar = ConvertKeyCodeToUnicode(event);
                    std::cerr << mod_header() << "KeyPress, unicodeChar = " << unicodeChar << std::endl;

                    callbacks.on_key_down(myHandle, unicodeChar);
                }
            return true;
            case KeyRelease:
                if (callbacks.on_key_up) {
                    unicodeChar = ConvertKeyCodeToUnicode(event);
                    callbacks.on_key_up(myHandle, unicodeChar);
                }
            return true;
            case MotionNotify:
                if (callbacks.on_mouse_move) {
                    callbacks.on_mouse_move(myHandle, event->xmotion.x, event->xmotion.y);
                }
            return true;
            case ButtonPress:
                if (callbacks.on_mouse_down) {
                    callbacks.on_mouse_down(myHandle, event->xbutton.button, event->xbutton.x, event->xbutton.y);
                }
            return true;
            case ButtonRelease:
                if (callbacks.on_mouse_up) {
                    callbacks.on_mouse_up(myHandle, event->xbutton.button, event->xbutton.x, event->xbutton.y);
                }
            return true;
            case ConfigureNotify:
                if (callbacks.on_resize) {
                    if (width != event->xconfigure.width || height != event->xconfigure.height) {
                        bool r = width == 0;

                        width = event->xconfigure.width;
                        height = event->xconfigure.height;

                        callbacks.on_resize(myHandle, width, height);
                        if (r) QueueRedraw();
                    }
                }
            return true;
            case FocusIn:
                if (callbacks.on_focus_in) {
                    callbacks.on_focus_in(myHandle);
                }
            return true;
            case FocusOut:
                if (callbacks.on_focus_out) {
                    callbacks.on_focus_out(myHandle);
                }
            return true;
            case ClientMessage:
                if (event->xclient.data.l[0] == AppX11->atoms.window._delete) {
                    Close();

                    return true;
                }
        }

        return false;
    }

    void CrystalWindow_X11::PresentImage(utf8_string_struct pixformat, P_ELEMENTS(void)  pixdata, size_t pixdata_length, int32_t width, int32_t height)
    {
        if (!pixdata || !pixformat) return;

        PixData proxy= Pixels_ConvertPixels(pixformat, "bgra:int8", pixdata, pixdata_length, width, height);
        if (!proxy && proxy.pix_format.c_str && strcmp(proxy.pix_format.c_str, "error") == 0) return;

        XImage* ximage;

        P_ELEMENTS(void) m = proxy ? proxy.pix_data : pixdata;
        Visual* visual = gl_visual_info ? gl_visual_info->visual : DefaultVisual(display, DefaultScreen(display));
        int depth = gl_visual_info ? gl_visual_info->depth : 24;

        ximage = XCreateImage(display, visual, depth, ZPixmap, 0, (char *) m, width, height, 32, 0);

        if (!ximage) {
            std::cerr << mod_header() << "Failed to create XImage" << std::endl;
            if (proxy) proxy.free();
            return;
        }

        GC gc = XCreateGC(display, window, 0, nullptr);
        if (!gc) {
            std::cerr << mod_header() << "Failed to create Graphics Context" << std::endl;
            if (proxy) proxy.free();

            ximage->data = nullptr;
            XDestroyImage(ximage);
            return;
        }

        XPutImage(display, window, gc, ximage, 0, 0, 0, 0, width, height);
        XFreeGC(display, gc);

        if (proxy) proxy.free();

        ximage->data = nullptr;
        XDestroyImage(ximage);
        XFlush(display);

    }

    void CrystalWindow_X11::QueueRedraw()
    {
        if (draw_queued) return;
        draw_queued = true;

        // Trigger an Expose event to force a redraw
        XEvent event;
        event.type = Expose;
        event.xexpose.window = window;
        event.xexpose.x = 0;
        event.xexpose.y = 0;
        event.xexpose.width = 0;
        event.xexpose.height = 0;
        event.xexpose.count = 0;

        XSendEvent(display, window, False, ExposureMask, &event);
        XFlush(display);
    }

    void CrystalWindow_X11::MouseCapture() {
        // Capture the pointer
        int32_t result = XGrabPointer(
            display, window, True, ButtonPressMask | ButtonReleaseMask | PointerMotionMask,
            GrabModeAsync, GrabModeAsync, None, None, last_user_time
        );

        if (result != GrabSuccess) {
            std::cerr << mod_header() << "Failed to capture mouse pointer" << std::endl;
        } else {
            std::cerr << mod_header() << "Mouse pointer captured" << std::endl;
        }
    }

    void CrystalWindow_X11::MouseRelease() {
        // Release the pointer
        XUngrabPointer(display, last_user_time);
        std::cerr << mod_header() << "Mouse pointer released" << std::endl;
    }

    void CrystalWindow_X11::Activate() {
            // 1. Get the atomic ID for the EWMH active window protocol
            Atom net_active_win = XInternAtom(display, "_NET_ACTIVE_WINDOW", False);

            XEvent event;
            memset(&event, 0, sizeof(event));

            // 2. Construct the client message event
            event.type = ClientMessage;
            event.xclient.window = window;
            event.xclient.message_type = net_active_win;
            event.xclient.format = 32;       // Data is in 32-bit long format

            // EWMH Source indication: 1 = Application, 2 = Pager/Taskbar
            event.xclient.data.l[0] = 1;
            // Timestamp (CurrentTime is 0, passing a real X server timestamp is better)
            event.xclient.data.l[1] = 0;
            // Currently active window (0 means none/ignored)
            event.xclient.data.l[2] = 0;

            // 3. Send event to the Root Window so the Window Manager intercepts it
            Window root = DefaultRootWindow(display);
            XSendEvent(display, root, False,
                       SubstructureNotifyMask | SubstructureRedirectMask, &event);

            // Flush the output buffer to make sure it sends immediately
            XFlush(display);


    }

#ifndef GLX_CONTEXT_MAJOR_VERSION_ARB
#define GLX_CONTEXT_MAJOR_VERSION_ARB           0x2091
#define GLX_CONTEXT_MINOR_VERSION_ARB           0x2092
#define GLX_CONTEXT_FLAGS_ARB                   0x2094
#define GLX_CONTEXT_PROFILE_MASK_ARB            0x9126
#define GLX_CONTEXT_CORE_PROFILE_BIT_ARB        0x00000001
#define GLX_CONTEXT_COMPATIBILITY_PROFILE_BIT_ARB 0x00000002
#endif

#ifndef GLX_CONTEXT_DEBUG_BIT_ARB
#define GLX_CONTEXT_DEBUG_BIT_ARB               0x00000001
#endif
#ifndef GLX_CONTEXT_FORWARD_COMPATIBLE_BIT_ARB
#define GLX_CONTEXT_FORWARD_COMPATIBLE_BIT_ARB  0x00000002
#endif

    typedef GLXContext (*glXCreateContextAttribsARBProc)(Display*, GLXFBConfig, GLXContext, Bool, const int*);

    bool CrystalWindow_X11::GLInitAdvanced(const GLOptions& options) {
        std::cerr << mod_header() << "X11 GLInitAdvanced started" << std::endl;
        std::cerr << mod_header() << "Requested OpenGL version: " << options.major << "." << options.minor << std::endl;
        if (options.stereo) std::cerr << mod_header() << "Requested Stereo: YES" << std::endl;

        // Log GLX version
        int glx_major, glx_minor;
        if (glXQueryVersion(display, &glx_major, &glx_minor)) {
            std::cerr << mod_header() << "GLX version: " << glx_major << "." << glx_minor << std::endl;
        } else {
            std::cerr << mod_header() << "Warning: Could not query GLX version" << std::endl;
        }

        if (!gl_fb_config || !gl_visual_info) {
            std::cerr << mod_header() << "Error: No GLX FBConfig or VisualInfo stored on window. OpenGL initialization failed." << std::endl;
            return false;
        }

        // Try to find a better FBConfig that matches the requested options but is compatible with the current visual
        int fb_attrs[] = {
            GLX_X_RENDERABLE, True,
            GLX_DRAWABLE_TYPE, GLX_WINDOW_BIT,
            GLX_RENDER_TYPE, GLX_RGBA_BIT,
            GLX_X_VISUAL_TYPE, GLX_TRUE_COLOR,
            GLX_DOUBLEBUFFER, options.doubleBuffer ? True : False,
            GLX_STEREO, options.stereo ? True : False,
            GLX_RED_SIZE, 8,
            GLX_GREEN_SIZE, 8,
            GLX_BLUE_SIZE, 8,
            GLX_ALPHA_SIZE, options.alphaBits,
            GLX_DEPTH_SIZE, options.depthBits,
            GLX_STENCIL_SIZE, options.stencilBits,
            None
        };

        int fb_count = 0;
        GLXFBConfig* fbc = glXChooseFBConfig(display, gl_visual_info->screen, fb_attrs, &fb_count);
        if (fbc && fb_count > 0) {
            bool found = false;
            for (int i = 0; i < fb_count; i++) {
                XVisualInfo* vi = glXGetVisualFromFBConfig(display, fbc[i]);
                if (vi) {
                    if (vi->visualid == gl_visual_info->visualid) {
                        gl_fb_config = fbc[i];
                        found = true;
                        XFree(vi);
                        break;
                    }
                    XFree(vi);
                }
            }
            if (found) {
                std::cerr << mod_header() << "Found compatible FBConfig matching requested options." << std::endl;
            } else {
                std::cerr << mod_header() << "Warning: Could not find FBConfig matching requested options that is compatible with current window visual." << std::endl;
            }
            XFree(fbc);
        } else {
            std::cerr << mod_header() << "Warning: glXChooseFBConfig found no configs matching requested options." << std::endl;
        }

        if (gl_context) {
            GLMakeCurrent();
            int actual_major = 0;
            int actual_minor = 0;
            GLGetVersion(actual_major, actual_minor);
            bool satisfies_request =
                actual_major > options.major ||
                (actual_major == options.major && actual_minor >= options.minor);
            std::cerr << mod_header() << " OpenGL context already exists: " << actual_major << "." << actual_minor << std::endl;
            return options.strict ? satisfies_request : true;
        }

        glXCreateContextAttribsARBProc glXCreateContextAttribsARB = (glXCreateContextAttribsARBProc)glXGetProcAddressARB((const GLubyte*)"glXCreateContextAttribsARB");

        if (glXCreateContextAttribsARB) {
            std::vector<int> context_attribs;
            context_attribs.push_back(GLX_CONTEXT_MAJOR_VERSION_ARB);
            context_attribs.push_back(options.major);
            context_attribs.push_back(GLX_CONTEXT_MINOR_VERSION_ARB);
            context_attribs.push_back(options.minor);

            int profile_mask = 0;
            if (options.profile == GLProfile::Core) profile_mask = GLX_CONTEXT_CORE_PROFILE_BIT_ARB;
            else if (options.profile == GLProfile::Compatibility) profile_mask = GLX_CONTEXT_COMPATIBILITY_PROFILE_BIT_ARB;

            if (profile_mask != 0) {
                context_attribs.push_back(GLX_CONTEXT_PROFILE_MASK_ARB);
                context_attribs.push_back(profile_mask);
            }

            int flags = 0;
            if (options.debug) flags |= GLX_CONTEXT_DEBUG_BIT_ARB;
            if (options.forwardCompatible) flags |= GLX_CONTEXT_FORWARD_COMPATIBLE_BIT_ARB;

            if (flags != 0) {
                context_attribs.push_back(GLX_CONTEXT_FLAGS_ARB);
                context_attribs.push_back(flags);
            }

            context_attribs.push_back(None);

            std::cerr << mod_header() << "Attempting to create OpenGL context via glXCreateContextAttribsARB..." << std::endl;
            gl_context = glXCreateContextAttribsARB(display, gl_fb_config, nullptr, True, context_attribs.data());

            if (!gl_context) {
                std::cerr << mod_header() << "Failed to create requested OpenGL context. Trying fallback..." << std::endl;
            }
        } else {
            std::cerr << mod_header() << "glXCreateContextAttribsARB not available. Falling back to legacy methods." << std::endl;
        }

        if (!gl_context) {
            std::cerr << mod_header() << "Attempting fallback: glXCreateNewContext..." << std::endl;
            gl_context = glXCreateNewContext(display, gl_fb_config, GLX_RGBA_TYPE, nullptr, True);
        }

        if (!gl_context && gl_visual_info) {
            std::cerr << mod_header() << "Attempting fallback: legacy glXCreateContext..." << std::endl;
            gl_context = glXCreateContext(display, gl_visual_info, nullptr, True);
        }

        if (!gl_context) {
            std::cerr << mod_header() << "CRITICAL: Failed to create any GLX context" << std::endl;
            return false;
        }

        if (!glXMakeCurrent(display, window, gl_context)) {
            std::cerr << mod_header() << "Could not make GL context current" << std::endl;
            glXMakeCurrent(display, None, nullptr);
            glXDestroyContext(display, gl_context);
            gl_context = nullptr;

            return false;
        }

        // Log GL info
        const char* gl_version = (const char*)glGetString(GL_VERSION);
        const char* gl_vendor = (const char*)glGetString(GL_VENDOR);
        const char* gl_renderer = (const char*)glGetString(GL_RENDERER);

        std::cerr << mod_header() << "Final OpenGL Version: " << (gl_version ? gl_version : "NULL") << std::endl;
        std::cerr << mod_header() << "Final OpenGL Vendor: " << (gl_vendor ? gl_vendor : "NULL") << std::endl;
        std::cerr << mod_header() << "Final OpenGL Renderer: " << (gl_renderer ? gl_renderer : "NULL") << std::endl;

        int actual_major = 0, actual_minor = 0;
        GLGetVersion(actual_major, actual_minor);
        if (options.strict && (actual_major < options.major || (actual_major == options.major && actual_minor < options.minor))) {
            std::cerr << mod_header() << "Error: Created context version (" << actual_major << "." << actual_minor 
                      << ") is less than requested (" << options.major << "." << options.minor << ") and strict mode is enabled." << std::endl;
            glXMakeCurrent(display, None, nullptr);
            glXDestroyContext(display, gl_context);
            gl_context = nullptr;

            return false;
        }

        std::cerr << mod_header() << "OpenGL context initialized successfully" << std::endl;
        return true;
    }

    void CrystalWindow_X11::GLGetVersion(int32_t& major, int32_t& minor) {
        major = 0;
        minor = 0;
        const char* gl_version = (const char*)glGetString(GL_VERSION);
        if (gl_version) {
            sscanf(gl_version, "%d.%d", &major, &minor);
        }
    }

    void CrystalWindow_X11::GLMakeCurrent() {
        if (!gl_context) return;
        if (!glXMakeCurrent(display, window, gl_context)) {
            std::cerr << mod_header() << "Could not make GL context current" << std::endl;
        }
    }

    void CrystalWindow_X11::GLPresent() {
        glXSwapBuffers(display, window);
    }

    void* CrystalWindow_X11::GLGetProcAddress(const char* name) {
        return (void*)glXGetProcAddress((const GLubyte*)name);
    }

    bool CrystalWindow_X11::CoordsToRoot(int32_t &x, int32_t &y) {
        Window root = DefaultRootWindow(display);
        Window child_return;

        int32_t tmp_x,tmp_y;

        if (!XTranslateCoordinates(display, window, root, x, y, &tmp_x, &tmp_y, &child_return)) {
            return false;
        }

        x = tmp_x;
        y = tmp_y;

        return true;
    }

    bool CrystalWindow_X11::CoordsFromRoot(int32_t &x, int32_t &y) {
        Window root = DefaultRootWindow(display);
        Window child_return;

        int32_t tmp_x,tmp_y;

        if (!XTranslateCoordinates(display, root, window, x, y, &tmp_x, &tmp_y, &child_return)) {
            return false;
        }

        x = tmp_x;
        y = tmp_y;

        return true;
    }

    void request_selection(Display* dpy, CrystalWindow_X11 *win, Atom selection, Atom target)
    {

        Atom property = NewAge::AppX11->atoms.selection_data; // fallback uses target as property

        // Always use our dedicated receiving property rather than target or None
        //Atom property = NewAge::AppX11->atoms.selection_data; // e.g., "CRYSTAL_SELECTION"
        XConvertSelection(dpy, selection, target, property, win->window, win->last_user_time);
        XFlush(dpy);
    }
}

