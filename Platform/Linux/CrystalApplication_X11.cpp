// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include "CrystalApplication_X11.h"

#include <iostream>
#include <unistd.h>



int32_t handleXError(P_INSTANCE(Display) display, P_INSTANCE(XErrorEvent) error)
{
    char errorText[256];
    XGetErrorText(display, error->error_code, errorText, sizeof errorText);
    fprintf(stderr, "Received X error: %s\n", errorText);
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

P_INSTANCE(WindowHandle) CrystalApplication_X11::WindowCreate(int32_t width, int32_t height, utf8_string_const title) {
    if (!globalDisplay) {
        std::cerr << "Global display is not initialized" << std::endl;
        return nullptr;
    }

    int32_t screen = DefaultScreen(globalDisplay);
    Window root = RootWindow(globalDisplay, screen);
    XSetWindowAttributes swa;
    swa.event_mask = ExposureMask | KeyPressMask | KeyReleaseMask | ButtonPressMask | ButtonReleaseMask | PointerMotionMask | StructureNotifyMask;
    Window win = XCreateWindow(globalDisplay, root, 0, 0, width, height, 0, CopyFromParent, InputOutput, CopyFromParent, CWEventMask, &swa);

    if (!win) {
        std::cerr << "Failed to create window" << std::endl;
        return nullptr;
    }

    XStoreName(globalDisplay, win, title);
    XMapWindow(globalDisplay, win);

    XSelectInput(globalDisplay, win, ExposureMask | KeyPressMask | KeyReleaseMask | ButtonPressMask | ButtonReleaseMask | PointerMotionMask | StructureNotifyMask);
    XFlush(globalDisplay);  // Ensure commands are sent to the X server

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
        free(window_structure);
        return nullptr;
    }
    window_handle->crystal_window = window_structure;
    window_handle->crystal_window->myHandle = window_handle;

    XSaveContext(globalDisplay, win, windowContext, (XPointer)window_handle);

    Application_WindowAdd(window_handle);
    std::cout << "Window created successfully" << std::endl;
    return window_handle;
}

void CrystalApplication_X11::DispatchEvent(XEvent &event) {
    P_INSTANCE(WindowHandle) window_handle;
    XFindContext(event.xany.display, event.xany.window, windowContext, (P_INSTANCE(XPointer)) & window_handle);

    P_INSTANCE(CrystalWindow_X11)win = (P_INSTANCE(CrystalWindow_X11)) window_handle->crystal_window;

    if (window_handle) {
        win->handle_xevent(&event);
    } else {
        std::cerr << "No window handle found for event" << std::endl;
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
