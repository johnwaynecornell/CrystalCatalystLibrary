#include <iostream>
#include <vector>
#include <cassert>
#include <cstring>
#include <thread>
#include <atomic>

#include "CrystalCatalystLibrary/CrystalCatalystLibrary.h"

#if defined(__linux__)
#include <X11/Xatom.h>
#include <X11/Xlib.h>
#include "../Platform/Linux/CrystalApplication_X11.h"
#include "../Platform/Linux/Windowing/CrystalWindow_X11.h"
#include "../Platform/Linux/Windowing/Clipboard_X11.h"
#include "../Platform/Linux/Windowing/FileUri_Linux.h"
#endif

using namespace NewAge;

// Helper to generate binary test buffer with embedded null bytes
static std::vector<uint8_t> create_binary_png_data() {
    // Standard PNG signature followed by arbitrary binary chunks with zeroes
    std::vector<uint8_t> data = {
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG magic
        0x00, 0x00, 0x00, 0x0D,                         // IHDR length (13)
        0x49, 0x48, 0x44, 0x52,                         // "IHDR"
        0x00, 0x00, 0x01, 0x00,                         // Width: 256
        0x00, 0x00, 0x01, 0x00,                         // Height: 256
        0x08, 0x06, 0x00, 0x00, 0x00,                   // 8-bit RGBA
        0x00, 0x00, 0x00, 0x00,                         // Embedded zeroes
        0xDE, 0xAD, 0xBE, 0xEF, 0x00, 0x00, 0xCA, 0xFE
    };
    return data;
}

static std::vector<uint8_t> create_binary_bmp_data() {
    // Valid minimal BMP 1x1 24bpp file stream
    // BITMAPFILEHEADER (14 bytes)
    // BITMAPINFOHEADER (40 bytes)
    // Pixel data (4 bytes: B, G, R, pad)
    std::vector<uint8_t> data = {
        'B', 'M',                                       // bfType
        58, 0, 0, 0,                                    // bfSize (14 + 40 + 4 = 58)
        0, 0, 0, 0,                                     // bfReserved1, bfReserved2
        54, 0, 0, 0,                                    // bfOffBits (14 + 40 = 54)
        40, 0, 0, 0,                                    // biSize (40)
        1, 0, 0, 0,                                     // biWidth (1)
        1, 0, 0, 0,                                     // biHeight (1)
        1, 0,                                           // biPlanes (1)
        24, 0,                                          // biBitCount (24)
        0, 0, 0, 0,                                     // biCompression (BI_RGB = 0)
        4, 0, 0, 0,                                     // biSizeImage (4)
        0, 0, 0, 0,                                     // biXPelsPerMeter
        0, 0, 0, 0,                                     // biYPelsPerMeter
        0, 0, 0, 0,                                     // biClrUsed
        0, 0, 0, 0,                                     // biClrImportant
        0x00, 0xFF, 0x00, 0x00                          // Pixel bits with embedded zeroes
    };
    return data;
}

void test_common_data_interchange_formats() {
    std::cout << "[TEST] Running test_common_data_interchange_formats..." << std::endl;

    P_INSTANCE(DataInterchange) di = DataInterchange_Create();
    assert(di != nullptr);

    // 1. Add image/png and image/bmp
    DataInterchange_FormatAdd(di, "image/png");
    DataInterchange_FormatAdd(di, "image/bmp");

    assert(DataInterchange_FormatExists(di, "image/png"));
    assert(DataInterchange_FormatExists(di, "image/bmp"));
    assert(!DataInterchange_FormatExists(di, "image/jpeg"));

    // Verify enumeration
    int count = 0;
    bool found_png = false;
    bool found_bmp = false;
    for (P_INSTANCE(DataInterchange::Node) n = DataInterchange_FormatEnum(di); n != nullptr; n = DataInterchange_FormatEnumNext(n)) {
        utf8_string_struct t;
        DataInterchange_FormatEnumText(n, &t);
        if (strcmp(t, "image/png") == 0) found_png = true;
        if (strcmp(t, "image/bmp") == 0) found_bmp = true;
        count++;
    }
    assert(count == 2);
    assert(found_png && found_bmp);

    // 2. Test binary data survives selection/reveal without truncation at zero bytes
    auto png_bytes = create_binary_png_data();
    DataInterchange_SelectionSet(di, "image/png", png_bytes.data(), png_bytes.size());

    utf8_string_struct revealed_format = nullptr;
    P_INSTANCE(void) revealed_data = nullptr;
    size_t revealed_size = 0;

    DataInterchange_SelectionReveal(di, &revealed_format, &revealed_data, &revealed_size);
    assert(revealed_format != nullptr);
    assert(strcmp(revealed_format, "image/png") == 0);
    assert(revealed_size == png_bytes.size());
    assert(memcmp(revealed_data, png_bytes.data(), png_bytes.size()) == 0);

    // Test BMP binary payload
    auto bmp_bytes = create_binary_bmp_data();
    DataInterchange_SelectionSet(di, "image/bmp", bmp_bytes.data(), bmp_bytes.size());

    DataInterchange_SelectionReveal(di, &revealed_format, &revealed_data, &revealed_size);
    assert(revealed_format != nullptr);
    assert(strcmp(revealed_format, "image/bmp") == 0);
    assert(revealed_size == bmp_bytes.size());
    assert(memcmp(revealed_data, bmp_bytes.data(), bmp_bytes.size()) == 0);

    DataInterchange_Free(di);
    std::cout << "[TEST] test_common_data_interchange_formats PASSED." << std::endl;
}

#if defined(__linux__)

