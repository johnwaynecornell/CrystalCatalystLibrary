// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#ifndef CRYSTALCATALYST_APPLICATION_H
#define CRYSTALCATALYST_APPLICATION_H

#include "CrystalCatalystLibrary.h"
#include "Windowing/CrystalWindow.h"

extern "C" {
_EXPORT_ void Application_Init(int32_t argc, P_ELEMENTS(utf8_string) argv);
_EXPORT_ int32_t Application_Run(void);
_EXPORT_ void Application_SignalClose(void);

_EXPORT_ int32_t Application_ArgumentCount();
_EXPORT_ void Application_Argument(int32_t index, P_OUT(utf8_string) argument_out);
_EXPORT_ void Application_ArgumentRemove(int32_t index);

_EXPORT_ void Application_WindowAdd(P_INSTANCE(WindowHandle) window_handle);
_EXPORT_ void Application_WindowRemove(P_INSTANCE(WindowHandle) window_handle);
}

class CrystalApplication {
public:
    virtual ~CrystalApplication() = default;

    HandleNode window_head = {nullptr, nullptr};

    int32_t argument_count = 0;
    P_ELEMENTS(utf8_string) argument_array;

    int64_t retain_count = 0;

    bool CloseSignalled;

    virtual void SetArguments(int32_t argc, P_ELEMENTS(utf8_string) argv);
    virtual void Init();
    virtual int32_t Run();
    virtual void SignalClose();

    virtual int32_t ArgumentCount();
    virtual void Argument(int32_t index, P_OUT(utf8_string) argument_out);
    virtual void ArgumentRemove(int32_t index);

    virtual void WindowAdd(P_INSTANCE(WindowHandle) window_handle);
    virtual void WindowRemove(P_INSTANCE(WindowHandle) window_handle);

    virtual P_INSTANCE(WindowHandle) WindowCreate(int32_t width, int32_t height, utf8_string_const title) = 0;

    virtual void DispatchCycle() = 0;
    virtual bool HasMessage() = 0;

    virtual void RetainerIncrement();
    virtual void RetainerDecrement();
};

extern P_INSTANCE(CrystalApplication)TheApplication;
#endif //CRYSTALCATALYST_APPLICATION_H
