// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#ifndef CRYSTALCATALYST_APPLICATION_H
#define CRYSTALCATALYST_APPLICATION_H

#include "CrystalCatalystLibrary.h"

using namespace JWCEssentials;

namespace NewAge {
    class CrystalApplication {
    public:
        virtual ~CrystalApplication();

        HandleNode window_head = {nullptr, nullptr};

        int32_t argument_count = 0;
        struct_array_struct<utf8_string_struct> argument_array;

        int64_t retain_count = 0;

        bool CloseSignalled = false;

        virtual void SetArguments(struct_array_struct<utf8_string_struct> args);
        virtual void Init();
        virtual int32_t Run();
        virtual void SignalClose();

        virtual int32_t ArgumentCount();
        virtual utf8_string_struct Argument(int32_t index);
        virtual void ArgumentRemove(int32_t index);

        virtual void WindowAdd(P_INSTANCE(WindowHandle) window_handle);
        virtual void WindowRemove(P_INSTANCE(WindowHandle) window_handle);

        virtual P_INSTANCE(WindowHandle) WindowCreate(int32_t width, int32_t height, utf8_string_struct title) = 0;
        virtual P_INSTANCE(WindowHandle) WindowCreate_Simple(int32_t width, int32_t height, utf8_string_struct title) = 0;

        virtual void DispatchCycle() = 0;
        virtual bool HasMessage() = 0;

        virtual void RetainerIncrement();
        virtual void RetainerDecrement();
    };

    extern thread_local P_INSTANCE(CrystalApplication) TheApplication;

    _EXPORT_ P_INSTANCE(CrystalApplication) Application_Peek();

    _EXPORT_ void Application_Init(struct_array_struct<utf8_string_struct> args);
    _EXPORT_ int32_t Application_Run(void);
    _EXPORT_ void Application_SignalClose(void);

    _EXPORT_ int32_t Application_ArgumentCount();
    _EXPORT_ utf8_string_struct Application_Argument(int32_t index);
    _EXPORT_ void Application_ArgumentRemove(int32_t index);

    _EXPORT_ void Application_WindowAdd(P_INSTANCE(WindowHandle) window_handle);
    _EXPORT_ void Application_WindowRemove(P_INSTANCE(WindowHandle) window_handle);
}

#endif //CRYSTALCATALYST_APPLICATION_H