void test_x11_atom_format_mapping_and_aliases() {
    std::cout << "[TEST] Running test_x11_atom_format_mapping_and_aliases..." << std::endl;

    struct_array_struct<utf8_string_struct> args;
    args.Alloc(0);
    Application_Init(args);

    P_INSTANCE(WindowHandle) win = CrystalWindow_CreateSimple(100, 100, "X11 Image Test Window");
    assert(win != nullptr);
    auto* xwin = static_cast<CrystalWindow_X11*>(win->crystal_window);

    Display* dpy = xwin->display;

    // Test FormatToAtom
    Atom png_atom = None;
    assert(FormatToAtom(dpy, "image/png", &png_atom));
    assert(png_atom == XInternAtom(dpy, "image/png", False));

    Atom bmp_atom = None;
    assert(FormatToAtom(dpy, "image/bmp", &bmp_atom));
    assert(bmp_atom == XInternAtom(dpy, "image/bmp", False));

    // Test DataImterchange_FormatsFromAtomArray alias normalization
    P_INSTANCE(DataInterchange) di = DataInterchange_Create();
    di->m_handle = win;

    Atom advertised[5];
    advertised[0] = XInternAtom(dpy, "image/png", False);
    advertised[1] = XInternAtom(dpy, "image/x-bmp", False);
    advertised[2] = XInternAtom(dpy, "image/x-MS-bmp", False);
    advertised[3] = XInternAtom(dpy, "image/bmp", False);
    advertised[4] = XInternAtom(dpy, "text/uri-list", False);

    DataImterchange_FormatsFromAtomArray(di, advertised, 5);

    // image/png should be present
    assert(DataInterchange_FormatExists(di, "image/png"));
    // image/bmp should be present (normalized from x-bmp, x-MS-bmp, and bmp)
    assert(DataInterchange_FormatExists(di, "image/bmp"));
    // text/file-uri should be present (normalized from text/uri-list)
    assert(DataInterchange_FormatExists(di, "text/file-uri"));

    // Check count: image/bmp should not be duplicated
    int count = 0;
    for (P_INSTANCE(DataInterchange::Node) n = DataInterchange_FormatEnum(di); n != nullptr; n = DataInterchange_FormatEnumNext(n)) {
        count++;
    }
    assert(count == 3); // image/png, image/bmp, text/file-uri

    // Verify xwin->advertised_atoms captured the 5 atoms
    assert(xwin->advertised_atoms.size() == 5);

    DataInterchange_Free(di);

    // Teardown
    delete TheApplication;
    TheApplication = nullptr;

    std::cout << "[TEST] test_x11_atom_format_mapping_and_aliases PASSED." << std::endl;
}

static std::vector<uint8_t> s_png_payload;
static std::vector<uint8_t> s_bmp_payload;

static void on_clipboard_provide_cb(P_INSTANCE(WindowHandle) handle, P_INSTANCE(DataInterchange) data, utf8_string_struct format) {
    if (strcmp(format, "image/png") == 0) {
        DataInterchange_SelectionSet(data, format, s_png_payload.data(), s_png_payload.size());
    } else if (strcmp(format, "image/bmp") == 0) {
        DataInterchange_SelectionSet(data, format, s_bmp_payload.data(), s_bmp_payload.size());
    }
}

void test_x11_clipboard_image_roundtrip() {
    std::cout << "[TEST] Running test_x11_clipboard_image_roundtrip..." << std::endl;

    struct_array_struct<utf8_string_struct> args;
    args.Alloc(0);
    Application_Init(args);

    P_INSTANCE(WindowHandle) win = CrystalWindow_CreateSimple(100, 100, "X11 Roundtrip Test Window");
    assert(win != nullptr);
    win->crystal_window->callbacks.on_clipboard_provide_chosen = on_clipboard_provide_cb;

    s_png_payload = create_binary_png_data();
    s_bmp_payload = create_binary_bmp_data();

    // 1. Copy image/png and image/bmp to clipboard
    P_INSTANCE(DataInterchange) copy_data = DataInterchange_Create();
    DataInterchange_FormatAdd(copy_data, "image/png");
    DataInterchange_FormatAdd(copy_data, "image/bmp");

    CrystalWindow_ClipboardCopy(win, copy_data);

    // 2. Paste from clipboard and verify advertised formats
    P_INSTANCE(DataInterchange) paste_data = CrystalWindow_ClipboardPaste(win);
    assert(paste_data != nullptr);

    assert(DataInterchange_FormatExists(paste_data, "image/png"));
    assert(DataInterchange_FormatExists(paste_data, "image/bmp"));

    // 3. Select image/png and verify byte-for-byte equality
    DataInterchange_Select(paste_data, "image/png");

    utf8_string_struct sel_format = nullptr;
    P_INSTANCE(void) sel_data = nullptr;
    size_t sel_size = 0;
    DataInterchange_SelectionReveal(paste_data, &sel_format, &sel_data, &sel_size);

    assert(sel_format != nullptr);
    assert(strcmp(sel_format, "image/png") == 0);
    assert(sel_size == s_png_payload.size());
    assert(memcmp(sel_data, s_png_payload.data(), s_png_payload.size()) == 0);

    // 4. Select image/bmp and verify byte-for-byte equality
    DataInterchange_Select(paste_data, "image/bmp");
    DataInterchange_SelectionReveal(paste_data, &sel_format, &sel_data, &sel_size);

    assert(sel_format != nullptr);
    assert(strcmp(sel_format, "image/bmp") == 0);
    assert(sel_size == s_bmp_payload.size());
    assert(memcmp(sel_data, s_bmp_payload.data(), s_bmp_payload.size()) == 0);

    DataInterchange_Free(paste_data);
    DataInterchange_Free(copy_data);

    // Teardown
    delete TheApplication;
    TheApplication = nullptr;

    std::cout << "[TEST] test_x11_clipboard_image_roundtrip PASSED." << std::endl;
}

#endif

