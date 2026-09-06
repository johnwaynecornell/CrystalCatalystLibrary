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

#endif

int main() {
    std::cout << "=== Running CrystalCatalyst DataInterchange Image Tests ===" << std::endl;
    test_common_data_interchange_formats();
    test_bmp_dib_conversion_invariants();
#if defined(__linux__)
    test_x11_atom_format_mapping_and_aliases();
    test_x11_clipboard_image_roundtrip();
    test_x11_bmp_alias_roundtrip();
#endif
    std::cout << "=== ALL DATA INTERCHANGE TESTS PASSED ===" << std::endl;
    return 0;
}
