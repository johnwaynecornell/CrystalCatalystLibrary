// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include "CrystalWindow_X11.h"
#include <unistd.h>
#include <iostream>

#include "DragDrop_X11.h"

#ifdef mod_header
#undef mod_header
#endif

#define mod_header() "CrystalWindow_X11:"

CrystalWindow_X11::~CrystalWindow_X11() {
    if (drag_provide) delete drag_provide;
}

void CrystalWindow_X11::Show(bool restore) {
    XRaiseWindow(display, window);
}

#include "CrystalWindow_X11.h"
#include <X11/XKBlib.h>
#include <X11/keysym.h>

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


bool CrystalWindow_X11::handle_xevent(P_INSTANCE(XEvent)  event) {
    if (drag_provide && drag_provide->dragging && drag_provide->handle_message(event)) return true;
    if (handle_drop_xevents(event)) return true;

    int32_t unicodeChar;
    switch (event->type) {
        case Expose:
            if (callbacks.on_draw) {
                callbacks.on_draw(myHandle);
            }
            draw_queued = false;
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
    }

    return false;
}

void CrystalWindow_X11::PresentImage(utf8_string_const pixformat, P_ELEMENTS(void)  pixdata, size_t pixdata_length, int32_t width, int32_t height)
{
    if (!pixdata || !pixformat) return;

    ReturnBuffer proxy= Pixels_ConvertPixels(pixformat, "bgra:int8", pixdata, pixdata_length, width, height);
    /* TODO */ //Check error condition


    XImage* ximage;

    P_ELEMENTS(void) m = proxy ? proxy.memory : pixdata;
    ximage = XCreateImage(display, CopyFromParent, 24, ZPixmap, 0, (char *) m, width, height, 32, 0);

    if (!ximage) {
        std::cerr << mod_header() << "Failed to create XImage" << std::endl;
        return;
    }

    GC gc = XCreateGC(display, window, 0, nullptr);
    if (!gc) {
        std::cerr << mod_header() << "Failed to create Graphics Context" << std::endl;
        if (proxy) ReturnBuffer_Deallocate(proxy);

        ximage->data = nullptr;
        XDestroyImage(ximage);
        return;
    }

    XPutImage(display, window, gc, ximage, 0, 0, 0, 0, width, height);
    XFreeGC(display, gc);

    if (proxy) ReturnBuffer_Deallocate(proxy);

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
        GrabModeAsync, GrabModeAsync, None, None, CurrentTime
    );

    if (result != GrabSuccess) {
        std::cerr << mod_header() << "Failed to capture mouse pointer" << std::endl;
    } else {
        std::cerr << mod_header() << "Mouse pointer captured" << std::endl;
    }
}

void CrystalWindow_X11::MouseRelease() {
    // Release the pointer
    XUngrabPointer(display, CurrentTime);
    std::cerr << mod_header() << "Mouse pointer released" << std::endl;
}

#include <GL/gl.h>
#include <GL/glx.h>

void CrystalWindow_X11::GL_Init() {
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