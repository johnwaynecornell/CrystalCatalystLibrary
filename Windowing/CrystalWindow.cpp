// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include <cstring>
#include "../include/CrystalCatalystLibrary/CrystalCatalystLibrary.h"
#include "CrystalWindow.h"

#include <assert.h>
#include <iostream>

void CrystalWindow_ApplicationRetain(P_INSTANCE(WindowHandle) window_handle)
{
    window_handle->crystal_window->ApplicationRetain();
}

void CrystalWindow_ApplicationRelease(P_INSTANCE(WindowHandle) window_handle)
{
    window_handle->crystal_window->ApplicationRelease();
}

void CrystalWindow::ApplicationRetain() {
    TheApplication->RetainerIncrement();
}

void CrystalWindow::ApplicationRelease() {
    TheApplication->RetainerDecrement();
}

P_INSTANCE(WindowCallbacks)  get_callbacks(P_INSTANCE(WindowHandle) handle)
{
    return &handle->crystal_window->callbacks;
}

P_INSTANCE(WindowHandle) CrystalWindow_Create(int32_t width, int32_t height, utf8_string_const title) {
    return TheApplication->WindowCreate(width, height, title);
}

void CrystalWindow_PresentImage(P_INSTANCE(WindowHandle) window_handle, utf8_string_const pixformat, P_ELEMENTS(void)  pixdata, size_t pixdata_length, int32_t width, int32_t height) {
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

void CrystalWindow_GL_Init(P_INSTANCE(WindowHandle) window_handle) {
    window_handle->crystal_window->GL_Init();
}

void CrystalWindow_Show(P_INSTANCE(WindowHandle) window_handle, bool restore)
{
    window_handle->crystal_window->Show(restore);
}


P_INSTANCE(WindowCallbacks)  get_callbacks(P_INSTANCE(WindowHandle)  handle);

bool lookup_message_handler(P_INSTANCE(WindowHandle) window_handle, utf8_string_const handler_name, P_ELEMENTS(void) *&handler_ptr) {
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
    else if (strcmp(handler_name, "on_idle") == 0)
        handler_ptr = (P_OUT(P_INSTANCE(void)) )&callbacks->on_idle;
    else
        handler_ptr = nullptr;

    return (handler_ptr != nullptr);
}

bool CrystalWindow_SetMessaqgeHandler(P_INSTANCE(WindowHandle) window_handle, utf8_string_const handler_name, P_INSTANCE(void) handler) {
    P_INSTANCE(void) *addr;
    if (!lookup_message_handler(window_handle, handler_name, addr)) return false;
    *addr = handler;
    return true;
}
