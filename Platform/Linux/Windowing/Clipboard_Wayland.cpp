#include "Clipboard_Wayland.h"

#include <iostream>
#include <sstream>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <stdexcept>
#include <vector>

#include "CrystalWindow_X11.h"

namespace NewAge {

    void Clipboard_Wayland::copyToClipboard() {
        std::string command = "wl-copy";
        FILE* pipe = popen(command.c_str(), "w");
        if (!pipe) {
            throw std::runtime_error("popen() failed!");
        }
        if (!selected_data.empty()) {
            fwrite(selected_data.data(), sizeof(uint8_t), selected_data.size(), pipe);
        }
        pclose(pipe);
    }

    void Clipboard_Wayland::pasteFromClipboard() {
        char buffer[512];
        std::vector<uint8_t> result;
        std::string command = "wl-paste";

        FILE* pipe = popen(command.c_str(), "r");
        if (!pipe) {
            throw std::runtime_error("popen() failed!");
        }
        size_t bytes_read = 0;
        while ((bytes_read = fread(buffer, 1, sizeof(buffer), pipe)) > 0) {
            result.insert(result.end(), buffer, buffer + bytes_read);
        }
        pclose(pipe);

        selected_data = std::move(result);
    }

    bool Clipboard_Wayland::isClipboardDataAvailable() const {
        std::string command = "wl-paste --no-newline";
        FILE* pipe = popen(command.c_str(), "r");
        if (!pipe) {
            return false;
        }
        char buffer[128];
        bool hasData = (fgets(buffer, sizeof(buffer), pipe) != nullptr);
        pclose(pipe);
        return hasData;
    }

    void Clipboard_Wayland::CrystalWindow_ClipboardCopyPersist() {
        copyToClipboard();
    }

    bool Clipboard_Wayland::IsAvailable() {
        const char* wayland_display = getenv("WAYLAND_DISPLAY");
        if (!wayland_display || wayland_display[0] == '\0') {
            return false;
        }
        static int available = -1;
        if (available == -1) {
            FILE* pipe = popen("which wl-copy 2>/dev/null", "r");
            if (pipe) {
                char buf[64];
                available = (fgets(buf, sizeof(buf), pipe) != nullptr) ? 1 : 0;
                pclose(pipe);
            } else {
                available = 0;
            }
        }
        return available == 1;
    }

    bool Clipboard_Wayland::CopyPersist(P_INSTANCE(WindowHandle) handle, P_INSTANCE(DataInterchange) data) {
        if (!IsAvailable() || !data) return false;

        std::vector<std::string> formats;
        for (P_INSTANCE(DragDropData::Node) node = DataInterchange_FormatEnum(data); node != nullptr; node = DataInterchange_FormatEnumNext(node)) {
            utf8_string_struct ty;
            DataInterchange_FormatEnumText(node, &ty);
            const char* s = (const char*)ty;
            if (s && s[0] != '\0') {
                formats.push_back(s);
            }
        }

        if (formats.empty()) return false;

        std::string target_format;
        for (const auto& fmt : formats) {
            if (fmt == "image/png") { target_format = fmt; break; }
        }
        if (target_format.empty()) {
            for (const auto& fmt : formats) {
                if (fmt == "image/bmp" || fmt == "text/plain" || fmt == "text/html" || fmt == "text/file-uri") {
                    target_format = fmt;
                    break;
                }
            }
        }
        if (target_format.empty()) {
            target_format = formats[0];
        }

        if (data->provide_chosen) {
            data->provide_chosen(data, (utf8_string_struct)target_format.c_str());
        }

        P_INSTANCE(void) d = nullptr;
        size_t sz = 0;
        DataInterchange_SelectionReveal(data, nullptr, &d, &sz);
        if (!d || sz == 0) return false;

        std::string mime_type = target_format;
        if (target_format == "text/file-uri") {
            mime_type = "text/uri-list";
        }

        std::string cmd = "wl-copy --type " + mime_type;
        FILE* pipe = popen(cmd.c_str(), "w");
        if (!pipe) return false;

        if (target_format == "text/file-uri") {
            std::string uris((const char*)d, sz);
            std::string cleaned;
            std::istringstream stream(uris);
            std::string line;
            while (std::getline(stream, line)) {
                if (!line.empty() && line.back() == '\r') line.pop_back();
                if (!line.empty()) {
                    cleaned += line + "\r\n";
                }
            }
            fwrite(cleaned.c_str(), 1, cleaned.size(), pipe);
        } else {
            fwrite(d, 1, sz, pipe);
        }
        pclose(pipe);

        return true;
    }

    bool Clipboard_Wayland::Paste(P_INSTANCE(WindowHandle) handle, P_INSTANCE(DataInterchange) data) {
        if (!IsAvailable() || !data) return false;
        FILE* pipe = popen("wl-paste --list-types", "r");
        if (!pipe) return false;
        char buf[256];
        while (fgets(buf, sizeof(buf), pipe) != nullptr) {
            std::string line(buf);
            while (!line.empty() && (line.back() == '\n' || line.back() == '\r')) line.pop_back();
            if (line.empty()) continue;
            if (line == "text/uri-list") {
                if (!DataInterchange_FormatExists(data, "text/file-uri"))
                    DataInterchange_FormatAdd(data, "text/file-uri");
            } else if (line == "image/x-bmp" || line == "image/x-MS-bmp") {
                if (!DataInterchange_FormatExists(data, "image/bmp"))
                    DataInterchange_FormatAdd(data, "image/bmp");
            } else {
                if (!DataInterchange_FormatExists(data, (utf8_string_struct)line.c_str()))
                    DataInterchange_FormatAdd(data, (utf8_string_struct)line.c_str());
            }
        }
        pclose(pipe);
        return true;
    }

    bool Clipboard_Wayland::Select(P_INSTANCE(DataInterchange) data, utf8_string_struct format) {
        const char* fmt_str = (const char*)format;
        if (!IsAvailable() || !data || !fmt_str || fmt_str[0] == '\0') return false;
        std::string mime_type = fmt_str;
        if (mime_type == "text/file-uri") {
            mime_type = "text/uri-list";
        }
        std::string cmd = "wl-paste --no-newline --type " + mime_type;
        FILE* pipe = popen(cmd.c_str(), "r");
        if (!pipe) return false;

        std::vector<uint8_t> buffer;
        char chunk[512];
        size_t n;
        while ((n = fread(chunk, 1, sizeof(chunk), pipe)) > 0) {
            buffer.insert(buffer.end(), chunk, chunk + n);
        }
        pclose(pipe);

        if (!buffer.empty()) {
            DataInterchange_SelectionSet(data, format, buffer.data(), buffer.size());
            if (data->m_handle && data->m_handle->crystal_window && data->m_handle->crystal_window->callbacks.on_clipboard_receive_data) {
                data->m_handle->crystal_window->callbacks.on_clipboard_receive_data(data->m_handle, data);
            }
            return true;
        }
        return false;
    }

    bool Clipboard_Wayland::Clear() {
        if (!IsAvailable()) return false;
        FILE* pipe = popen("wl-copy --clear", "w");
        if (pipe) {
            pclose(pipe);
            return true;
        }
        return false;
    }

}