void test_bmp_dib_conversion_invariants() {
    std::cout << "[TEST] Running test_bmp_dib_conversion_invariants..." << std::endl;

    auto full_bmp = create_binary_bmp_data();
    assert(full_bmp.size() == 58);
    assert(full_bmp[0] == 'B' && full_bmp[1] == 'M');

    // Extract DIB part (skipping 14 bytes)
    std::vector<uint8_t> dib_part(full_bmp.begin() + 14, full_bmp.end());
    assert(dib_part.size() == 44);
    uint32_t biSize = *reinterpret_cast<uint32_t*>(dib_part.data());
    assert(biSize == 40);

    // Verify DIB payload can be reconstructed to valid BMP byte stream
    uint32_t offsetInDib = 40; // 24bpp, biClrUsed = 0 -> 40
    uint32_t bfOffBits = 14 + offsetInDib;
    uint32_t bfSize = 14 + (uint32_t)dib_part.size();

    std::vector<uint8_t> reconstructed_bmp(14 + dib_part.size());
    reconstructed_bmp[0] = 'B';
    reconstructed_bmp[1] = 'M';
    *reinterpret_cast<uint32_t*>(&reconstructed_bmp[2]) = bfSize;
    *reinterpret_cast<uint16_t*>(&reconstructed_bmp[6]) = 0;
    *reinterpret_cast<uint16_t*>(&reconstructed_bmp[8]) = 0;
    *reinterpret_cast<uint32_t*>(&reconstructed_bmp[10]) = bfOffBits;
    memcpy(reconstructed_bmp.data() + 14, dib_part.data(), dib_part.size());

    assert(reconstructed_bmp.size() == full_bmp.size());
    assert(memcmp(reconstructed_bmp.data(), full_bmp.data(), full_bmp.size()) == 0);

    std::cout << "[TEST] test_bmp_dib_conversion_invariants PASSED." << std::endl;
}

#if defined(__linux__)

void test_x11_bmp_alias_roundtrip() {
    std::cout << "[TEST] Running test_x11_bmp_alias_roundtrip..." << std::endl;

    struct_array_struct<utf8_string_struct> args;
    args.Alloc(0);
    Application_Init(args);

    P_INSTANCE(WindowHandle) win = CrystalWindow_CreateSimple(100, 100, "X11 Alias Test Window");
    assert(win != nullptr);
    win->crystal_window->callbacks.on_clipboard_provide_chosen = on_clipboard_provide_cb;

    s_bmp_payload = create_binary_bmp_data();

    // 1. Copy image/bmp to clipboard
    P_INSTANCE(DataInterchange) copy_data = DataInterchange_Create();
    DataInterchange_FormatAdd(copy_data, "image/bmp");
    CrystalWindow_ClipboardCopy(win, copy_data);

    // 2. Paste and manually inject image/x-MS-bmp alias in advertised atoms to simulate an external app
    P_INSTANCE(DataInterchange) paste_data = CrystalWindow_ClipboardPaste(win);
    assert(paste_data != nullptr);

    auto* xwin = static_cast<CrystalWindow_X11*>(win->crystal_window);
    Display* dpy = xwin->display;
    xwin->advertised_atoms.clear();
    xwin->advertised_atoms.push_back(XInternAtom(dpy, "image/x-MS-bmp", False));

    // Selecting canonical "image/bmp" should resolve to target atom "image/x-MS-bmp" and succeed
    DataInterchange_Select(paste_data, "image/bmp");

    utf8_string_struct sel_format = nullptr;
    P_INSTANCE(void) sel_data = nullptr;
    size_t sel_size = 0;
    DataInterchange_SelectionReveal(paste_data, &sel_format, &sel_data, &sel_size);

    assert(sel_format != nullptr);
    assert(strcmp(sel_format, "image/bmp") == 0);
    assert(sel_size == s_bmp_payload.size());
    assert(memcmp(sel_data, s_bmp_payload.data(), s_bmp_payload.size()) == 0);

    DataInterchange_Free(paste_data);
    DataInterchange_Free(copy_data);

    // Teardown
    delete TheApplication;
    TheApplication = nullptr;

    std::cout << "[TEST] test_x11_bmp_alias_roundtrip PASSED." << std::endl;
}

void test_x11_clipboard_persistence_without_manager() {
    std::cout << "[TEST] Running test_x11_clipboard_persistence_without_manager..." << std::endl;

    struct_array_struct<utf8_string_struct> args;
    args.Alloc(0);
    Application_Init(args);

    P_INSTANCE(WindowHandle) win = CrystalWindow_CreateSimple(100, 100, "Persistence No Manager Test Window");
    assert(win != nullptr);

    static std::string last_error_msg;
    static bool error_called = false;
    error_called = false;
    last_error_msg.clear();

    win->crystal_window->callbacks.on_data_interchange_error = [](P_INSTANCE(WindowHandle) h, P_INSTANCE(DataInterchange) di, utf8_string_struct msg) {
        error_called = true;
        if (msg) last_error_msg = msg;
    };

    P_INSTANCE(DataInterchange) copy_data = DataInterchange_Create();
    DataInterchange_FormatAdd(copy_data, "text/plain");

    // When no CLIPBOARD_MANAGER is running, CrystalWindow_ClipboardCopyPersist should report error and return cleanly
    CrystalWindow_ClipboardCopyPersist(win, copy_data);

    assert(error_called == true);
    assert(last_error_msg.find("no clipboard manager running") != std::string::npos);

    DataInterchange_Free(copy_data);

    delete TheApplication;
    TheApplication = nullptr;

    std::cout << "[TEST] test_x11_clipboard_persistence_without_manager PASSED." << std::endl;
}

