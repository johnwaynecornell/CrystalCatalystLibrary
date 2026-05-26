// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include "../CrystalApplication_X11.h"
#include "CrystalWindow_X11.h"
#include "DragProvide_X11.h"
#include <unistd.h>
#include <iostream>

#include "DragDrop_X11.h"

#ifdef mod_header
#undef mod_header
#endif

#define mod_header() "CrystalWindow_X11:"

#include <iomanip>
#include <X11/XKBlib.h>
#include <X11/keysym.h>

using namespace JWCEssentials;

namespace NewAge {
    CrystalWindow_X11::~CrystalWindow_X11() {
        if (drag_provide) delete drag_provide;
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
        /* TODO */ //Check error condition

        XImage* ximage;

        P_ELEMENTS(void) m = proxy ? proxy.pix_data : pixdata;
        ximage = XCreateImage(display, CopyFromParent, 24, ZPixmap, 0, (char *) m, width, height, 32, 0);

        if (!ximage) {
            std::cerr << mod_header() << "Failed to create XImage" << std::endl;
            return;
        }

        GC gc = XCreateGC(display, window, 0, nullptr);
        if (!gc) {
            std::cerr << mod_header() << "Failed to create Graphics Context" << std::endl;
            if (proxy) proxy.pix_data_free(proxy.pix_data);

            ximage->data = nullptr;
            XDestroyImage(ximage);
            return;
        }

        XPutImage(display, window, gc, ximage, 0, 0, 0, 0, width, height);
        XFreeGC(display, gc);

        if (proxy) proxy.pix_data_free(proxy.pix_data);

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

#include <GL/gl.h>
#include <GL/glx.h>

    void CrystalWindow_X11::GLInit() {
        // Define the GLX attributes for the visual
        int32_t attributes[] = {
            GLX_RGBA, GLX_DEPTH_SIZE, 24, GLX_DOUBLEBUFFER, None
        };

        // Get a matching framebuffer configuration
        XVisualInfo* visual_info = glXChooseVisual(display, 0, attributes);
        if (visual_info == nullptr) {
            std::cerr << mod_header() << "No appropriate visual found" << std::endl;
            return;
        }

        // Create a GLX context
        gl_context = glXCreateContext(display, visual_info, nullptr, GL_TRUE);
        if (gl_context == nullptr) {
            std::cerr << mod_header() << "Failed to create a GLX context" << std::endl;
            XFree(visual_info);
            return;
        }

        // Make the context current
        if (!glXMakeCurrent(display, window, gl_context)) {
            std::cerr << mod_header() << "Could not make GL context current" << std::endl;
            glXDestroyContext(display, gl_context);
            XFree(visual_info);
            return;
        }

        // Free the visual info
        XFree(visual_info);

        std::cerr << mod_header() << "OpenGL context initialized" << std::endl;
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

