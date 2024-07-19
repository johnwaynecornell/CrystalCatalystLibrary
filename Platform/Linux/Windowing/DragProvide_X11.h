// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#ifndef DRAGPROVIDE_X11_H
#define DRAGPROVIDE_X11_H

#include <X11/Xlib.h>
#include <X11/Xatom.h>
#include <X11/Xutil.h>
#include <iostream>

class CrystalWindow_X11;
class DragDropData;

class DragProvide_X11 {
public:

    DragProvide_X11(P_INSTANCE(CrystalWindow_X11)source_window);
    ~DragProvide_X11();

    void StartDrag(P_INSTANCE(DragDropData)  data, int32_t x, int32_t y);
    void UpdateMotion(int32_t x, int32_t y);
    void EndDrag(bool success);

    P_INSTANCE(CrystalWindow_X11)source_window;
    Window target_window;
    P_INSTANCE(DragDropData)  drag_data;
    bool dragging;
    bool entered;

    bool handle_message(P_INSTANCE(XEvent) event);
protected:
    void drag_is_finished(bool success);

    bool wait_for_status = false;

    void clear_target();

    void send_xdnd_enter();
    void send_xdnd_position(int32_t x, int32_t y);
    void send_xdnd_leave();
    void send_xdnd_drop();
    void send_xdnd_finished(bool success);

    bool handle_selection_request(P_INSTANCE(XEvent) event);
    bool handle_xdnd_status(P_INSTANCE(XEvent)  event);
    bool handle_xdnd_finished(P_INSTANCE(XEvent)  event);

    Window get_window_under_cursor(int32_t &x, int32_t &y);

};

#endif //DRAGPROVIDE_X11_H