void test_x11_clipboard_persistence_with_manager() {
    std::cout << "[TEST] Running test_x11_clipboard_persistence_with_manager..." << std::endl;

    std::atomic<bool> manager_ready(false);
    std::atomic<bool> manager_done(false);
    std::atomic<bool> manager_success(false);
    std::vector<uint8_t> mgr_saved_png;
    std::vector<uint8_t> mgr_saved_bmp;

    auto png_expected = create_binary_png_data();
    auto bmp_expected = create_binary_bmp_data();

    // Spawn Thread M: Simulated Clipboard Manager
    std::thread manager_thread([&]() {
        Display* dpy = XOpenDisplay(nullptr);
        if (!dpy) return;

        Atom cm_atom = XInternAtom(dpy, "CLIPBOARD_MANAGER", False);
        Atom cb_atom = XInternAtom(dpy, "CLIPBOARD", False);
        Atom st_atom = XInternAtom(dpy, "SAVE_TARGETS", False);
        Atom targets_atom = XInternAtom(dpy, "TARGETS", False);
        Atom sel_prop_atom = XInternAtom(dpy, "MGR_SAVED_DATA", False);
        Atom png_atom = XInternAtom(dpy, "image/png", False);
        Atom bmp_atom = XInternAtom(dpy, "image/bmp", False);

        int screen = DefaultScreen(dpy);
        Window root = RootWindow(dpy, screen);
        Window mgr_win = XCreateSimpleWindow(dpy, root, -100, -100, 10, 10, 0, 0, 0);

        XSelectInput(dpy, mgr_win, PropertyChangeMask);
        XSetSelectionOwner(dpy, cm_atom, mgr_win, CurrentTime);
        XFlush(dpy);

        assert(XGetSelectionOwner(dpy, cm_atom) == mgr_win);
        manager_ready = true;

        // Process events for manager
        XEvent ev;
        bool save_targets_done = false;
        while (!save_targets_done) {
            XNextEvent(dpy, &ev);
            if (ev.type == SelectionRequest && ev.xselectionrequest.selection == cm_atom) {
                auto* req = &ev.xselectionrequest;
                if (req->target == st_atom) {
                    Window producer_win = XGetSelectionOwner(dpy, cb_atom);

                    // Manager queries TARGETS from producer
                    XConvertSelection(dpy, cb_atom, targets_atom, sel_prop_atom, mgr_win, CurrentTime);
                    XFlush(dpy);

                    XEvent notify_ev;
                    bool got_targets = false;
                    std::vector<Atom> targets_to_save;

                    while (!got_targets) {
                        XNextEvent(dpy, &notify_ev);
                        if (notify_ev.type == SelectionNotify && notify_ev.xselection.target == targets_atom) {
                            if (notify_ev.xselection.property != None) {
                                Atom actual_type;
                                int actual_format;
                                unsigned long nitems, bytes_after;
                                unsigned char* prop = nullptr;
                                XGetWindowProperty(dpy, mgr_win, sel_prop_atom, 0, ~0, True, AnyPropertyType,
                                                   &actual_type, &actual_format, &nitems, &bytes_after, &prop);
                                if (prop) {
                                    Atom* atms = (Atom*)prop;
                                    for (unsigned long i = 0; i < nitems; ++i) {
                                        targets_to_save.push_back(atms[i]);
                                    }
                                    XFree(prop);
                                }
                            }
                            got_targets = true;
                        }
                    }

                    // For each target (e.g. image/png and image/bmp), query format from producer
                    for (Atom t : targets_to_save) {
                        XConvertSelection(dpy, cb_atom, t, sel_prop_atom, mgr_win, CurrentTime);
                        XFlush(dpy);

                        bool got_data = false;
                        while (!got_data) {
                            XNextEvent(dpy, &notify_ev);
                            if (notify_ev.type == SelectionNotify && notify_ev.xselection.target == t) {
                                if (notify_ev.xselection.property != None) {
                                    Atom actual_type;
                                    int actual_format;
                                    unsigned long nitems, bytes_after;
                                    unsigned char* prop = nullptr;
                                    XGetWindowProperty(dpy, mgr_win, sel_prop_atom, 0, ~0, True, AnyPropertyType,
                                                       &actual_type, &actual_format, &nitems, &bytes_after, &prop);
                                    if (prop) {
                                        if (t == png_atom) {
                                            mgr_saved_png.assign(prop, prop + nitems);
                                        } else if (t == bmp_atom) {
                                            mgr_saved_bmp.assign(prop, prop + nitems);
                                        }
                                        XFree(prop);
                                    }
                                }
                                got_data = true;
                            }
                        }
                    }

                    // Respond to SAVE_TARGETS SelectionRequest with success
                    XSelectionEvent resp = {0};
                    resp.type = SelectionNotify;
                    resp.display = req->display;
                    resp.requestor = req->requestor;
                    resp.selection = req->selection;
                    resp.target = req->target;
                    resp.property = req->property;
                    resp.time = req->time;
                    XSendEvent(dpy, req->requestor, False, 0, (XEvent*)&resp);
                    XFlush(dpy);

                    save_targets_done = true;
                }
            }
        }

        manager_success = true;
        while (!manager_done) {
            std::this_thread::sleep_for(std::chrono::milliseconds(10));
        }

        XSetSelectionOwner(dpy, cm_atom, None, CurrentTime);
        XDestroyWindow(dpy, mgr_win);
        XCloseDisplay(dpy);
    });

    // Wait until manager thread is ready
    while (!manager_ready) {
        std::this_thread::sleep_for(std::chrono::milliseconds(5));
    }

    // Thread P (Producer): initialize, persist, and teardown
    {
        struct_array_struct<utf8_string_struct> args;
        args.Alloc(0);
        Application_Init(args);

        P_INSTANCE(WindowHandle) win = CrystalWindow_CreateSimple(100, 100, "Persistence Producer Window");
        assert(win != nullptr);
        s_png_payload = png_expected;
        s_bmp_payload = bmp_expected;
        win->crystal_window->callbacks.on_clipboard_provide_chosen = on_clipboard_provide_cb;

        P_INSTANCE(DataInterchange) copy_data = DataInterchange_Create();
        DataInterchange_FormatAdd(copy_data, "image/png");
        DataInterchange_FormatAdd(copy_data, "image/bmp");

        // Execute persistent copy
        CrystalWindow_ClipboardCopyPersist(win, copy_data);

        DataInterchange_Free(copy_data);

        // Teardown application & window completely (simulating process exit)
        delete TheApplication;
        TheApplication = nullptr;
    }

    // Verify simulated manager received and saved both payloads
    assert(manager_success == true);
    assert(mgr_saved_png.size() == png_expected.size());
    assert(memcmp(mgr_saved_png.data(), png_expected.data(), png_expected.size()) == 0);
    assert(mgr_saved_bmp.size() == bmp_expected.size());
    assert(memcmp(mgr_saved_bmp.data(), bmp_expected.data(), bmp_expected.size()) == 0);

    manager_done = true;
    manager_thread.join();

    std::cout << "[TEST] test_x11_clipboard_persistence_with_manager PASSED." << std::endl;
}

