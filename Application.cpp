// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include "include/CrystalCatalystLibrary/CrystalCatalystLibrary.h"
#include <iostream>

namespace NewAge {
    P_INSTANCE(CrystalApplication)TheApplication = nullptr;


    P_INSTANCE(CrystalApplication) platform_initialize();
    void platform_uninitialize();

    void Application_Init(struct_array_struct<utf8_string_struct> args)
    {
        if (TheApplication != nullptr) {
            std::cerr << "Application already initialized. The app model means only one application can exist at a time." << std::endl;
            exit(1);
        }
        TheApplication = platform_initialize();
        TheApplication->SetArguments(args);
        TheApplication->Init();
    }

    void CrystalApplication::SetArguments(struct_array_struct<utf8_string_struct> args) {
        // Initialize arguments (if any)
        argument_array = std::move(args);
    }



    void Application_SignalClose(void)
    {
        TheApplication->SignalClose();
    }

    void CrystalApplication::SignalClose() {
        CloseSignalled = true;
    }

    int32_t Application_ArgumentCount()
    {
        return TheApplication->ArgumentCount();
    }

    int32_t CrystalApplication::ArgumentCount() {
        return argument_count;
    }


    utf8_string_struct Application_Argument(int32_t index)
    {
        return TheApplication->Argument(index);
    }

    utf8_string_struct CrystalApplication::Argument(int32_t index) {
        return this->argument_array[index];
    }


    void Application_ArgumentRemove(int32_t index)
    {
        TheApplication->ArgumentRemove(index);
    }

    void CrystalApplication::ArgumentRemove(int32_t index)
    {
        if (index < 0 || index >= argument_count) return;
        for (int32_t i = index; i < argument_count - 1; i++) argument_array[i] = argument_array[i + 1];
        argument_count--;
    }

    void Application_WindowAdd(P_INSTANCE(WindowHandle) window_handle) {
        TheApplication->WindowAdd(window_handle);
    }


    void CrystalApplication::WindowAdd(P_INSTANCE(WindowHandle) window_handle)
    {
        P_INSTANCE(HandleNode)  cur = &window_head;
        while (cur->next != nullptr) cur = cur->next;
        P_INSTANCE(HandleNode)  new_node = (P_INSTANCE(HandleNode) )malloc(sizeof(HandleNode));
        if (!new_node) {
            std::cerr << "Failed to allocate memory for new_node" << std::endl;
            return;
        }
        new_node->handle = window_handle;
        new_node->next = nullptr;

        cur->next = new_node;
    }
    void Application_WindowRemove(P_INSTANCE(WindowHandle) window_handle) {
        TheApplication->WindowRemove(window_handle);
    }

    void CrystalApplication::WindowRemove(P_INSTANCE(WindowHandle) window_handle)
    {
        P_INSTANCE(HandleNode)  cur = &window_head;

        if (window_handle == nullptr) return;

        while (cur->next != nullptr) {
            if (cur->next->handle == window_handle) {
                P_INSTANCE(HandleNode)  removed = cur->next;
                cur->next = removed->next;

                if (removed->handle) {
                    if (removed->handle->crystal_window) {
                        delete removed->handle->crystal_window;
                    }
                    free(removed->handle);
                }
                free(removed);
            } else {
                cur = cur->next;
            }
        }
    }

    void CrystalApplication::RetainerIncrement() {
        retain_count++;
    }

    void CrystalApplication::RetainerDecrement() {
        if (--retain_count == 0) SignalClose();
    }

    int32_t Application_Run() {
        return TheApplication->Run();
    }

    int32_t CrystalApplication::Run() {

        while (!CloseSignalled) {

            DispatchCycle();

            for (P_INSTANCE(HandleNode)  node = window_head.next; !CloseSignalled && node != nullptr; node = node->next) {
                auto* callbacks = &node->handle->crystal_window->callbacks;

                if (node->handle->crystal_window->ready)
                    if (callbacks->on_idle) {
                        callbacks->on_idle(node->handle);
                    }
            }

            HasMessage();
        }

        return 0;
    }

    void CrystalApplication::Init() {

    }



    bool CrystalWindow_HasMessage() {
        return TheApplication->HasMessage();
    }
}