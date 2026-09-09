// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.

#include <iostream>
#include <cassert>
#include <atomic>
#include <thread>
#include <chrono>

#include "CrystalCatalystLibrary/CrystalCatalystLibrary.h"

#if defined(__linux__)
#include <X11/Xlib.h>
#include <X11/Xatom.h>
#include "../Platform/Linux/CrystalApplication_X11.h"
#include "../Platform/Linux/Windowing/CrystalWindow_X11.h"
#elif defined(_WIN32)
#include <windows.h>
#include "../Platform/Windows/CrystalApplication_Windows.h"
#include "../Platform/Windows/Windowing/CrystalWindow_Windows.h"
#endif

using namespace NewAge;

// ============================================================================
// Test 1: WindowCreate without Show
// Window must be ready immediately upon creation, must NOT be mapped (hidden),
// and OnIdle must execute normally.
// ============================================================================

static std::atomic<bool> s_hidden_idle_ran{false};

static void on_idle_hidden_window(P_INSTANCE(WindowHandle) handle) {
    s_hidden_idle_ran = true;
    assert(handle != nullptr);
    assert(handle->crystal_window != nullptr);
    assert(handle->crystal_window->ready == true);

#if defined(__linux__)
    auto* win_x11 = (CrystalWindow_X11*)handle->crystal_window;
    XWindowAttributes wa;
    Status st = XGetWindowAttributes(win_x11->display, win_x11->window, &wa);
    assert(st != 0);
    // Window must be unmapped (hidden)
    assert(wa.map_state == IsUnmapped);
#elif defined(_WIN32)
    auto* win_win = (CrystalWindow_Windows*)handle->crystal_window;
    assert(!IsWindowVisible(win_win->hwnd));
#endif

    CrystalWindow_ApplicationRelease(handle);
    Application_SignalClose();
}

void test_window_create_hidden_and_idle_processing() {
    std::cout << "[TEST] Running test_window_create_hidden_and_idle_processing..." << std::endl;

    s_hidden_idle_ran = false;

    struct_array_struct<utf8_string_struct> args;
    args.Alloc(0);
    Application_Init(args);

    P_INSTANCE(WindowHandle) win = CrystalWindow_Create(400, 300, "Hidden Window Test");
    assert(win != nullptr);
    assert(win->crystal_window != nullptr);

    // Invariant: successful WindowCreate -> ready == true
    assert(win->crystal_window->ready == true);

#if defined(__linux__)
    auto* win_x11 = (CrystalWindow_X11*)win->crystal_window;
    XWindowAttributes wa;
    Status st = XGetWindowAttributes(win_x11->display, win_x11->window, &wa);
    assert(st != 0);
    assert(wa.map_state == IsUnmapped);
#elif defined(_WIN32)
    auto* win_win = (CrystalWindow_Windows*)win->crystal_window;
    assert(!IsWindowVisible(win_win->hwnd));
#endif

    CrystalWindow_ApplicationRetain(win);
    CrystalWindow_SetMessageHandler(win, "on_idle", (P_INSTANCE(void))on_idle_hidden_window);

    int32_t rc = Application_Run();
    assert(rc == 0);
    assert(s_hidden_idle_ran.load() == true);

    delete TheApplication;
    TheApplication = nullptr;

    std::cout << "[TEST] test_window_create_hidden_and_idle_processing PASSED." << std::endl;
}

// ============================================================================
// Test 2: WindowCreate_Simple without Show
// Utility window must be ready immediately, must NOT be mapped,
// and OnIdle must execute normally.
// ============================================================================

static std::atomic<bool> s_simple_idle_ran{false};

static void on_idle_simple_window(P_INSTANCE(WindowHandle) handle) {
    s_simple_idle_ran = true;
    assert(handle != nullptr);
    assert(handle->crystal_window != nullptr);
    assert(handle->crystal_window->ready == true);

#if defined(__linux__)
    auto* win_x11 = (CrystalWindow_X11*)handle->crystal_window;
    XWindowAttributes wa;
    Status st = XGetWindowAttributes(win_x11->display, win_x11->window, &wa);
    assert(st != 0);
    assert(wa.map_state == IsUnmapped);
#elif defined(_WIN32)
    auto* win_win = (CrystalWindow_Windows*)handle->crystal_window;
    assert(!IsWindowVisible(win_win->hwnd));
#endif

    CrystalWindow_ApplicationRelease(handle);
    Application_SignalClose();
}