void test_x11_files_advertisement_and_validation() {
    std::cout << "[TEST] Running test_x11_files_advertisement_and_validation..." << std::endl;

    struct_array_struct<utf8_string_struct> args;
    args.Alloc(0);
    Application_Init(args);

    P_INSTANCE(WindowHandle) win = CrystalWindow_CreateSimple(100, 100, "Files Ad Validation Window");
    assert(win != nullptr);
    auto* xwin = static_cast<CrystalWindow_X11*>(win->crystal_window);
    Display* dpy = xwin->display;

    // 1. Files-only DataInterchange
    P_INSTANCE(DataInterchange) di = DataInterchange_Create();
    di->m_handle = win;
    DataInterchange_FormatAdd(di, "text/file-uri");

    // Test ClipboardTargetWasAdvertised for Files
    utf8_string_struct out_fmt = nullptr;
    Atom uri_list_atom = XInternAtom(dpy, "text/uri-list", False);
    Atom plain_atom = XInternAtom(dpy, "text/plain", False);
    Atom utf8_atom = XInternAtom(dpy, "UTF8_STRING", False);
    Atom string_atom = XInternAtom(dpy, "STRING", False);
    Atom text_atom = XInternAtom(dpy, "TEXT", False);
    Atom html_atom = XInternAtom(dpy, "text/html", False);
    Atom png_atom = XInternAtom(dpy, "image/png", False);

    assert(ClipboardTargetWasAdvertised(dpy, di, uri_list_atom, &out_fmt));
    assert(out_fmt != nullptr && strcmp(out_fmt, "text/file-uri") == 0);

    assert(!ClipboardTargetWasAdvertised(dpy, di, plain_atom, &out_fmt));
    assert(!ClipboardTargetWasAdvertised(dpy, di, utf8_atom, &out_fmt));
    assert(!ClipboardTargetWasAdvertised(dpy, di, string_atom, &out_fmt));
    assert(!ClipboardTargetWasAdvertised(dpy, di, text_atom, &out_fmt));
    assert(!ClipboardTargetWasAdvertised(dpy, di, html_atom, &out_fmt));
    assert(!ClipboardTargetWasAdvertised(dpy, di, png_atom, &out_fmt));

    // Test DataImterchange_AtomArrayFromFormats produces ONLY text/uri-list
    Atom* types = nullptr;
    int num_types = 0;
    DataImterchange_AtomArrayFromFormats(di, &types, &num_types);
    assert(num_types == 1);
    assert(types != nullptr);
    assert(types[0] == uri_list_atom);
    delete[] types;

    DataInterchange_Free(di);

    // 2. Text-only DataInterchange
    P_INSTANCE(DataInterchange) di_text = DataInterchange_Create();
    di_text->m_handle = win;
    DataInterchange_FormatAdd(di_text, "text/plain");

    assert(ClipboardTargetWasAdvertised(dpy, di_text, plain_atom, &out_fmt));
    assert(out_fmt != nullptr && strcmp(out_fmt, "text/plain") == 0);
    assert(ClipboardTargetWasAdvertised(dpy, di_text, utf8_atom, &out_fmt));
    assert(out_fmt != nullptr && strcmp(out_fmt, "text/plain") == 0);
    assert(ClipboardTargetWasAdvertised(dpy, di_text, string_atom, &out_fmt));
    assert(out_fmt != nullptr && strcmp(out_fmt, "text/plain") == 0);
    assert(ClipboardTargetWasAdvertised(dpy, di_text, text_atom, &out_fmt));
    assert(out_fmt != nullptr && strcmp(out_fmt, "text/plain") == 0);
    assert(!ClipboardTargetWasAdvertised(dpy, di_text, uri_list_atom, &out_fmt));
    assert(!ClipboardTargetWasAdvertised(dpy, di_text, png_atom, &out_fmt));

    DataInterchange_Free(di_text);

    delete TheApplication;
    TheApplication = nullptr;

    std::cout << "[TEST] test_x11_files_advertisement_and_validation PASSED." << std::endl;
}

static std::atomic<int> s_file_provide_called(0);
static std::atomic<int> s_text_provide_called(0);
static std::string s_last_provided_format;

static void on_files_provide_cb(P_INSTANCE(WindowHandle) handle, P_INSTANCE(DataInterchange) data, utf8_string_struct format) {
    if (format && strcmp(format, "text/file-uri") == 0) {
        s_file_provide_called++;
        s_last_provided_format = format.c_str;
        const char* payload = "/home/jwc/Edge.txt\n";
        DataInterchange_SelectionSet(data, format, (void*)payload, strlen(payload));
    } else if (format && strcmp(format, "text/plain") == 0) {
        s_text_provide_called++;
        s_last_provided_format = format.c_str;
    }
}

