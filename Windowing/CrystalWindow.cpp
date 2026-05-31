// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include <cstring>
#include "CrystalCatalystLibrary/CrystalCatalystLibrary.h"

#include <assert.h>
#include <iostream>

using namespace JWCEssentials;

namespace NewAge {

    void CrystalWindow_ApplicationRetain(P_INSTANCE(WindowHandle) window_handle)
    {
        window_handle->crystal_window->ApplicationRetain();
    }

    void CrystalWindow_ApplicationRelease(P_INSTANCE(WindowHandle) window_handle)
    {
        window_handle->crystal_window->ApplicationRelease();
    }

    CrystalWindow::CrystalWindow()
    {
        callbacks = {};
        myHandle = nullptr;

        current_clipboard_provide_data = nullptr;
        current_clipboard_receive_data = nullptr;
        current_drag_receive_data = nullptr;
    }

    void CrystalWindow::ApplicationRetain() {
        TheApplication->RetainerIncrement();
    }

    void CrystalWindow::ApplicationRelease() {
        TheApplication->RetainerDecrement();
    }

    P_INSTANCE(WindowCallbacks) get_callbacks(P_INSTANCE(WindowHandle) handle)
    {
        return &handle->crystal_window->callbacks;
    }

    P_INSTANCE(WindowHandle) CrystalWindow_Create(int32_t width, int32_t height, utf8_string_struct title) {
        auto Ret = TheApplication->WindowCreate(width, height, title);
        if (Ret != nullptr)
        {
            CrystalWindow_SetStandardCursor(Ret, CRYSTAL_CURSOR_ARROW);
            // Image: CatalystCrystal.png
            // Width: 51
            // Height: 61
            // Format: RGBA8888
            //unsigned char CrystalCatalystIcon[12444]

            utf8_string_struct pixformat;
            void *pixdata;
            size_t  pixdata_length;
            int width, height;

            CrystalWindow_GetDefaultStockIcon(&pixformat, &pixdata, &pixdata_length, &width, &height);
            CrystalWindow_SetIcon(Ret, pixformat, pixdata, pixdata_length, width, height);

        }
        return Ret;
    }

    P_INSTANCE(WindowHandle) CrystalWindow_CreateSimple(int32_t width, int32_t height, utf8_string_struct title) {
        auto Ret = TheApplication->WindowCreate_Simple(width, height, title);
        if (Ret != nullptr)
        {
            CrystalWindow_SetStandardCursor(Ret, CRYSTAL_CURSOR_ARROW);
            utf8_string_struct pixformat;
            void *pixdata;
            size_t pixdata_length;
            int width, height;

            CrystalWindow_GetDefaultStockIcon(&pixformat, &pixdata, &pixdata_length, &width, &height);
            CrystalWindow_SetIcon(Ret, pixformat, pixdata, pixdata_length, width, height);
        }
        return Ret;
    }

    double CrystalWindow_uptimeSeconds(P_INSTANCE(WindowHandle) window_handle) {
        return window_handle->crystal_window->uptime_seconds();
    }

    void   CrystalWindow_uptimeReset(P_INSTANCE(WindowHandle) window_handle) {
        window_handle->crystal_window->reset_uptime();
    }


    void CrystalWindow_PresentPix(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(PixData) pix) {
        CrystalWindow_PresentImage(window_handle, pix->pix_format.c_str, pix->pix_data, pix->pix_data_length, pix->width, pix->height);
    }

    void CrystalWindow_PresentImage(P_INSTANCE(WindowHandle) window_handle, utf8_string_struct pixformat, P_ELEMENTS(void)  pixdata, size_t pixdata_length, int32_t width, int32_t height) {
        window_handle->crystal_window->PresentImage(pixformat, pixdata, pixdata_length, width, height);
    }

    void CrystalWindow_QueueRedraw(P_INSTANCE(WindowHandle) window_handle) {
        window_handle->crystal_window->QueueRedraw();
    }

    void CrystalWindow_RegisterDragTarget(P_INSTANCE(WindowHandle) window_handle) {
        window_handle->crystal_window->RegisterDragTarget();
    }

    void CrystalWindow_DragStart(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(DragDropData)  data, int32_t x, int32_t y) {
        window_handle->crystal_window->DragStart(data, x, y);
    }

    void CrystalWindow_MouseCapture(P_INSTANCE(WindowHandle) window_handle) {
        window_handle->crystal_window->MouseCapture();
    }

    void CrystalWindow_MouseRelease(P_INSTANCE(WindowHandle) window_handle) {
        window_handle->crystal_window->MouseRelease();
    }

    void CrystalWindow_GLInit(P_INSTANCE(WindowHandle) window_handle) {
        CrystalWindow_GLInitVersioned(window_handle, 3, 3);
    }

