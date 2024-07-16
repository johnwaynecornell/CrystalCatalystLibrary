#include "Application.h"
#include <iostream>

P_INSTANCE(CrystalApplication)TheApplication = nullptr;


P_INSTANCE(CrystalApplication) platform_initialize();
void platform_uninitialize(int32_t argc, P_ELEMENTS(utf8_string) argv);

void Application_Init(int32_t argc, P_ELEMENTS(utf8_string) argv)
{
    TheApplication = platform_initialize();
    TheApplication->SetArguments(argc, argv);
    TheApplication->Init();
}

void CrystalApplication::SetArguments(int32_t argc, P_ELEMENTS(utf8_string) argv) {
    // Initialize arguments (if any)
    argument_count = argc;
    argument_array = new utf8_string [argument_count];
    for (int32_t i = 0; i < argument_count; i++) {
        argument_array[i] = argv[i];
    }
}


bool CloseSignalled = false;
int64_t RetainCount = 0;

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


void Application_Argument(int32_t index, P_OUT(utf8_string)  argument_out)
{
    TheApplication->Argument(index, argument_out);
}

void CrystalApplication::Argument(int32_t index, P_OUT(utf8_string) argument_out) {
    if (index < 0 || index >= argument_count) *argument_out = nullptr;
    else *argument_out = argument_array[index];
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

            free(removed);
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

        for (P_INSTANCE(HandleNode)  node = window_head.next; node != nullptr; node = node->next) {
            auto* callbacks = &node->handle->crystal_window->callbacks;

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