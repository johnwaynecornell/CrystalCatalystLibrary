// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#ifndef CRYSTALCATALYST_LINUX_CRYSTALWINDOW_H
#define CRYSTALCATALYST_LINUX_CRYSTALWINDOW_H

#include "../../../CrystalCatalystLibrary.h"
#include "../Platform.h"
#include "DragProvide_X11.h"

class CrystalWindow_X11 : public CrystalWindow {
public:
    Window window;
    P_INSTANCE(Display)  display;
    GLXContext gl_context;
    DragProvide_X11* drag_provide;

    bool draw_queued=true;

    virtual ~CrystalWindow_X11();

    virtual void PresentImage(utf8_string_const pixformat, P_ELEMENTS(void)  pixdata, size_t pixdata_length, int32_t width, int32_t height);
    virtual void QueueRedraw();

    virtual void MouseCapture();
    virtual void MouseRelease();
    virtual void GL_Init();
    virtual void Show(bool restore);

    virtual void RegisterDragTarget();
    virtual void DragStart(P_INSTANCE(DragDropData) data, int32_t x, int32_t y);

    /* INTERNAL */
    virtual bool handle_xevent(P_INSTANCE(XEvent)  event);
    virtual bool handle_drop_xevents(P_INSTANCE(XEvent)  event);

    virtual bool handle_client_message(P_INSTANCE(XEvent)  event);
    virtual bool handle_xdnd_enter(P_INSTANCE(XEvent)  event);
    virtual bool handle_xdnd_position(P_INSTANCE(XEvent)  event);
    virtual bool handle_xdnd_leave(P_INSTANCE(XEvent)  event);
    virtual bool handle_xdnd_drop(P_INSTANCE(XEvent)  event);

    virtual bool handle_selection_notify(P_INSTANCE(XEvent)  event);

    bool CoordsToRoot(int32_t &x, int32_t &y);
    bool CoordsFromRoot(int32_t &x, int32_t &y);
};



#endif //CRYSTALCATALYST_CRYSTALWINDOW_H