void test_window_create_simple_hidden_and_idle_processing() {
    std::cout << "[TEST] Running test_window_create_simple_hidden_and_idle_processing..." << std::endl;

    s_simple_idle_ran = false;

    struct_array_struct<utf8_string_struct> args;
    args.Alloc(0);
    Application_Init(args);

    P_INSTANCE(WindowHandle) win = CrystalWindow_CreateSimple(100, 100, "Hidden Simple Window");
    assert(win != nullptr);
    assert(win->crystal_window != nullptr);

    // Invariant: successful WindowCreate_Simple -> ready == true
    assert(win->crystal_window->ready == true);

#if defined(__linux__)
    auto* win_x11 = (CrystalWindow_X11*)win->crystal_window;
    XWindowAttributes wa;
    Status st = XGetWindowAttributes(win_x11->display, win_x11->window, &wa);
    assert(st != 0);
    assert(wa.map_state == IsUnmapped);
#elif defined(_WIN32)
    auto* win_win = (CrystalWindow_Windows*)win->crystal_window;
    assert(!IsWindowVisible(win_win->hwnd));
#endif

    CrystalWindow_ApplicationRetain(win);
    CrystalWindow_SetMessageHandler(win, "on_idle", (P_INSTANCE(void))on_idle_simple_window);

    int32_t rc = Application_Run();
    assert(rc == 0);
    assert(s_simple_idle_ran.load() == true);

    delete TheApplication;
    TheApplication = nullptr;

    std::cout << "[TEST] test_window_create_simple_hidden_and_idle_processing PASSED." << std::endl;
}

// ============================================================================
// Test 3: WindowCreate then Show
// Window becomes mapped/viewable, and Expose/draw callback executes.
// ============================================================================

static std::atomic<int> s_draw_count{0};
static std::atomic<int> s_idle_count{0};

static void on_draw_visible_window(P_INSTANCE(WindowHandle) handle) {
    s_draw_count++;
}

static void on_idle_visible_window(P_INSTANCE(WindowHandle) handle) {
    s_idle_count++;

    // Let a few cycles pass so draw / Expose can be handled
    if (s_draw_count.load() > 0 || s_idle_count >= 50) {
#if defined(__linux__)
        auto* win_x11 = (CrystalWindow_X11*)handle->crystal_window;
        XWindowAttributes wa;
        Status st = XGetWindowAttributes(win_x11->display, win_x11->window, &wa);
        assert(st != 0);
        // Once shown and dispatched, window must be mapped (not IsUnmapped)
        assert(wa.map_state != IsUnmapped);
#elif defined(_WIN32)
        auto* win_win = (CrystalWindow_Windows*)handle->crystal_window;
        assert(IsWindowVisible(win_win->hwnd));
#endif
        CrystalWindow_ApplicationRelease(handle);
        Application_SignalClose();
    }
}

void test_window_create_then_show_and_draw() {
    std::cout << "[TEST] Running test_window_create_then_show_and_draw..." << std::endl;

    s_draw_count = 0;
    s_idle_count = 0;

    struct_array_struct<utf8_string_struct> args;
    args.Alloc(0);
    Application_Init(args);

    P_INSTANCE(WindowHandle) win = CrystalWindow_Create(400, 300, "Visible Window Test");
    assert(win != nullptr);
    assert(win->crystal_window != nullptr);
    assert(win->crystal_window->ready == true);

#if defined(__linux__)
    auto* win_x11 = (CrystalWindow_X11*)win->crystal_window;
    XWindowAttributes wa_before;
    XGetWindowAttributes(win_x11->display, win_x11->window, &wa_before);
    assert(wa_before.map_state == IsUnmapped);
#elif defined(_WIN32)
    auto* win_win = (CrystalWindow_Windows*)win->crystal_window;
    assert(!IsWindowVisible(win_win->hwnd));
#endif

    CrystalWindow_SetMessageHandler(win, "on_draw", (P_INSTANCE(void))on_draw_visible_window);
    CrystalWindow_SetMessageHandler(win, "on_idle", (P_INSTANCE(void))on_idle_visible_window);
    CrystalWindow_ApplicationRetain(win);

    // Call Show
    CrystalWindow_Show(win, true);

    int32_t rc = Application_Run();
    assert(rc == 0);
    assert(s_idle_count.load() > 0);
    // Draw callback must have been executed on Expose after Show()
    assert(s_draw_count.load() > 0);

    delete TheApplication;
    TheApplication = nullptr;

    std::cout << "[TEST] test_window_create_then_show_and_draw PASSED (Draws: " << s_draw_count.load() << ")." << std::endl;
}

// ============================================================================
// Test 4: Repeated Show calls are safe and idempotent
// ============================================================================

static std::atomic<int> s_repeated_idle_count{0};

static void on_idle_repeated_show_window(P_INSTANCE(WindowHandle) handle) {
    s_repeated_idle_count++;
    if (s_repeated_idle_count >= 10) {
#if defined(__linux__)
        auto* win_x11 = (CrystalWindow_X11*)handle->crystal_window;
        XWindowAttributes wa;
        Status st = XGetWindowAttributes(win_x11->display, win_x11->window, &wa);
        assert(st != 0);
        assert(wa.map_state != IsUnmapped);
#elif defined(_WIN32)
        auto* win_win = (CrystalWindow_Windows*)handle->crystal_window;
        assert(IsWindowVisible(win_win->hwnd));
#endif
        CrystalWindow_ApplicationRelease(handle);
        Application_SignalClose();
    }
}