void test_x11_files_selection_request_allowed_and_rejected() {
    std::cout << "[TEST] Running test_x11_files_selection_request_allowed_and_rejected..." << std::endl;

    struct_array_struct<utf8_string_struct> args;
    args.Alloc(0);
    Application_Init(args);

    P_INSTANCE(WindowHandle) win = CrystalWindow_CreateSimple(100, 100, "Files Request Test Window");
    assert(win != nullptr);
    win->crystal_window->callbacks.on_clipboard_provide_chosen = on_files_provide_cb;

    s_file_provide_called = 0;
    s_text_provide_called = 0;
    s_last_provided_format.clear();

    // Copy text/file-uri to clipboard
    P_INSTANCE(DataInterchange) copy_data = DataInterchange_Create();
    DataInterchange_FormatAdd(copy_data, "text/file-uri");
    CrystalWindow_ClipboardCopy(win, copy_data);

    auto* xwin = static_cast<CrystalWindow_X11*>(win->crystal_window);
    Display* dpy = xwin->display;
    Atom cb_atom = AppX11->atoms.clipboard;
    Atom targets_atom = AppX11->atoms.targets;
    Atom uri_list_atom = XInternAtom(dpy, "text/uri-list", False);
    Atom plain_atom = XInternAtom(dpy, "text/plain", False);
    Atom utf8_atom = XInternAtom(dpy, "UTF8_STRING", False);
    Atom string_atom = XInternAtom(dpy, "STRING", False);
    Atom text_atom = XInternAtom(dpy, "TEXT", False);
    Atom prop_atom = XInternAtom(dpy, "TEST_CLIENT_PROP", False);

    // Create a client window to request selections
    int screen = DefaultScreen(dpy);
    Window root = RootWindow(dpy, screen);
    Window client_win = XCreateSimpleWindow(dpy, root, -100, -100, 10, 10, 0, 0, 0);

    // Helper lambda to send XConvertSelection and wait for SelectionNotify
    auto request_and_get_notify = [&](Atom target) -> XSelectionEvent {
        XConvertSelection(dpy, cb_atom, target, prop_atom, client_win, CurrentTime);
        XFlush(dpy);

        XEvent ev;
        while (true) {
            XNextEvent(dpy, &ev);
            // Dispatch to application so window handles SelectionRequest
            static_cast<CrystalApplication_X11*>(TheApplication)->DispatchEvent(ev);
            if (ev.type == SelectionNotify && ev.xselection.requestor == client_win && ev.xselection.target == target) {
                return ev.xselection;
            }
        }
    };

    // 1. Request TARGETS -> should succeed and contain only text/uri-list
    {
        XSelectionEvent notify = request_and_get_notify(targets_atom);
        assert(notify.property != None);

        Atom actual_type;
        int actual_format;
        unsigned long nitems, bytes_after;
        unsigned char* prop = nullptr;
        XGetWindowProperty(dpy, client_win, prop_atom, 0, ~0, True, AnyPropertyType,
                           &actual_type, &actual_format, &nitems, &bytes_after, &prop);
        assert(prop != nullptr);
        assert(actual_type == XA_ATOM);
        assert(nitems == 1);
        Atom* atms = (Atom*)prop;
        assert(atms[0] == uri_list_atom);
        XFree(prop);
    }

    // 2. Request text/uri-list -> should succeed, invoke provider, and return cleaned uri list
    {
        int initial_calls = s_file_provide_called.load();
        XSelectionEvent notify = request_and_get_notify(uri_list_atom);
        assert(notify.property != None);
        assert(s_file_provide_called.load() == initial_calls + 1);

        Atom actual_type;
        int actual_format;
        unsigned long nitems, bytes_after;
        unsigned char* prop = nullptr;
        XGetWindowProperty(dpy, client_win, prop_atom, 0, ~0, True, AnyPropertyType,
                           &actual_type, &actual_format, &nitems, &bytes_after, &prop);
        assert(prop != nullptr);
        std::string returned_data((char*)prop, nitems);
        assert(returned_data.find("file:///home/jwc/Edge.txt") != std::string::npos);
        XFree(prop);
    }

    // 3. Request text/plain -> should be REJECTED (property == None) and provider NOT called
    {
        int file_calls = s_file_provide_called.load();
        int text_calls = s_text_provide_called.load();
        XSelectionEvent notify = request_and_get_notify(plain_atom);
        assert(notify.property == None);
        assert(s_file_provide_called.load() == file_calls);
        assert(s_text_provide_called.load() == text_calls);
    }

    // 4. Request UTF8_STRING -> should be REJECTED (property == None) and provider NOT called
    {
        int file_calls = s_file_provide_called.load();
        int text_calls = s_text_provide_called.load();
        XSelectionEvent notify = request_and_get_notify(utf8_atom);
        assert(notify.property == None);
        assert(s_file_provide_called.load() == file_calls);
        assert(s_text_provide_called.load() == text_calls);
    }

    // 5. Request STRING -> should be REJECTED (property == None)
    {
        XSelectionEvent notify = request_and_get_notify(string_atom);
        assert(notify.property == None);
    }

    // 6. Request TEXT -> should be REJECTED (property == None)
    {
        XSelectionEvent notify = request_and_get_notify(text_atom);
        assert(notify.property == None);
    }

    DataInterchange_Free(copy_data);
    XDestroyWindow(dpy, client_win);

    delete TheApplication;
    TheApplication = nullptr;

    std::cout << "[TEST] test_x11_files_selection_request_allowed_and_rejected PASSED." << std::endl;
}

