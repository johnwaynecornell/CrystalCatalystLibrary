// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include "CrystalApplication_X11.h"

#include <iomanip>
#include <iostream>
#include <unistd.h>
#include <X11/Xatom.h>

using namespace JWCEssentials;

namespace NewAge {
    int32_t handleXError(P_INSTANCE(Display) display, P_INSTANCE(XErrorEvent) error)
    {
        char errorText[256];
        XGetErrorText(display, error->error_code, errorText, sizeof errorText);

        fprintf(stderr, "XError: %s (req=%u, res=0x%lx, minor=%u)\n",
                errorText, error->request_code, error->resourceid, error->minor_code);

        return 0; // Return 0 to continue execution, non-zero will cause the program to exit
    }

    int32_t handleXIOError(P_INSTANCE(Display) display)
    {
        fprintf(stderr, "Received fatal I/O error on X server\n");
        return 1; // You may want to exit the program here as the connection to the X server is lost
    }

    CrystalApplication_X11 *AppX11 = nullptr;

    void CrystalApplication_X11::Init()
    {
        XInitThreads();

        CrystalApplication::Init();

        windowContext = XUniqueContext();

        // Initialize global display connection
        globalDisplay = XOpenDisplay(nullptr);
        if (!globalDisplay) {
            std::cerr << "Unable to open X display in platform_init" << std::endl;
            exit(1);
        }

        InitAtoms();

        // Set up error handlers
        XSetErrorHandler(handleXError);
        XSetIOErrorHandler(handleXIOError);

        AppX11 = this;
    }

    P_INSTANCE(WindowHandle) CrystalApplication_X11::WindowCreate(int32_t width, int32_t height, utf8_string_struct title) {
        if (!globalDisplay) {
            std::cerr << "Global display is not initialized" << std::endl;
            return nullptr;
        }

        int32_t screen = DefaultScreen(globalDisplay);
        Window root = RootWindow(globalDisplay, screen);

        GLXFBConfig fb_config = nullptr;
        XVisualInfo* visual_info = nullptr;
        Colormap colormap = None;

        int fb_attrs[] = {
            GLX_X_RENDERABLE, True,
            GLX_DRAWABLE_TYPE, GLX_WINDOW_BIT,
            GLX_RENDER_TYPE, GLX_RGBA_BIT,
            GLX_X_VISUAL_TYPE, GLX_TRUE_COLOR,
            GLX_DOUBLEBUFFER, True,
            GLX_RED_SIZE, 8,
            GLX_GREEN_SIZE, 8,
            GLX_BLUE_SIZE, 8,
            GLX_ALPHA_SIZE, 8,
            GLX_DEPTH_SIZE, 24,
            GLX_STENCIL_SIZE, 8,
            None
        };

        int fb_count = 0;
        GLXFBConfig* fbc = glXChooseFBConfig(globalDisplay, screen, fb_attrs, &fb_count);
        if (fbc && fb_count > 0) {
            fb_config = fbc[0];
            XFree(fbc);
            visual_info = glXGetVisualFromFBConfig(globalDisplay, fb_config);
        } else {
            std::cerr << "CrystalApplication_X11: Failed to find a suitable GLX FBConfig. Falling back to default visual." << std::endl;
        }

        XSetWindowAttributes swa;
        swa.event_mask = ExposureMask | KeyPressMask | KeyReleaseMask | ButtonPressMask | ButtonReleaseMask | PointerMotionMask | StructureNotifyMask | EnterWindowMask | LeaveWindowMask;
        
        Window win;
        if (visual_info) {
            colormap = XCreateColormap(globalDisplay, root, visual_info->visual, AllocNone);
            swa.colormap = colormap;
            swa.background_pixmap = None;
            swa.border_pixel = 0;
            win = XCreateWindow(globalDisplay, root, 0, 0, (unsigned int)width, (unsigned int)height, 0,
                                visual_info->depth, InputOutput, visual_info->visual,
                                CWEventMask | CWColormap | CWBackPixmap | CWBorderPixel, &swa);
        } else {
            win = XCreateWindow(globalDisplay, root, 0, 0, (unsigned int)width, (unsigned int)height, 0,
                                CopyFromParent, InputOutput, CopyFromParent,
                                CWEventMask, &swa);
        }

        if (!win) {
            std::cerr << "Failed to create window" << std::endl;
            if (visual_info) XFree(visual_info);
            return nullptr;
        }

        Atom protos[2] = {
            ((CrystalApplication_X11*)TheApplication)->atoms.window._delete,
            ((CrystalApplication_X11*)TheApplication)->atoms.window.take_focus
        };
        XSetWMProtocols(globalDisplay, win, protos, 2);

        XStoreName(globalDisplay, win, title);
        XMapWindow(globalDisplay, win);

        XSelectInput(globalDisplay, win, ExposureMask | KeyPressMask | KeyReleaseMask | ButtonPressMask | ButtonReleaseMask | PointerMotionMask | StructureNotifyMask | PropertyChangeMask | EnterWindowMask | LeaveWindowMask);
        XFlush(globalDisplay);  // Ensure commands are sent to the X server

        auto* window_structure = new CrystalWindow_X11();
        if (!window_structure) {
            std::cerr << "Failed to allocate memory for window_structure" << std::endl;
            if (visual_info) XFree(visual_info);
            return nullptr;
        }

        window_structure->window = win;
        window_structure->display = globalDisplay;
        window_structure->gl_context = nullptr;
        window_structure->gl_fb_config = fb_config;
        window_structure->gl_visual_info = visual_info;
        window_structure->gl_colormap = colormap;

        auto* window_handle = (P_INSTANCE(WindowHandle))malloc(sizeof(WindowHandle));
        if (!window_handle) {
            std::cerr << "Failed to allocate memory for window_handle" << std::endl;
            delete window_structure;
            return nullptr;
        }
        window_handle->crystal_window = window_structure;
        window_handle->crystal_window->myHandle = window_handle;

        XSaveContext(globalDisplay, win, windowContext, (XPointer)window_handle);

        Application_WindowAdd(window_handle);
        XFlush(globalDisplay);

        return window_handle;
    }

