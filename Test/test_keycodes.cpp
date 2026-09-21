#include "CrystalCatalystLibrary/CrystalCatalystLibrary.h"
#include "../Platform/Windows/Windowing/KeyCode_Windows.h"
#include "keycode_contract.h"
#include <cassert>
#include <cstring>
#include <iostream>
#include <map>
#include <regex>
#include <string>
#include <vector>

#if defined(__linux__)
#include "../Platform/Linux/Windowing/KeyCode_X11.h"
#include "../Platform/Linux/Windowing/CrystalWindow_X11.h"
#elif defined(_WIN32)
#include "../Platform/Windows/Windowing/CrystalWindow_Windows.h"
#endif

using namespace NewAge;

static std::map<std::string, int32_t> managedKeys() {
    const std::string source = ManagedKeyCodeContract;
    const std::regex member(R"((\w+)\s*=\s*(0x[0-9a-fA-F]+))");
    std::map<std::string, int32_t> result;
    for (std::sregex_iterator i(source.begin(), source.end(), member), end; i != end; ++i)
        result[(*i)[1]] = std::stoi((*i)[2], nullptr, 16);
    assert(result.at("Escape") == 0xFF1B);
    return result;
}

static void testWindowsMapping(const std::map<std::string, int32_t>& keys) {
    struct NamedKey { uint32_t vk; const char* name; };
    const NamedKey named[] = {
        {0x08, "BackSpace"}, {0x09, "Tab"}, {0x0C, "Clear"},
        {0x0D, "Return"}, {0x13, "Pause"}, {0x14, "CapsLock"},
        {0x1B, "Escape"}, {0x21, "PageUp"}, {0x22, "PageDown"},
        {0x23, "End"}, {0x24, "Home"}, {0x25, "Left"}, {0x26, "Up"},
        {0x27, "Right"}, {0x28, "Down"}, {0x2A, "Print"}, {0x2C, "Print"},
        {0x2D, "Insert"}, {0x2E, "Delete"}, {0x5B, "Super_Left"},
        {0x5C, "Super_Right"}, {0x5D, "Menu"}, {0x6A, "KP_Multiply"},
        {0x6B, "KP_Add"}, {0x6C, "KP_Separator"}, {0x6D, "KP_Subtract"},
        {0x6E, "KP_Decimal"}, {0x6F, "KP_Divide"}, {0x90, "NumLock"},
        {0x91, "ScrollLock"}, {0xA0, "Shift_Left"}, {0xA1, "Shift_Right"},
        {0xA2, "Control_Left"}, {0xA3, "Control_Right"},
        {0xA4, "Alt_Left"}, {0xA5, "Alt_Right"}
    };
    for (const auto& key : named)
        assert(TranslateWindowsNamedKey(key.vk, 0, false) == keys.at(key.name));
    for (uint32_t i = 0; i < 24; ++i)
        assert(TranslateWindowsNamedKey(0x70 + i, 0, false) == keys.at("F" + std::to_string(i + 1)));
    for (uint32_t i = 0; i < 10; ++i)
        assert(TranslateWindowsNamedKey(0x60 + i, 0, false) == keys.at("KP_" + std::to_string(i)));

    struct Navigation { uint32_t vk, scan; const char* name; };
    const Navigation navigation[] = {
        {0x21, 0x49, "PageUp"}, {0x22, 0x51, "PageDown"},
        {0x23, 0x4F, "End"}, {0x24, 0x47, "Home"},
        {0x25, 0x4B, "Left"}, {0x26, 0x48, "Up"},
        {0x27, 0x4D, "Right"}, {0x28, 0x50, "Down"},
        {0x2D, 0x52, "Insert"}, {0x2E, 0x53, "Delete"}
    };
    for (const auto& key : navigation) {
        assert(TranslateWindowsNamedKey(key.vk, key.scan, true) == keys.at(key.name));
        assert(TranslateWindowsNamedKey(key.vk, key.scan, false) == keys.at(std::string("KP_") + key.name));
    }
    assert(TranslateWindowsNamedKey(0x0C, 0x4C, false) == keys.at("KP_Begin"));
    assert(TranslateWindowsNamedKey(0x0D, 0x1C, true) == keys.at("KP_Enter"));
    assert(TranslateWindowsNamedKey(0x10, 0x2A, false) == keys.at("Shift_Left"));
    assert(TranslateWindowsNamedKey(0x10, 0x36, false) == keys.at("Shift_Right"));
    assert(TranslateWindowsNamedKey(0x11, 0x1D, false) == keys.at("Control_Left"));
    assert(TranslateWindowsNamedKey(0x11, 0x1D, true) == keys.at("Control_Right"));
    assert(TranslateWindowsNamedKey(0x12, 0x38, false) == keys.at("Alt_Left"));
    assert(TranslateWindowsNamedKey(0x12, 0x38, true) == keys.at("Alt_Right"));
    // Printable keys and unrecognized keys must reach the existing fallback,
    // not accidentally become navigation keys with overlapping numeric values.
    for (uint32_t vk : {0x20U, 0x31U, 0x41U, 0x52U, 0x53U, 0xBAU, 0xFFU})
        assert(TranslateWindowsNamedKey(vk, 0, false) == 0);
}

static std::vector<int32_t> pressed, released;
static void onDown(WindowHandle*, int32_t key) { pressed.push_back(key); }
static void onUp(WindowHandle*, int32_t key) { released.push_back(key); }

