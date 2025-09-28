// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#ifndef CRYSTALCATALYST_CRYSTALWINDOW_H
#define CRYSTALCATALYST_CRYSTALWINDOW_H

#include <chrono>

#include "DragDrop.h"
#include "Clipboard.h"
#include "Pixels.h"

using namespace JWCEssentials;

namespace NewAge {
    //##### DUMP REGION ##### callbacks
    typedef struct {
        void (*on_draw)(P_INSTANCE(WindowHandle) window_handle);

        void (*on_key_down)(P_INSTANCE(WindowHandle) window_handle, int32_t keycode);
        void (*on_key_up)(P_INSTANCE(WindowHandle) window_handle, int32_t keycode);

        void (*on_mouse_move)(P_INSTANCE(WindowHandle) window_handle, int32_t x, int32_t y);
        void (*on_mouse_down)(P_INSTANCE(WindowHandle) window_handle, int32_t button, int32_t x, int32_t y);
        void (*on_mouse_up)(P_INSTANCE(WindowHandle) window_handle, int32_t button, int32_t x, int32_t y);

        void (*on_resize)(P_INSTANCE(WindowHandle) window_handle, int32_t width, int32_t height);

        void (*on_close)(P_INSTANCE(WindowHandle) window_handle);

        void (*on_focus_in)(P_INSTANCE(WindowHandle) window_handle);
        void (*on_focus_out)(P_INSTANCE(WindowHandle) window_handle);

        void (*on_drag_receive_start)(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DragDropData)  data);

        void (*on_drag_receive_enter)(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DragDropData)  data);
        void (*on_drag_receive_motion)(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DragDropData)  data, int32_t x, int32_t y, uint32_t key_state);
        void (*on_drag_receive_leave)(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DragDropData)  data);
        void (*on_drag_receive_drop)(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DragDropData)  data);
        utf8_string_struct (*on_drag_receive_select)(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DragDropData)  data);

        void (*on_drag_provide_status)(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DragDropData)  data);
        void (*on_drag_provide_chosen)(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DragDropData)  data, utf8_string_struct format);
        void (*on_drag_provide_finished)(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DragDropData) data, bool success);

        void (*on_clipboard_provide_chosen)(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DataInterchange)  data, utf8_string_struct format);
        void (*on_clipboard_receive_data)(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DataInterchange)  data);

        void (*on_data_interchange_error)(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DataInterchange)  data, utf8_string_struct error);

        void (*on_idle)(P_INSTANCE(WindowHandle) window_handle);
    } WindowCallbacks;


    //##### end DUMP REGION ##### callbacks

    _EXPORT_ P_INSTANCE(WindowHandle) CrystalWindow_Create(int32_t width, int32_t height, utf8_string_struct title);
    _EXPORT_ P_INSTANCE(WindowHandle) CrystalWindow_CreateSimple(int32_t width, int32_t height, utf8_string_struct title);
    _EXPORT_ void CrystalWindow_ApplicationRetain(P_INSTANCE(WindowHandle) window_handle);
    _EXPORT_ void CrystalWindow_ApplicationRelease(P_INSTANCE(WindowHandle) window_handle);

    _EXPORT_ void CrystalWindow_PresentImage(P_INSTANCE(WindowHandle) window_handle, utf8_string_struct pixformat, P_ELEMENTS(void)  pixdata, size_t pixdata_length, int32_t width, int32_t height);
    _EXPORT_ void CrystalWindow_QueueRedraw(P_INSTANCE(WindowHandle) window_handle);

    _EXPORT_ void CrystalWindow_MouseCapture(P_INSTANCE(WindowHandle) window_handle);
    _EXPORT_ void CrystalWindow_MouseRelease(P_INSTANCE(WindowHandle) window_handle);
    _EXPORT_ void CrystalWindow_GLInit(P_INSTANCE(WindowHandle) window_handle);
    _EXPORT_ void CrystalWindow_Show(P_INSTANCE(WindowHandle) window_handle, bool restore);
    _EXPORT_ void CrystalWindow_Close(P_INSTANCE(WindowHandle) window_handle);
    _EXPORT_ void CrystalWindow_PostClose(P_INSTANCE(WindowHandle) window_handle);

    _EXPORT_ bool CrystalWindow_SetMessaqeHandler(P_INSTANCE(WindowHandle) window_handle, utf8_string_struct handler_name, P_INSTANCE(void) handler);

    _EXPORT_ double CrystalWindow_uptimeSeconds(P_INSTANCE(WindowHandle) window_handle);
    _EXPORT_ void   CrystalWindow_uptimeReset(P_INSTANCE(WindowHandle) window_handle);




    class CrystalWindow {
    public:
        virtual ~CrystalWindow() = default;

        WindowCallbacks callbacks;
        P_INSTANCE(WindowHandle) myHandle;

        P_INSTANCE(DataInterchange) current_clipboard_provide_data;


        P_INSTANCE(DataInterchange) current_clipboard_receive_data;
        P_INSTANCE(DragDropData)  current_drag_receive_data;

        int width = 0;
        int height = 0;

        bool ready = false;
        bool received_first_message = false;

    private:
        struct MonotonicTimer {
            std::chrono::steady_clock::time_point start{std::chrono::steady_clock::now()};
            void   reset()   { start = std::chrono::steady_clock::now(); }
            double elapsed() const {
                using dsec = std::chrono::duration<double>;
                return std::chrono::duration_cast<dsec>(std::chrono::steady_clock::now() - start).count();
            }
        };
        MonotonicTimer uptime_;
    public:

        double uptime_seconds() const { return uptime_.elapsed(); }
        void   reset_uptime()         { uptime_.reset(); }

        virtual void PresentImage(utf8_string_struct pixformat, P_ELEMENTS(void)  pixdata, size_t pixdata_length, int32_t width, int32_t height) =0;
        virtual void QueueRedraw() = 0;

        virtual void MouseCapture() = 0;
        virtual void MouseRelease() = 0;
        virtual void GLInit() = 0;
        virtual void Show(bool restore) = 0;
        virtual void Close() = 0;
        virtual void PostClose() = 0;


        virtual void RegisterDragTarget() = 0;
        virtual void DragStart(P_INSTANCE(DragDropData) data, int32_t x, int32_t y) = 0;

        virtual void ApplicationRetain();
        virtual void ApplicationRelease();
    };

    typedef struct WindowHandle
    {
        P_INSTANCE(CrystalWindow) crystal_window;
    } WindowHandle;

    typedef struct HandleNode
    {
        P_INSTANCE(WindowHandle) handle;
        P_INSTANCE(HandleNode) next;
    } HandleNode;

    extern HandleNode window_head;
}

#endif //CRYSTALCATALYST_CRYSTALWINDOW_H
