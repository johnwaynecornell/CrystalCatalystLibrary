// Native persistence regressions. Linux uses fake wl-clipboard executables so
// routing and subprocess failures can be tested without a compositor.
#include "CrystalCatalystLibrary/CrystalCatalystLibrary.h"
#include <cassert>
#include <cstring>
#include <iostream>
#include <string>
#include <sstream>
#include <chrono>
#include <vector>

using namespace NewAge;
static std::string payload;
static std::string lastError;
static int received = 0;
static void provide(WindowHandle*, DataInterchange* data, utf8_string_struct format) {
    DataInterchange_SelectionSet(data, format, (void*)payload.data(), payload.size());
}
static void error(WindowHandle*, DataInterchange*, utf8_string_struct message) {
    lastError = message.c_str;
}
static void receive(WindowHandle*, DataInterchange*) { ++received; }
static void checkPayload(DataInterchange* data, const std::string& expected) {
    void* bytes = nullptr;
    size_t size = 0;
    DataInterchange_SelectionReveal(data, nullptr, &bytes, &size);
    assert(size == expected.size());
    assert(size == 0 || memcmp(bytes, expected.data(), size) == 0);
}

#if defined(__linux__)
#include "../Platform/Linux/Windowing/CrystalWindow_X11.h"
#include <filesystem>
#include <fstream>
#include <sys/stat.h>
#include <unistd.h>

static Window fakeOwner = None;
static int ownerQueries = 0;
// Any owner/manager query in a Wayland session is a routing regression.
extern "C" Window XGetSelectionOwner(Display*, Atom) {
    ++ownerQueries;
    return fakeOwner;
}
static void script(const std::string& path, const std::string& body) {
    std::ofstream(path) << "#!/bin/sh\n" << body;
    assert(chmod(path.c_str(), 0700) == 0);
}
static void testRouting() {
    char directory[] = "/tmp/crystal-clipboard-test-XXXXXX";
    assert(mkdtemp(directory));
    std::string dir = directory;
    setenv("PATH", dir.c_str(), 1);
    setenv("DISPLAY", ":fake-xwayland", 1);
    setenv("WAYLAND_DISPLAY", "fake-wayland", 1);
    setenv("CLIPBOARD_TEST_DIR", dir.c_str(), 1);
    script(dir + "/wl-copy", R"SH(
if [ "$1" = --clear ]; then
    : > "$CLIPBOARD_TEST_DIR/payload"
    exit 0
fi
printf '%s\n' "$2" > "$CLIPBOARD_TEST_DIR/type"
/bin/cat > "$CLIPBOARD_TEST_DIR/payload"
)SH");
    script(dir + "/wl-paste", R"SH(
if [ "$1" = --list-types ]; then
    /bin/cat "$CLIPBOARD_TEST_DIR/type"
else
    /bin/cat "$CLIPBOARD_TEST_DIR/payload"
fi
)SH");
    CrystalWindow_X11 window;
    WindowHandle handle{&window};
    window.callbacks.on_clipboard_provide_chosen = provide;
    window.callbacks.on_clipboard_receive_data = receive;
    window.callbacks.on_data_interchange_error = error;
    // Both manager/owner states must route identically, including in XWayland.
    for (Window owner : {Window(None), Window(42)}) {
        fakeOwner = owner;
        for (const std::string& value : {std::string("new clipboard\n"), std::string()}) {
            payload = value;
            auto* copy = DataInterchange_Create();
            DataInterchange_FormatAdd(copy, "text/plain");
            CrystalWindow_ClipboardCopyPersist(&handle, copy);
            assert(lastError.empty());
            auto* paste = CrystalWindow_ClipboardPaste(&handle);
            assert(DataInterchange_FormatExists(paste, "text/plain"));
            int before = received;
            DataInterchange_Select(paste, "text/plain");
            assert(received == before + 1);
            checkPayload(paste, value);
            CrystalWindow_ClipboardCopy(&handle, copy);
            CrystalWindow_ClipboardClear();
            assert(std::filesystem::file_size(dir + "/payload") == 0);
            DataInterchange_Free(paste);
            DataInterchange_Free(copy);
        }
    }
    // File semantics must use text/uri-list on the wire and local paths on return.
    payload = "/tmp/a b#%.txt\n/tmp/\xE6\xBC\xA2.txt\n";
    auto* files = DataInterchange_Create();
    DataInterchange_FormatAdd(files, "text/file-uri");
    CrystalWindow_ClipboardCopyPersist(&handle, files);
    auto* paste = CrystalWindow_ClipboardPaste(&handle);
    assert(DataInterchange_FormatExists(paste, "text/file-uri"));
    assert(!DataInterchange_FormatExists(paste, "text/plain"));
    DataInterchange_Select(paste, "text/file-uri");
    checkPayload(paste, payload);
    DataInterchange_Free(paste);

    // A process that emits data and then fails must never deliver that data.
    script(dir + "/wl-paste", "printf stale\nprintf denied >&2\nexit 7\n");
    int before = received;
    DataInterchange_Select(files, "text/file-uri");
    assert(received == before);
    assert(lastError.find("denied") != std::string::npos);
    lastError.clear();
    paste = CrystalWindow_ClipboardPaste(&handle);
    assert(lastError.find("denied") != std::string::npos);
    assert(DataInterchange_FormatEnum(paste) == nullptr);
    DataInterchange_Free(paste);
    script(dir + "/wl-copy", "printf rejected >&2\nexit 9\n");
    lastError.clear();
    CrystalWindow_ClipboardCopyPersist(&handle, files);
    assert(lastError.find("rejected") != std::string::npos);
    lastError.clear();
    std::filesystem::remove(dir + "/wl-copy");
    CrystalWindow_ClipboardCopyPersist(&handle, files);
    assert(lastError.find("wl-copy") != std::string::npos);
    std::filesystem::remove(dir + "/wl-paste");
    lastError.clear();
    DataInterchange_Select(files, "text/file-uri");
    assert(lastError.find("wl-paste") != std::string::npos);
    assert(received == before);
    script(dir + "/wl-copy", "printf rejected >&2\nexit 9\n");
    std::ostringstream clearError;
    auto* oldErrors = std::cerr.rdbuf(clearError.rdbuf());
    CrystalWindow_ClipboardClear();
    std::cerr.rdbuf(oldErrors);
    assert(clearError.str().find("rejected") != std::string::npos);
    script(dir + "/wl-paste", "printf stale\n/bin/sleep 30\n");
    lastError.clear();
    auto started = std::chrono::steady_clock::now();
    DataInterchange_Select(files, "text/file-uri");
    assert(lastError.find("timed out") != std::string::npos);
    assert(std::chrono::steady_clock::now() - started < std::chrono::seconds(8));
    assert(received == before);
    assert(ownerQueries == 0);
    DataInterchange_Free(files);
    std::filesystem::remove_all(dir);
}
#endif