static void testEvents(const std::map<std::string, int32_t>& keys) {
    struct_array_struct<utf8_string_struct> args;
    args.Alloc(0);
    Application_Init(args);
    auto* handle = CrystalWindow_CreateSimple(100, 100, "Keycode regression");
    assert(handle && handle->crystal_window);
    handle->crystal_window->callbacks.on_key_down = onDown;
    handle->crystal_window->callbacks.on_key_up = onUp;

#if defined(__linux__)
    auto* window = static_cast<CrystalWindow_X11*>(handle->crystal_window);
    auto send = [&](KeySym symbol, unsigned int state, int32_t expected) {
        pressed.clear(); released.clear();
        XEvent event = {};
        event.xkey.display = window->display;
        event.xkey.window = window->window;
        event.xkey.keycode = XKeysymToKeycode(window->display, symbol);
        assert(event.xkey.keycode != 0);
        event.xkey.state = state;
        event.type = KeyPress;
        assert(window->handle_xevent(&event));
        event.type = KeyRelease;
        assert(window->handle_xevent(&event));
        assert(pressed == std::vector<int32_t>{expected});
        assert(released == pressed);
    };
    send(XK_Escape, 0, keys.at("Escape"));
    send(XK_Return, 0, keys.at("Return"));
    send(XK_BackSpace, 0, keys.at("BackSpace"));
    send(XK_Left, 0, keys.at("Left"));
    send(XK_F1, 0, keys.at("F1"));
    send(XK_Shift_L, 0, keys.at("Shift_Left"));
    send(XK_Shift_R, 0, keys.at("Shift_Right"));
    send(XK_Control_R, 0, keys.at("Control_Right"));
    send(XK_Alt_L, 0, keys.at("Alt_Left"));
    send(XK_Tab, 0, keys.at("Tab"));
    send(XK_Tab, ShiftMask, keys.at("Tab"));
    send(XK_KP_Enter, 0, keys.at("KP_Enter"));
    send(XK_s, 0, 's');
    send(XK_s, ShiftMask, 'S');
    send(XK_r, 0, 'r');
    send(XK_1, 0, '1');
#elif defined(_WIN32)
    auto* window = static_cast<CrystalWindow_Windows*>(handle->crystal_window);
    // Give printable-key assertions a known layout on international hosts.
    const HKL testLayout = LoadKeyboardLayoutW(L"00000409", 0);
    assert(testLayout);
    const HKL originalLayout = ActivateKeyboardLayout(testLayout, 0);
    assert(originalLayout);
    BYTE originalState[256];
    assert(GetKeyboardState(originalState));
    auto send = [&](UINT vk, unsigned scan, bool extended, int32_t expected, bool system = false, bool shift = false) {
        pressed.clear(); released.clear();
        BYTE state[256] = {};
        if (shift) state[VK_SHIFT] = 0x80;
        assert(SetKeyboardState(state));
        LPARAM info = 1 | (static_cast<LPARAM>(scan) << 16) | (static_cast<LPARAM>(extended) << 24);
        if (system) info |= static_cast<LPARAM>(1) << 29;
        SendMessage(window->hwnd, system ? WM_SYSKEYDOWN : WM_KEYDOWN, vk, info);
        info |= static_cast<LPARAM>(static_cast<uintptr_t>(3) << 30);
        SendMessage(window->hwnd, system ? WM_SYSKEYUP : WM_KEYUP, vk, info);
        assert(pressed == std::vector<int32_t>{expected});
        assert(released == pressed);
    };
    send(VK_ESCAPE, 1, false, keys.at("Escape"));
    send(VK_RETURN, 0x1C, false, keys.at("Return"));
    send(VK_RETURN, 0x1C, true, keys.at("KP_Enter"));
    send(VK_LEFT, 0x4B, true, keys.at("Left"));
    send(VK_LEFT, 0x4B, false, keys.at("KP_Left"));
    send(VK_SHIFT, 0x2A, false, keys.at("Shift_Left"));
    send(VK_SHIFT, 0x36, false, keys.at("Shift_Right"));
    send(VK_CONTROL, 0x1D, true, keys.at("Control_Right"));
    send(VK_MENU, 0x38, false, keys.at("Alt_Left"), true);
    send(VK_MENU, 0x38, true, keys.at("Alt_Right"), true);
    send(VK_F6, 0x40, false, keys.at("F6"), true);
    send(VK_TAB, 0x0F, false, keys.at("Tab"), false, true);
    send('S', 0x1F, false, 's');
    send('S', 0x1F, false, 'S', false, true);
    send('R', 0x13, false, 'r');
    send('1', 0x02, false, '1');
    assert(SetKeyboardState(originalState));
    assert(ActivateKeyboardLayout(originalLayout, 0));
#endif
    delete TheApplication;
    TheApplication = nullptr;
}

int main(int argc, char** argv) {
    auto keys = managedKeys();
    testWindowsMapping(keys);
#if defined(__linux__)
    assert(NormalizeX11KeySym(XK_Escape) == keys.at("Escape"));
    assert(NormalizeX11KeySym(XK_ISO_Left_Tab) == keys.at("Tab"));
    assert(NormalizeX11KeySym(NoSymbol) == 0);
    assert(NormalizeX11KeySym(0x0101F600) == 0x0101F600); // Unicode-encoded keysym
    assert(NormalizeX11KeySym(XK_Greek_alpha) == XK_Greek_alpha); // Legacy keysym
#endif
    if (argc != 2 || std::strcmp(argv[1], "--mapping-only") != 0) testEvents(keys);
    std::cout << "Keycode regressions passed\n";
}
