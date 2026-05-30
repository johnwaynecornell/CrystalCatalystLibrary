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
        CrystalWindow_X11();
        Window window = None;
        P_INSTANCE(Display)  display = nullptr;
        GLXContext gl_context = nullptr;
        GLXFBConfig gl_fb_config = nullptr;
        XVisualInfo* gl_visual_info = nullptr;
        Colormap gl_colormap = None;
        DragProvide_X11* drag_provide = nullptr;
        // In CrystalWindow_X11 members, add:
        Atom target_atom = None;     // the requested "format" (e.g., UTF8_STRING/text/html)
        Atom property_atom = None;   // the property we expect data on (e.g., CRYSTAL_SELECTION)
        Atom expected_selection = None;
        bool clipboard_pending = false;
        bool retry_with_property = false;   // add this bool in your window state
        bool tried_primary = false;         // add this too

        Time last_user_time = CurrentTime;

        bool draw_queued=true;

        ~CrystalWindow_X11() override;
        Time get_user_time(XEvent* ev);

        void PresentImage(utf8_string_struct pixformat, P_ELEMENTS(void)  pixdata, size_t pixdata_length, int32_t width, int32_t height) override;
        void QueueRedraw() override;

        void MouseCapture() override;
        void MouseRelease() override;
        void GLInit() override;
        void GLMakeCurrent() override;
        void GLPresent() override;
        void* GLGetProcAddress(const char* name) override;
        void Show(bool restore) override;
        void Close() override;
        void PostClose() override;

        void SetSize(int32_t width, int32_t height) override;
        void GetSize(int32_t& width, int32_t& height) override;
        void SetLocation(int32_t x, int32_t y) override;
        void GetLocation(int32_t& x, int32_t& y) override;

        void SetCursor(utf8_string_struct pixformat, P_ELEMENTS(void) pixdata, size_t pixdata_length, int32_t width, int32_t height, int32_t hot_x, int32_t hot_y) override;
        void SetStandardCursor(CrystalCursor cursor_enum) override;
        void SetIcon(utf8_string_struct pixformat, P_ELEMENTS(void) pixdata, size_t pixdata_length, int32_t width, int32_t height) override;
        void SetTitle(utf8_string_struct title) override;
        void GetTitle(P_OUT(utf8_string_struct) title) override;

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

    // Issue XConvertSelection with either property=None (first try) or property=target (retry).
    void request_selection(Display* dpy, P_INSTANCE(CrystalWindow_X11) win, Atom selection, Atom target);

}
#endif //CRYSTALCATALYST_CRYSTALWINDOW_H