void test_x11_files_clipboard_persistence_with_manager() {
    std::cout << "[TEST] Running test_x11_files_clipboard_persistence_with_manager..." << std::endl;

    std::atomic<bool> manager_ready(false);
    std::atomic<bool> manager_done(false);
    std::atomic<bool> manager_success(false);
    std::string mgr_saved_uri_list;
    bool mgr_saw_plain_text = false;

    // Spawn Thread M: Simulated Clipboard Manager
    std::thread manager_thread([&]() {
        Display* dpy = XOpenDisplay(nullptr);
        if (!dpy) return;

        Atom cm_atom = XInternAtom(dpy, "CLIPBOARD_MANAGER", False);
        Atom cb_atom = XInternAtom(dpy, "CLIPBOARD", False);
        Atom st_atom = XInternAtom(dpy, "SAVE_TARGETS", False);
        Atom targets_atom = XInternAtom(dpy, "TARGETS", False);
        Atom sel_prop_atom = XInternAtom(dpy, "MGR_SAVED_DATA", False);
        Atom uri_list_atom = XInternAtom(dpy, "text/uri-list", False);
        Atom plain_atom = XInternAtom(dpy, "text/plain", False);

        int screen = DefaultScreen(dpy);
        Window root = RootWindow(dpy, screen);
        Window mgr_win = XCreateSimpleWindow(dpy, root, -100, -100, 10, 10, 0, 0, 0);

        XSelectInput(dpy, mgr_win, PropertyChangeMask);
        XSetSelectionOwner(dpy, cm_atom, mgr_win, CurrentTime);
        XFlush(dpy);

        assert(XGetSelectionOwner(dpy, cm_atom) == mgr_win);
        manager_ready = true;

        // Process events for manager
        XEvent ev;
        bool save_targets_done = false;
        while (!save_targets_done) {
            XNextEvent(dpy, &ev);
            if (ev.type == SelectionRequest && ev.xselectionrequest.selection == cm_atom) {
                auto* req = &ev.xselectionrequest;
                if (req->target == st_atom) {
                    Window producer_win = XGetSelectionOwner(dpy, cb_atom);

                    // Manager queries TARGETS from producer
                    XConvertSelection(dpy, cb_atom, targets_atom, sel_prop_atom, mgr_win, CurrentTime);
                    XFlush(dpy);

                    XEvent notify_ev;
                    bool got_targets = false;
                    std::vector<Atom> targets_to_save;

                    while (!got_targets) {
                        XNextEvent(dpy, &notify_ev);
                        if (notify_ev.type == SelectionNotify && notify_ev.xselection.target == targets_atom) {
                            if (notify_ev.xselection.property != None) {
                                Atom actual_type;
                                int actual_format;
                                unsigned long nitems, bytes_after;
                                unsigned char* prop = nullptr;
                                XGetWindowProperty(dpy, mgr_win, sel_prop_atom, 0, ~0, True, AnyPropertyType,
                                                   &actual_type, &actual_format, &nitems, &bytes_after, &prop);
                                if (prop) {
                                    Atom* atms = (Atom*)prop;
                                    for (unsigned long i = 0; i < nitems; ++i) {
                                        targets_to_save.push_back(atms[i]);
                                    }
                                    XFree(prop);
                                }
                            }
                            got_targets = true;
                        }
                    }

                    // Verify TARGETS contains only text/uri-list
                    assert(targets_to_save.size() == 1);
                    assert(targets_to_save[0] == uri_list_atom);

                    // Manager probes text/plain to see if producer erroneously answers
                    {
                        XConvertSelection(dpy, cb_atom, plain_atom, sel_prop_atom, mgr_win, CurrentTime);
                        XFlush(dpy);

                        bool got_reply = false;
                        while (!got_reply) {
                            XNextEvent(dpy, &notify_ev);
                            if (notify_ev.type == SelectionNotify && notify_ev.xselection.target == plain_atom) {
                                if (notify_ev.xselection.property != None) {
                                    mgr_saw_plain_text = true;
                                    XDeleteProperty(dpy, mgr_win, sel_prop_atom);
                                }
                                got_reply = true;
                            }
                        }
                    }

                    // Manager queries text/uri-list
                    {
                        XConvertSelection(dpy, cb_atom, uri_list_atom, sel_prop_atom, mgr_win, CurrentTime);
                        XFlush(dpy);

                        bool got_data = false;
                        while (!got_data) {
                            XNextEvent(dpy, &notify_ev);
                            if (notify_ev.type == SelectionNotify && notify_ev.xselection.target == uri_list_atom) {
                                if (notify_ev.xselection.property != None) {
                                    Atom actual_type;
                                    int actual_format;
                                    unsigned long nitems, bytes_after;
                                    unsigned char* prop = nullptr;
                                    XGetWindowProperty(dpy, mgr_win, sel_prop_atom, 0, ~0, True, AnyPropertyType,
                                                       &actual_type, &actual_format, &nitems, &bytes_after, &prop);
                                    if (prop) {
                                        mgr_saved_uri_list.assign((char*)prop, nitems);
                                        XFree(prop);
                                    }
                                }
                                got_data = true;
                            }
                        }
                    }

                    // Respond to SAVE_TARGETS SelectionRequest with success
                    XSelectionEvent resp = {0};
                    resp.type = SelectionNotify;
                    resp.display = req->display;
                    resp.requestor = req->requestor;
                    resp.selection = req->selection;
                    resp.target = req->target;
                    resp.property = req->property;
                    resp.time = req->time;
                    XSendEvent(dpy, req->requestor, False, 0, (XEvent*)&resp);
                    XFlush(dpy);

                    save_targets_done = true;
                }
            }
        }

        manager_success = true;
        while (!manager_done) {
            std::this_thread::sleep_for(std::chrono::milliseconds(10));
        }

        XSetSelectionOwner(dpy, cm_atom, None, CurrentTime);
        XDestroyWindow(dpy, mgr_win);
        XCloseDisplay(dpy);
    });

    // Wait until manager thread is ready
    while (!manager_ready) {
        std::this_thread::sleep_for(std::chrono::milliseconds(5));
    }

    // Thread P (Producer): initialize, persist, and teardown
    {
        struct_array_struct<utf8_string_struct> args;
        args.Alloc(0);
        Application_Init(args);

        P_INSTANCE(WindowHandle) win = CrystalWindow_CreateSimple(100, 100, "Persistence Producer Window");
        assert(win != nullptr);
        win->crystal_window->callbacks.on_clipboard_provide_chosen = on_files_provide_cb;

        P_INSTANCE(DataInterchange) copy_data = DataInterchange_Create();
        DataInterchange_FormatAdd(copy_data, "text/file-uri");

        // Execute persistent copy
        CrystalWindow_ClipboardCopyPersist(win, copy_data);

        DataInterchange_Free(copy_data);

        // Teardown application & window completely (simulating process exit)
        delete TheApplication;
        TheApplication = nullptr;
    }

    // Verify simulated manager results
    assert(manager_success == true);
    assert(!mgr_saw_plain_text);
    assert(mgr_saved_uri_list.find("file:///home/jwc/Edge.txt") != std::string::npos);

    manager_done = true;
    manager_thread.join();

    std::cout << "[TEST] test_x11_files_clipboard_persistence_with_manager PASSED." << std::endl;
}