    P_INSTANCE(WindowHandle) CrystalApplication_X11::WindowCreate_Simple(int32_t width, int32_t height, utf8_string_struct title) {
        if (!globalDisplay) {
            std::cerr << "Global display is not initialized" << std::endl;
            return nullptr;
        }

        int32_t screen = DefaultScreen(globalDisplay);
        Window root = RootWindow(globalDisplay, screen);
        //XSetWindowAttributes swa;
        //swa.event_mask = ExposureMask | KeyPressMask | KeyReleaseMask | ButtonPressMask | ButtonReleaseMask | PointerMotionMask | StructureNotifyMask;

        Window win = XCreateSimpleWindow(globalDisplay, root,-10000,-10000,width,height,0,0,0);

        if (!win) {
            std::cerr << "Failed to create window" << std::endl;
            return nullptr;
        }

        Atom protos[2] = {
            ((CrystalApplication_X11*)TheApplication)->atoms.window._delete,
            ((CrystalApplication_X11*)TheApplication)->atoms.window.take_focus
        };
        XSetWMProtocols(globalDisplay, win, protos, 2);


        XStoreName(globalDisplay, win, title);
        XMapWindow(globalDisplay, win);

        XSelectInput(globalDisplay, win, ExposureMask | KeyPressMask | KeyReleaseMask | ButtonPressMask | ButtonReleaseMask | PointerMotionMask | StructureNotifyMask | PropertyChangeMask | EnterWindowMask | LeaveWindowMask);
        XFlush(globalDisplay);  // Ensure commands are sent to the X server

        Atom net_wm_state = XInternAtom(globalDisplay, "_NET_WM_STATE", False);
        Atom skip_taskbar = XInternAtom(globalDisplay, "_NET_WM_STATE_SKIP_TASKBAR", False);
        Atom skip_pager   = XInternAtom(globalDisplay, "_NET_WM_STATE_SKIP_PAGER", False);
        XChangeProperty(globalDisplay, win, net_wm_state, XA_ATOM, 32, PropModeReplace,
                        reinterpret_cast<unsigned char const*>(&skip_taskbar), 1);
        XChangeProperty(globalDisplay, win, net_wm_state, XA_ATOM, 32, PropModeAppend,
                        reinterpret_cast<unsigned char const*>(&skip_pager), 1);


        auto* window_structure = new CrystalWindow_X11();
        if (!window_structure) {
            std::cerr << "Failed to allocate memory for window_structure" << std::endl;
            return nullptr;
        }

        window_structure->window = win;
        window_structure->display = globalDisplay;
        window_structure->gl_context = nullptr;

        auto* window_handle = (P_INSTANCE(WindowHandle))malloc(sizeof(WindowHandle));
        if (!window_handle) {
            std::cerr << "Failed to allocate memory for window_handle" << std::endl;
            delete window_structure;
            return nullptr;
        }
        window_handle->crystal_window = window_structure;
        window_handle->crystal_window->myHandle = window_handle;

        XSaveContext(globalDisplay, win, windowContext, (XPointer)window_handle);

        Application_WindowAdd(window_handle);
        XSynchronize(globalDisplay, True);
        XFlush(globalDisplay);  // Ensure commands are sent to the X server
        return window_handle;
    }

    void CrystalApplication_X11::DispatchEvent(XEvent &event) {
        P_INSTANCE(WindowHandle) window_handle = nullptr;
        if (XFindContext(event.xany.display, event.xany.window, windowContext, (P_INSTANCE(XPointer)) & window_handle) != 0) {
            return;
        }

        if (window_handle && window_handle->crystal_window) {
            P_INSTANCE(CrystalWindow_X11)win = (P_INSTANCE(CrystalWindow_X11)) window_handle->crystal_window;
            win->handle_xevent(&event);
        }

    }

    void CrystalApplication_X11::DispatchCycle()
    {
        XEvent event;

        while (XPending(globalDisplay)) {
            XNextEvent(globalDisplay, &event);
            DispatchEvent(event);
        }
    }

    bool CrystalApplication_X11::HasMessage()
    {
        if (!XPending(globalDisplay)) usleep(16000);  // Approximately 60 FPS

        if (XPending(globalDisplay)) return true;  // Approximately 60 FPS
        usleep(16000);
        return false;
    }
}