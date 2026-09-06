// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.

#include <iostream>
#include <thread>
#include <atomic>
#include <cassert>
#include <vector>

#if !defined(_WIN32)
#include <sys/wait.h>
#include <unistd.h>
#endif

#include "CrystalCatalystLibrary/CrystalCatalystLibrary.h"
#if defined(__linux__)
#include "../Platform/Linux/CrystalApplication_X11.h"
#endif

using namespace NewAge;

void test_parent_thread_peek() {
    std::cout << "[TEST] Running test_parent_thread_peek..." << std::endl;
    // Step 1: On a thread before initialization, Application_Peek() must return nullptr.
    assert(Application_Peek() == nullptr);
    assert(TheApplication == nullptr);
    std::cout << "[TEST] test_parent_thread_peek PASSED." << std::endl;
}

void test_distinct_applications_on_threads() {
    std::cout << "[TEST] Running test_distinct_applications_on_threads..." << std::endl;

    P_INSTANCE(CrystalApplication) app1 = nullptr;
    P_INSTANCE(CrystalApplication) app2 = nullptr;

    std::atomic<bool> thread1_ready{false};
    std::atomic<bool> thread2_ready{false};
    std::atomic<bool> proceed{false};

    std::thread t1([&]() {
        assert(Application_Peek() == nullptr);
        struct_array_struct<utf8_string_struct> args;
        args.Alloc(0);
        Application_Init(args);
        app1 = Application_Peek();
        assert(app1 != nullptr);
        assert(TheApplication == app1);
        assert(Application_Peek() == app1); // Repeated calls on same thread return same instance

        #if defined(__linux__)
        assert(AppX11 == app1);
        #endif

        thread1_ready = true;
        while (!proceed) {
            std::this_thread::yield();
        }

        // Verify independent state
        assert(TheApplication->retain_count == 0);
        assert(!TheApplication->CloseSignalled);

        // Mutate state on thread 1
        TheApplication->RetainerIncrement();
        TheApplication->RetainerIncrement();
        assert(TheApplication->retain_count == 2);

        // Teardown
        delete TheApplication;
        TheApplication = nullptr;
        assert(Application_Peek() == nullptr);
    });

    std::thread t2([&]() {
        assert(Application_Peek() == nullptr);
        struct_array_struct<utf8_string_struct> args;
        args.Alloc(0);
        Application_Init(args);
        app2 = Application_Peek();
        assert(app2 != nullptr);
        assert(TheApplication == app2);
        assert(Application_Peek() == app2); // Repeated calls on same thread return same instance

        #if defined(__linux__)
        assert(AppX11 == app2);
        #endif

        thread2_ready = true;
        while (!proceed) {
            std::this_thread::yield();
        }

        // Verify independent state before thread 2 mutation
        assert(TheApplication->retain_count == 0);
        assert(!TheApplication->CloseSignalled);

        // Mutate state on thread 2
        Application_SignalClose();
        assert(TheApplication->CloseSignalled);
        assert(TheApplication->retain_count == 0);

        // Teardown
        delete TheApplication;
        TheApplication = nullptr;
        assert(Application_Peek() == nullptr);
    });

    while (!thread1_ready || !thread2_ready) {
        std::this_thread::yield();
    }

    assert(app1 != app2);
    std::cout << "  Thread 1 app pointer: " << app1 << std::endl;
    std::cout << "  Thread 2 app pointer: " << app2 << std::endl;

    proceed = true;

    t1.join();
    t2.join();

    std::cout << "[TEST] test_distinct_applications_on_threads PASSED." << std::endl;
}

static std::thread::id s_tid_callback_a;
static std::thread::id s_tid_callback_b;

static void on_idle_thread_a(P_INSTANCE(WindowHandle) handle) {
    s_tid_callback_a = std::this_thread::get_id();
    assert(Application_Peek() != nullptr);
    CrystalWindow_ApplicationRelease(handle);
    Application_SignalClose();
}

static void on_idle_thread_b(P_INSTANCE(WindowHandle) handle) {
    s_tid_callback_b = std::this_thread::get_id();
    assert(Application_Peek() != nullptr);
    CrystalWindow_ApplicationRelease(handle);
    Application_SignalClose();
}