void test_repeated_show_safety() {
    std::cout << "[TEST] Running test_repeated_show_safety..." << std::endl;

    s_repeated_idle_count = 0;

    struct_array_struct<utf8_string_struct> args;
    args.Alloc(0);
    Application_Init(args);

    P_INSTANCE(WindowHandle) win = CrystalWindow_Create(400, 300, "Repeated Show Test");
    assert(win != nullptr);

    CrystalWindow_SetMessageHandler(win, "on_idle", (P_INSTANCE(void))on_idle_repeated_show_window);
    CrystalWindow_ApplicationRetain(win);

    // Repeated Show calls with different restore flags
    CrystalWindow_Show(win, true);
    CrystalWindow_Show(win, false);
    CrystalWindow_Show(win, true);

    int32_t rc = Application_Run();
    assert(rc == 0);
    assert(s_repeated_idle_count.load() >= 10);

    delete TheApplication;
    TheApplication = nullptr;

    std::cout << "[TEST] test_repeated_show_safety PASSED." << std::endl;
}

// ============================================================================
// Test 5: Hidden Simple Utility Window Clipboard Operation
// ============================================================================

static std::atomic<bool> s_clipboard_test_done{false};

static void on_idle_clipboard_utility(P_INSTANCE(WindowHandle) handle) {
    // Copy text via DataInterchange on hidden utility window
    P_INSTANCE(DataInterchange) di = DataInterchange_Create();
    const char* test_payload = "CrystalCatalyst Hidden Window Clipboard Payload";
    DataInterchange_SelectionSet(di, "text/plain", (P_ELEMENTS(void))test_payload, strlen(test_payload));
    CrystalWindow_ClipboardCopy(handle, di);

    s_clipboard_test_done = true;
    CrystalWindow_ApplicationRelease(handle);
    Application_SignalClose();
}

void test_hidden_simple_window_clipboard() {
    std::cout << "[TEST] Running test_hidden_simple_window_clipboard..." << std::endl;

    s_clipboard_test_done = false;

    struct_array_struct<utf8_string_struct> args;
    args.Alloc(0);
    Application_Init(args);

    P_INSTANCE(WindowHandle) win = CrystalWindow_CreateSimple(1, 1, "Clip Utility Test");
    assert(win != nullptr);
    assert(win->crystal_window->ready == true);

#if defined(__linux__)
    auto* win_x11 = (CrystalWindow_X11*)win->crystal_window;
    XWindowAttributes wa;
    XGetWindowAttributes(win_x11->display, win_x11->window, &wa);
    assert(wa.map_state == IsUnmapped);
#elif defined(_WIN32)
    auto* win_win = (CrystalWindow_Windows*)win->crystal_window;
    assert(!IsWindowVisible(win_win->hwnd));
#endif

    CrystalWindow_ApplicationRetain(win);
    CrystalWindow_SetMessageHandler(win, "on_idle", (P_INSTANCE(void))on_idle_clipboard_utility);

    int32_t rc = Application_Run();
    assert(rc == 0);
    assert(s_clipboard_test_done.load() == true);

    delete TheApplication;
    TheApplication = nullptr;

    std::cout << "[TEST] test_hidden_simple_window_clipboard PASSED." << std::endl;
}

// ============================================================================
// Test 6: OpenGL Window Creation, Show, and Close Lifecycle
// Ensures OpenGL context initialization and subsequent window close / teardown
// does NOT crash or segfault.
// ============================================================================

static std::atomic<bool> s_opengl_close_idle_ran{false};

static void on_idle_opengl_window(P_INSTANCE(WindowHandle) handle) {
    s_opengl_close_idle_ran = true;
    CrystalWindow_ApplicationRelease(handle);
    Application_SignalClose();
}

void test_opengl_window_close_lifecycle() {
    std::cout << "[TEST] Running test_opengl_window_close_lifecycle..." << std::endl;

    s_opengl_close_idle_ran = false;

    struct_array_struct<utf8_string_struct> args;
    args.Alloc(0);
    Application_Init(args);

    P_INSTANCE(WindowHandle) win = CrystalWindow_Create(400, 300, "OpenGL Close Test");
    assert(win != nullptr);

    bool gl_ok = CrystalWindow_GLInitVersioned(win, 3, 3);
    assert(gl_ok);

    CrystalWindow_Show(win, true);
    CrystalWindow_ApplicationRetain(win);
    CrystalWindow_SetMessageHandler(win, "on_idle", (P_INSTANCE(void))on_idle_opengl_window);

    int32_t rc = Application_Run();
    assert(rc == 0);
    assert(s_opengl_close_idle_ran.load() == true);

    std::cout << "[TEST] test_opengl_window_close_lifecycle PASSED." << std::endl;
}

int main() {
    std::cout << "=== Running CrystalCatalyst Window Lifecycle Tests ===" << std::endl;

    test_window_create_hidden_and_idle_processing();
    test_window_create_simple_hidden_and_idle_processing();
    test_window_create_then_show_and_draw();
    test_repeated_show_safety();
    test_hidden_simple_window_clipboard();
    test_opengl_window_close_lifecycle();

    std::cout << "=== ALL WINDOW LIFECYCLE TESTS PASSED ===" << std::endl;
    return 0;
}