void test_native_file_uri_helpers() {
    std::cout << "[TEST] Running test_native_file_uri_helpers..." << std::endl;

    // 1. Plain path roundtrip
    {
        std::string path = "/home/test/file.txt";
        std::string uri = LocalPathToFileUri(path);
        assert(uri == "file:///home/test/file.txt");
        std::string decoded;
        assert(FileUriToLocalPath(uri, decoded));
        assert(decoded == path);
    }

    // 2. Spaces
    {
        std::string path = "/home/test/a b.txt";
        std::string uri = LocalPathToFileUri(path);
        assert(uri == "file:///home/test/a%20b.txt");
        std::string decoded;
        assert(FileUriToLocalPath(uri, decoded));
        assert(decoded == path);
    }

    // 3. Reserved characters (#, %)
    {
        std::string path = "/tmp/clip flow # %.txt";
        std::string uri = LocalPathToFileUri(path);
        assert(uri == "file:///tmp/clip%20flow%20%23%20%25.txt");
        std::string decoded;
        assert(FileUriToLocalPath(uri, decoded));
        assert(decoded == path);
    }

    // 4. UTF-8 filename
    {
        std::string path = "/tmp/ünicode.txt";
        std::string uri = LocalPathToFileUri(path);
        assert(uri == "file:///tmp/%C3%BCnicode.txt");
        std::string decoded;
        assert(FileUriToLocalPath(uri, decoded));
        assert(decoded == path);
    }

    // 5. Multiple URI-list entries, CRLF and line splitting
    {
        std::string local_paths = "/home/test/one.txt\n/home/test/two.txt\n";
        std::string uri_list = LocalPathsToUriList(local_paths);
        assert(uri_list == "file:///home/test/one.txt\r\nfile:///home/test/two.txt\r\n");

        std::string roundtrip_paths = UriListToLocalPaths(uri_list);
        assert(roundtrip_paths == "/home/test/one.txt\n/home/test/two.txt\n");
    }

    // 6. Comments in URI-list
    {
        std::string uri_list_with_comments = "# A comment line\r\nfile:///home/test/file.txt\r\n# Another comment\r\n";
        std::string decoded_paths = UriListToLocalPaths(uri_list_with_comments);
        assert(decoded_paths == "/home/test/file.txt\n");
    }

    // 7. localhost normalization
    {
        std::string localhost_uri = "file://localhost/home/test/file.txt";
        std::string decoded_path;
        assert(FileUriToLocalPath(localhost_uri, decoded_path));
        assert(decoded_path == "/home/test/file.txt");
    }

    // 8. Rejection of remote hosts and non-file schemes
    {
        std::string remote_uri = "file://remotehost/share/file.txt";
        std::string decoded_path;
        assert(!FileUriToLocalPath(remote_uri, decoded_path));

        std::string http_uri = "http://example.com/file.txt";
        assert(!FileUriToLocalPath(http_uri, decoded_path));

        std::string raw_path = "/home/test/file.txt";
        assert(!FileUriToLocalPath(raw_path, decoded_path));
    }

    // 9. Do not double-encode existing file:// URI
    {
        std::string existing_uri = "file:///home/test/a%20b.txt";
        std::string re_encoded = LocalPathToFileUri(existing_uri);
        assert(re_encoded == existing_uri);
    }

    std::cout << "[TEST] test_native_file_uri_helpers PASSED." << std::endl;
}

#endif

int main() {
    std::cout << "=== Running CrystalCatalyst DataInterchange Image Tests ===" << std::endl;
    test_common_data_interchange_formats();
    test_bmp_dib_conversion_invariants();
#if defined(__linux__)
    test_native_file_uri_helpers();
    test_x11_atom_format_mapping_and_aliases();
    test_x11_clipboard_image_roundtrip();
    test_x11_bmp_alias_roundtrip();
    test_x11_clipboard_persistence_without_manager();
    test_x11_clipboard_persistence_with_manager();
    test_x11_files_advertisement_and_validation();
    test_x11_files_selection_request_allowed_and_rejected();
    test_x11_files_clipboard_persistence_with_manager();
#endif
    std::cout << "=== ALL DATA INTERCHANGE TESTS PASSED ===" << std::endl;
    return 0;
}