void test_two_ui_threads_windows_and_callbacks_affinity() {
    std::cout << "[TEST] Running test_two_ui_threads_windows_and_callbacks_affinity..." << std::endl;

    // 1. On parent thread before init
    assert(Application_Peek() == nullptr);

    std::thread::id tid_a;
    std::thread::id tid_b;

    s_tid_callback_a = std::thread::id();
    s_tid_callback_b = std::thread::id();

    P_INSTANCE(CrystalApplication) app_a = nullptr;
    P_INSTANCE(CrystalApplication) app_b = nullptr;

    std::atomic<bool> thread_a_finished{false};
    std::atomic<bool> thread_b_finished{false};

    // UI Thread A
    std::thread thread_a([&]() {
        tid_a = std::this_thread::get_id();
        assert(Application_Peek() == nullptr);

        struct_array_struct<utf8_string_struct> args;
        args.Alloc(0);
        Application_Init(args);

        app_a = Application_Peek();
        assert(app_a != nullptr);
        assert(TheApplication == app_a);
        assert(Application_Peek() == app_a);

        P_INSTANCE(WindowHandle) win_a = CrystalWindow_CreateSimple(100, 100, "UI Thread A Window");
        assert(win_a != nullptr);
        CrystalWindow_ApplicationRetain(win_a);

        win_a->crystal_window->ready = true;
        CrystalWindow_SetMessageHandler(win_a, "on_idle", (P_INSTANCE(void))on_idle_thread_a);

        int32_t run_result = Application_Run();
        assert(run_result == 0);

        // 11. After Application_Run() teardown, Application_Peek() must be nullptr
        assert(Application_Peek() == nullptr);
        assert(TheApplication == nullptr);
        thread_a_finished = true;
    });

    // UI Thread B
    std::thread thread_b([&]() {
        tid_b = std::this_thread::get_id();
        assert(Application_Peek() == nullptr);

        struct_array_struct<utf8_string_struct> args;
        args.Alloc(0);
        Application_Init(args);

        app_b = Application_Peek();
        assert(app_b != nullptr);
        assert(TheApplication == app_b);
        assert(Application_Peek() == app_b);

        P_INSTANCE(WindowHandle) win_b = CrystalWindow_CreateSimple(100, 100, "UI Thread B Window");
        assert(win_b != nullptr);
        CrystalWindow_ApplicationRetain(win_b);

        win_b->crystal_window->ready = true;
        CrystalWindow_SetMessageHandler(win_b, "on_idle", (P_INSTANCE(void))on_idle_thread_b);

        int32_t run_result = Application_Run();
        assert(run_result == 0);

        // 11. After Application_Run() teardown, Application_Peek() must be nullptr
        assert(Application_Peek() == nullptr);
        assert(TheApplication == nullptr);
        thread_b_finished = true;
    });

    thread_a.join();
    thread_b.join();

    // Verify invariants across the two threads
    assert(tid_a != tid_b);
    assert(app_a != nullptr);
    assert(app_b != nullptr);
    assert(app_a != app_b);
    assert(thread_a_finished.load());
    assert(thread_b_finished.load());
    assert(s_tid_callback_a == tid_a);
    assert(s_tid_callback_b == tid_b);

    // Parent thread remains null
    assert(Application_Peek() == nullptr);

    std::cout << "  Thread A ID: " << tid_a << ", App: " << app_a << ", Callback Thread ID: " << s_tid_callback_a << std::endl;
    std::cout << "  Thread B ID: " << tid_b << ", App: " << app_b << ", Callback Thread ID: " << s_tid_callback_b << std::endl;
    std::cout << "[TEST] test_two_ui_threads_windows_and_callbacks_affinity PASSED." << std::endl;
}

void test_reinitialization_after_teardown() {
    std::cout << "[TEST] Running test_reinitialization_after_teardown..." << std::endl;

    std::thread t([&]() {
        assert(Application_Peek() == nullptr);

        struct_array_struct<utf8_string_struct> args;
        args.Alloc(0);
        Application_Init(args);
        assert(TheApplication != nullptr);
        assert(Application_Peek() == TheApplication);
        P_INSTANCE(CrystalApplication) first_app = Application_Peek();

        delete TheApplication;
        TheApplication = nullptr;

        assert(TheApplication == nullptr);
        assert(Application_Peek() == nullptr);

        // Second init on same thread after cleanup
        Application_Init(args);
        assert(TheApplication != nullptr);
        assert(Application_Peek() == TheApplication);
        P_INSTANCE(CrystalApplication) second_app = Application_Peek();

        delete TheApplication;
        TheApplication = nullptr;
        assert(Application_Peek() == nullptr);
    });

    t.join();
    std::cout << "[TEST] test_reinitialization_after_teardown PASSED." << std::endl;
}

