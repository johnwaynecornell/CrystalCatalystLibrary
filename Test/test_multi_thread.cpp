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

void test_distinct_applications_on_threads() {
    std::cout << "[TEST] Running test_distinct_applications_on_threads..." << std::endl;

    P_INSTANCE(CrystalApplication) app1 = nullptr;
    P_INSTANCE(CrystalApplication) app2 = nullptr;

    std::atomic<bool> thread1_ready{false};
    std::atomic<bool> thread2_ready{false};
    std::atomic<bool> proceed{false};

    std::thread t1([&]() {
        struct_array_struct<utf8_string_struct> args;
        args.Alloc(0);
        Application_Init(args);
        app1 = TheApplication;
        assert(app1 != nullptr);

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
    });

    std::thread t2([&]() {
        struct_array_struct<utf8_string_struct> args;
        args.Alloc(0);
        Application_Init(args);
        app2 = TheApplication;
        assert(app2 != nullptr);

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

void test_reinitialization_after_teardown() {
    std::cout << "[TEST] Running test_reinitialization_after_teardown..." << std::endl;

    std::thread t([&]() {
        struct_array_struct<utf8_string_struct> args;
        args.Alloc(0);
        Application_Init(args);
        assert(TheApplication != nullptr);
        P_INSTANCE(CrystalApplication) first_app = TheApplication;

        delete TheApplication;
        TheApplication = nullptr;

        assert(TheApplication == nullptr);

        // Second init on same thread after cleanup
        Application_Init(args);
        assert(TheApplication != nullptr);
        P_INSTANCE(CrystalApplication) second_app = TheApplication;

        delete TheApplication;
        TheApplication = nullptr;
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

    struct_array_struct<utf8_string_struct> args;
    args.Alloc(0);
    Application_Init(args);
    assert(TheApplication != nullptr);

    TheApplication->RetainerIncrement();
    assert(TheApplication->retain_count == 1);
    TheApplication->RetainerDecrement();
    assert(TheApplication->retain_count == 0);
    assert(TheApplication->CloseSignalled);

    int32_t run_res = Application_Run();
    assert(run_res == 0);
    assert(TheApplication == nullptr);

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
    test_distinct_applications_on_threads();
    test_multi_thread_window_isolation();
    test_reinitialization_after_teardown();
    test_double_init_rejected_on_same_thread();
    test_single_thread_lifecycle();
    std::cout << "=== ALL TESTS PASSED ===" << std::endl;
    return 0;
}
