// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#ifndef CRYSTALCATALYST_LINUX_CRYSTALWINDOW_H
#define CRYSTALCATALYST_LINUX_CRYSTALWINDOW_H

#include <GL/glx.h>

#include "CrystalCatalystLibrary/CrystalCatalystLibrary.h"

using namespace JWCEssentials;

namespace NewAge {
    class DragProvide_X11;

    class CrystalWindow_X11 : public CrystalWindow {
    public:
        Window window;
        P_INSTANCE(Display)  display;
        GLXContext gl_context;
        DragProvide_X11* drag_provide;

        bool draw_queued=true;

        ~CrystalWindow_X11() override;

        void PresentImage(utf8_string_struct pixformat, P_ELEMENTS(void)  pixdata, size_t pixdata_length, int32_t width, int32_t height) override;
        void QueueRedraw() override;

        void MouseCapture() override;
        void MouseRelease() override;
        void GLInit() override;
        void Show(bool restore) override;
        void Close() override;
        void PostClose() override;
        void RegisterDragTarget() override;
        void DragStart(P_INSTANCE(DragDropData) data, int32_t x, int32_t y) override;

        /* INTERNAL */
        virtual bool handle_xevent(P_INSTANCE(XEvent)  event);
        virtual bool handle_drop_xevents(P_INSTANCE(XEvent)  event);

        virtual bool handle_client_message(P_INSTANCE(XEvent)  event);
        virtual bool handle_xdnd_enter(P_INSTANCE(XEvent)  event);
        virtual bool handle_xdnd_position(P_INSTANCE(XEvent)  event);
        virtual bool handle_xdnd_leave(P_INSTANCE(XEvent)  event);
        virtual bool handle_xdnd_drop(P_INSTANCE(XEvent)  event);

        virtual bool handle_selection_notify(P_INSTANCE(XEvent)  event);
        virtual bool handle_property_notify(P_INSTANCE(XEvent) event);
        virtual bool handle_selection_request(P_INSTANCE(XEvent) event);

        bool CoordsToRoot(int32_t &x, int32_t &y);
        bool CoordsFromRoot(int32_t &x, int32_t &y);
    };

    void DataImterchange_FormatsFromAtomArray(P_INSTANCE(DataInterchange) dataInterchange, P_ELEMENTS(Atom) types, int num_types);
    void DataImterchange_AtomArrayFromFormats(P_INSTANCE(DataInterchange) dataInterchange, P_INSTANCE(P_ELEMENTS(Atom)) types, P_INSTANCE(int) num_types);
}
#endif //CRYSTALCATALYST_CRYSTALWINDOW_H