void test_double_init_rejected_on_same_thread() {
    std::cout << "[TEST] Running test_double_init_rejected_on_same_thread..." << std::endl;

#if !defined(_WIN32)
    pid_t pid = fork();
    if (pid == 0) {
        // Child process
        struct_array_struct<utf8_string_struct> args;
        args.Alloc(0);
        Application_Init(args);
        // Second call on same thread should exit(1) with diagnostic
        Application_Init(args);
        _exit(0); // Should not be reached
    } else {
        int status = 0;
        waitpid(pid, &status, 0);
        assert(WIFEXITED(status));
        assert(WEXITSTATUS(status) == 1);
    }
#else
    std::cout << "  (Skipping fork-based exit test on Windows)" << std::endl;
#endif

    std::cout << "[TEST] test_double_init_rejected_on_same_thread PASSED." << std::endl;
}

void test_single_thread_lifecycle() {
    std::cout << "[TEST] Running test_single_thread_lifecycle..." << std::endl;

    assert(Application_Peek() == nullptr);

    struct_array_struct<utf8_string_struct> args;
    args.Alloc(0);
    Application_Init(args);
    assert(TheApplication != nullptr);
    assert(Application_Peek() == TheApplication);

    TheApplication->RetainerIncrement();
    assert(TheApplication->retain_count == 1);
    TheApplication->RetainerDecrement();
    assert(TheApplication->retain_count == 0);
    assert(TheApplication->CloseSignalled);

    int32_t run_res = Application_Run();
    assert(run_res == 0);
    assert(TheApplication == nullptr);
    assert(Application_Peek() == nullptr);

    std::cout << "[TEST] test_single_thread_lifecycle PASSED." << std::endl;
}

void test_multi_thread_window_isolation() {
    std::cout << "[TEST] Running test_multi_thread_window_isolation..." << std::endl;

    P_INSTANCE(WindowHandle) win1 = nullptr;
    P_INSTANCE(WindowHandle) win2 = nullptr;

    std::atomic<bool> t1_created{false};
    std::atomic<bool> t2_created{false};
    std::atomic<bool> proceed{false};

    std::thread t1([&]() {
        struct_array_struct<utf8_string_struct> args;
        args.Alloc(0);
        Application_Init(args);

        win1 = CrystalWindow_CreateSimple(100, 100, "Thread 1 Window");
        assert(win1 != nullptr);
        assert(TheApplication->window_head.next != nullptr);
        assert(TheApplication->window_head.next->handle == win1);
        assert(TheApplication->window_head.next->next == nullptr);

        t1_created = true;
        while (!proceed) {
            std::this_thread::yield();
        }

        // Verify that thread 1's window list only has win1
        assert(TheApplication->window_head.next != nullptr);
        assert(TheApplication->window_head.next->handle == win1);
        assert(TheApplication->window_head.next->next == nullptr);

        // Remove window on thread 1
        Application_WindowRemove(win1);
        assert(TheApplication->window_head.next == nullptr);

        delete TheApplication;
        TheApplication = nullptr;
        assert(Application_Peek() == nullptr);
    });

    std::thread t2([&]() {
        struct_array_struct<utf8_string_struct> args;
        args.Alloc(0);
        Application_Init(args);

        win2 = CrystalWindow_CreateSimple(100, 100, "Thread 2 Window");
        assert(win2 != nullptr);
        assert(TheApplication->window_head.next != nullptr);
        assert(TheApplication->window_head.next->handle == win2);
        assert(TheApplication->window_head.next->next == nullptr);

        t2_created = true;
        while (!proceed) {
            std::this_thread::yield();
        }

        // Verify that thread 2's window list only has win2
        assert(TheApplication->window_head.next != nullptr);
        assert(TheApplication->window_head.next->handle == win2);
        assert(TheApplication->window_head.next->next == nullptr);

        // Remove window on thread 2
        Application_WindowRemove(win2);
        assert(TheApplication->window_head.next == nullptr);

        delete TheApplication;
        TheApplication = nullptr;
        assert(Application_Peek() == nullptr);
    });

    while (!t1_created || !t2_created) {
        std::this_thread::yield();
    }

    assert(win1 != win2);

    proceed = true;

    t1.join();
    t2.join();

    std::cout << "[TEST] test_multi_thread_window_isolation PASSED." << std::endl;
}

int main() {
    std::cout << "=== Running CrystalCatalyst Multi-Thread Tests ===" << std::endl;
    test_parent_thread_peek();
    test_distinct_applications_on_threads();
    test_two_ui_threads_windows_and_callbacks_affinity();
    test_multi_thread_window_isolation();
    test_reinitialization_after_teardown();
    test_double_init_rejected_on_same_thread();
    test_single_thread_lifecycle();
    std::cout << "=== ALL TESTS PASSED ===" << std::endl;
    return 0;
}