    bool CrystalWindow_GLInitVersioned(P_INSTANCE(WindowHandle) window_handle, int32_t major, int32_t minor) {
        GLOptions options{};
        options.major = major;
        options.minor = minor;
        return window_handle->crystal_window->GLInitAdvanced(options);
    }

    bool CrystalWindow_GLInitAdvanced(P_INSTANCE(WindowHandle) window_handle, GLOptions options) {
        return window_handle->crystal_window->GLInitAdvanced(options);
    }

    void CrystalWindow_GLGetVersion(P_INSTANCE(WindowHandle) window_handle, P_OUT(int32_t) major, P_OUT(int32_t) minor) {
        window_handle->crystal_window->GLGetVersion(*major, *minor);
    }

    void CrystalWindow_GLMakeCurrent(P_INSTANCE(WindowHandle) window_handle) {
        window_handle->crystal_window->GLMakeCurrent();
    }

    void CrystalWindow_GLPresent(P_INSTANCE(WindowHandle) window_handle) {
        window_handle->crystal_window->GLPresent();
    }

    P_INSTANCE(void) CrystalWindow_GLGetProcAddress(P_INSTANCE(WindowHandle) window_handle, P_ELEMENTS(const char) name) {
        return window_handle->crystal_window->GLGetProcAddress(name);
    }

    void CrystalWindow_Show(P_INSTANCE(WindowHandle) window_handle, bool restore)
    {
        window_handle->crystal_window->Show(restore);
    }

    void CrystalWindow_Close(P_INSTANCE(WindowHandle) window_handle)
    {
        window_handle->crystal_window->Close();
    }

    void CrystalWindow_PostClose(P_INSTANCE(WindowHandle) window_handle) {
        window_handle->crystal_window->PostClose();
    }

    void CrystalWindow_SetSize(P_INSTANCE(WindowHandle) window_handle, int32_t width, int32_t height) {
        window_handle->crystal_window->SetSize(width, height);
    }

    void CrystalWindow_GetSize(P_INSTANCE(WindowHandle) window_handle, P_OUT(int32_t) width, P_OUT(int32_t) height) {
        window_handle->crystal_window->GetSize(*width, *height);
    }

    void CrystalWindow_SetLocation(P_INSTANCE(WindowHandle) window_handle, int32_t x, int32_t y) {
        window_handle->crystal_window->SetLocation(x, y);
    }

    void CrystalWindow_GetLocation(P_INSTANCE(WindowHandle) window_handle, P_OUT(int32_t) x, P_OUT(int32_t) y) {
        window_handle->crystal_window->GetLocation(*x, *y);
    }

    void CrystalWindow_CursorPix(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(PixData) pix, int32_t hot_x, int32_t hot_y) {
        CrystalWindow_SetCursor(window_handle, pix->pix_format.c_str, pix->pix_data, pix->pix_data_length, pix->width, pix->height, hot_x, hot_y);
    }

    void CrystalWindow_SetCursor(P_INSTANCE(WindowHandle) window_handle, utf8_string_struct pixformat, P_ELEMENTS(void)  pixdata, size_t pixdata_length, int32_t width, int32_t height, int32_t hot_x, int32_t hot_y) {
        window_handle->crystal_window->SetCursor(pixformat, pixdata, pixdata_length, width, height, hot_x, hot_y);
    }

    void CrystalWindow_SetStandardCursor(P_INSTANCE(WindowHandle) window_handle, CrystalCursor cursor_enum) {
        window_handle->crystal_window->SetStandardCursor(cursor_enum);
    }

    void CrystalWindow_IconPix(P_INSTANCE(WindowHandle) window_handle, P_INSTANCE(PixData) pix) {
        
        CrystalWindow_SetIcon(window_handle, pix->pix_format.c_str, pix->pix_data, pix->pix_data_length, pix->width, pix->height);
    }

    void CrystalWindow_SetIcon(P_INSTANCE(WindowHandle) window_handle, utf8_string_struct pixformat, P_ELEMENTS(void)  pixdata, size_t pixdata_length, int32_t width, int32_t height) {
        window_handle->crystal_window->SetIcon(pixformat, pixdata, pixdata_length, width, height);
    }

    void CrystalWindow_SetTitle(P_INSTANCE(WindowHandle) window_handle, utf8_string_struct title) {
        window_handle->crystal_window->SetTitle(title);
    }

    void CrystalWindow_GetTitle(P_INSTANCE(WindowHandle) window_handle, P_OUT(utf8_string_struct) title) {
        window_handle->crystal_window->GetTitle(title);
    }

    P_INSTANCE(WindowCallbacks)  get_callbacks(P_INSTANCE(WindowHandle)  handle);