#if defined(_WIN32)
#include <windows.h>
#include <shellapi.h>
#include <shlobj.h>
static void testWindowsFiles() {
    struct_array_struct<utf8_string_struct> args;
    args.Alloc(0);
    Application_Init(args);
    auto* handle = CrystalWindow_CreateSimple(100, 100, "File clipboard regression");
    handle->crystal_window->callbacks.on_clipboard_provide_chosen = provide;
    handle->crystal_window->callbacks.on_clipboard_receive_data = receive;
    handle->crystal_window->callbacks.on_data_interchange_error = error;
    payload = "C:\\a b#%.txt\r\nC:\\\xE6\xBC\xA2.txt\r\n\\\\server\\share\\folder";
    auto* copy = DataInterchange_Create();
    DataInterchange_FormatAdd(copy, "text/file-uri");
    CrystalWindow_ClipboardCopyPersist(handle, copy);
    assert(lastError.empty());
    assert(OpenClipboard(nullptr));
    assert(IsClipboardFormatAvailable(CF_HDROP));
    assert(!IsClipboardFormatAvailable(CF_UNICODETEXT));
    auto memory = GetClipboardData(CF_HDROP);
    auto* drop = static_cast<DROPFILES*>(GlobalLock(memory));
    assert(drop && drop->pFiles == sizeof(DROPFILES) && drop->fWide);
    const auto* paths = reinterpret_cast<const WCHAR*>(reinterpret_cast<const char*>(drop) + drop->pFiles);
    size_t units = 0;
    for (int i = 0; i < 3; ++i) units += wcslen(paths + units) + 1;
    assert(paths[units] == L'\0');
    GlobalUnlock(memory);
    HDROP hdrop = reinterpret_cast<HDROP>(memory);
    assert(DragQueryFileW(hdrop, 0xffffffff, nullptr, 0) == 3);
    WCHAR first[100];
    assert(DragQueryFileW(hdrop, 0, first, 100));
    assert(std::wstring(first) == L"C:\\a b#%.txt");
    CloseClipboard();
    auto* paste = CrystalWindow_ClipboardPaste(handle);
    assert(DataInterchange_FormatExists(paste, "text/file-uri"));
    DataInterchange_Select(paste, "text/file-uri");
    assert(lastError.empty() && received == 1);
    checkPayload(paste, "C:\\a b#%.txt\nC:\\\xE6\xBC\xA2.txt\n\\\\server\\share\\folder\n");
    // A provider that supplies no new data must fail, even after a successful copy.
    handle->crystal_window->callbacks.on_clipboard_provide_chosen = nullptr;
    CrystalWindow_ClipboardCopyPersist(handle, copy);
    assert(!lastError.empty());
    handle->crystal_window->callbacks.on_clipboard_provide_chosen = [](WindowHandle*, DataInterchange* data, utf8_string_struct) {
        const char text[] = "wrong format";
        DataInterchange_SelectionSet(data, "text/plain", (void*)text, sizeof(text) - 1);
    };
    lastError.clear();
    CrystalWindow_ClipboardCopyPersist(handle, copy);
    assert(!lastError.empty());
    assert(IsClipboardFormatAvailable(CF_HDROP));
    assert(!IsClipboardFormatAvailable(CF_UNICODETEXT));
    // Malformed path bytes must fail before destroying the last valid clipboard.
    auto* invalid = DataInterchange_Create();
    DataInterchange_FormatAdd(invalid, "text/file-uri");
    handle->crystal_window->callbacks.on_clipboard_provide_chosen = provide;
    payload = std::string("C:\\bad") + char(0xff);
    lastError.clear();
    CrystalWindow_ClipboardCopyPersist(handle, invalid);
    assert(!lastError.empty());
    assert(IsClipboardFormatAvailable(CF_HDROP));
    DataInterchange_Free(invalid);
    DataInterchange_Free(paste);
    DataInterchange_Free(copy);
    delete TheApplication;
    TheApplication = nullptr;
}
#endif

int main() {
#if defined(__linux__)
    testRouting();
#elif defined(_WIN32)
    testWindowsFiles();
#endif
    std::cout << "Native clipboard persistence regressions passed\n";
}