    bool lookup_message_handler(P_INSTANCE(WindowHandle) window_handle, utf8_string_struct handler_name, P_ELEMENTS(void) *&handler_ptr) {
        auto* callbacks = get_callbacks(window_handle);

        if (strcmp(handler_name, "on_draw") == 0)
            handler_ptr = (P_OUT(P_INSTANCE(void)) )&callbacks->on_draw;
        else if (strcmp(handler_name, "on_key_down") == 0)
            handler_ptr = (P_OUT(P_INSTANCE(void)) )&callbacks->on_key_down;
        else if (strcmp(handler_name, "on_key_up") == 0)
            handler_ptr = (P_OUT(P_INSTANCE(void)) )&callbacks->on_key_up;
        else if (strcmp(handler_name, "on_mouse_move") == 0)
            handler_ptr = (P_OUT(P_INSTANCE(void)) )&callbacks->on_mouse_move;
        else if (strcmp(handler_name, "on_mouse_down") == 0)
            handler_ptr = (P_OUT(P_INSTANCE(void)) )&callbacks->on_mouse_down;
        else if (strcmp(handler_name, "on_mouse_up") == 0)
            handler_ptr = (P_OUT(P_INSTANCE(void)) )&callbacks->on_mouse_up;
        else if (strcmp(handler_name, "on_resize") == 0)
            handler_ptr = (P_OUT(P_INSTANCE(void)) )&callbacks->on_resize;
        else if (strcmp(handler_name, "on_close") == 0)
            handler_ptr = (P_OUT(P_INSTANCE(void)) )&callbacks->on_close;
        else if (strcmp(handler_name, "on_focus_in") == 0)
            handler_ptr = (P_OUT(P_INSTANCE(void)) )&callbacks->on_focus_in;
        else if (strcmp(handler_name, "on_focus_out") == 0)
            handler_ptr = (P_OUT(P_INSTANCE(void)) )&callbacks->on_focus_out;
        else if (strcmp(handler_name, "on_drag_receive_start") == 0)
            handler_ptr = (P_OUT(P_INSTANCE(void)) )&callbacks->on_drag_receive_start;
        else if (strcmp(handler_name, "on_drag_receive_enter") == 0)
            handler_ptr = (P_OUT(P_INSTANCE(void)) )&callbacks->on_drag_receive_enter;
        else if (strcmp(handler_name, "on_drag_receive_leave") == 0)
            handler_ptr = (P_OUT(P_INSTANCE(void)) )&callbacks->on_drag_receive_leave;
        else if (strcmp(handler_name, "on_drag_receive_motion") == 0)
            handler_ptr = (P_OUT(P_INSTANCE(void)) )&callbacks->on_drag_receive_motion;
        else if (strcmp(handler_name, "on_drag_receive_select") == 0)
            handler_ptr = (P_OUT(P_INSTANCE(void)) )&callbacks->on_drag_receive_select;
        else if (strcmp(handler_name, "on_drag_receive_drop") == 0)
            handler_ptr = (P_OUT(P_INSTANCE(void)) )&callbacks->on_drag_receive_drop;
        else if (strcmp(handler_name, "on_drag_provide_chosen") == 0)
            handler_ptr = (P_OUT(P_INSTANCE(void)) )&callbacks->on_drag_provide_chosen;
        else if (strcmp(handler_name, "on_drag_provide_status") == 0)
            handler_ptr = (P_OUT(P_INSTANCE(void)) )&callbacks->on_drag_provide_status;
        else if (strcmp(handler_name, "on_drag_provide_finished") == 0)
            handler_ptr = (P_OUT(P_INSTANCE(void)) )&callbacks->on_drag_provide_finished;
        else if (strcmp(handler_name, "on_clipboard_provide_chosen") == 0)
            handler_ptr = (P_OUT(P_INSTANCE(void)) )&callbacks->on_clipboard_provide_chosen;
        else if (strcmp(handler_name, "on_clipboard_receive_data") == 0)
            handler_ptr = (P_OUT(P_INSTANCE(void)) )&callbacks->on_clipboard_receive_data;
        else if (strcmp(handler_name, "on_data_interchange_error") == 0)
            handler_ptr = (P_OUT(P_INSTANCE(void)) )&callbacks->on_data_interchange_error;
        else if (strcmp(handler_name, "on_idle") == 0)
            handler_ptr = (P_OUT(P_INSTANCE(void)) )&callbacks->on_idle;
        else
            handler_ptr = nullptr;

        return (handler_ptr != nullptr);
    }

    bool CrystalWindow_SetMessageHandler(P_INSTANCE(WindowHandle) window_handle, utf8_string_struct handler_name, P_INSTANCE(void) handler) {
        P_INSTANCE(void) *addr;
        if (!lookup_message_handler(window_handle, handler_name, addr)) return false;
        *addr = handler;
        return true;
    }
}