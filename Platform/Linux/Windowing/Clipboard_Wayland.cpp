#include "Clipboard_Wayland.h"
#include "FileUri_Linux.h"

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
            std::string uri_list = LocalPathsToUriList((const char*)d, sz);
            fwrite(uri_list.c_str(), 1, uri_list.size(), pipe);
        } else {
            fwrite(d, 1, sz, pipe);
        }
        int status = pclose(pipe);

        return (status == 0);
    }

    bool Clipboard_Wayland::Paste(P_INSTANCE(WindowHandle) handle, P_INSTANCE(DataInterchange) data) {
        if (!IsAvailable() || !data) return false;
        FILE* pipe = popen("wl-paste --list-types 2>/dev/null", "r");
        if (!pipe) return false;
        char buf[256];
        std::vector<std::string> raw_types;
        bool has_uri_list = false;
        while (fgets(buf, sizeof(buf), pipe) != nullptr) {
            std::string line(buf);
            while (!line.empty() && (line.back() == '\n' || line.back() == '\r')) line.pop_back();
            if (line.empty()) continue;
            if (line == "text/uri-list") {
                has_uri_list = true;
            }
            raw_types.push_back(line);
        }
        int status = pclose(pipe);
        if (status != 0) return false;

        bool anyAdded = false;
        for (const auto& line : raw_types) {
            if (line == "text/uri-list") {
                if (!DataInterchange_FormatExists(data, "text/file-uri")) {
                    DataInterchange_FormatAdd(data, "text/file-uri");
                    anyAdded = true;
                }
            } else if (line == "image/x-bmp" || line == "image/x-MS-bmp") {
                if (!DataInterchange_FormatExists(data, "image/bmp")) {
                    DataInterchange_FormatAdd(data, "image/bmp");
                    anyAdded = true;
                }
            } else if (has_uri_list && (line == "text/plain" || line == "text/plain;charset=utf-8" ||
                                        line == "UTF8_STRING" || line == "STRING" || line == "TEXT")) {
                // When text/uri-list is present, ignore plain-text fallback types synthesized by wl-copy
                continue;
            } else {
                if (!DataInterchange_FormatExists(data, (utf8_string_struct)line.c_str())) {
                    DataInterchange_FormatAdd(data, (utf8_string_struct)line.c_str());
                    anyAdded = true;
                }
            }
        }
        return anyAdded;
    }

    bool Clipboard_Wayland::Select(P_INSTANCE(DataInterchange) data, utf8_string_struct format) {
        const char* fmt_str = (const char*)format;
        if (!IsAvailable() || !data || !fmt_str || fmt_str[0] == '\0') return false;

        std::vector<std::string> candidates;
        std::string req(fmt_str);
        if (req == "text/file-uri") {
            candidates.push_back("text/uri-list");
        } else if (req == "image/bmp") {
            candidates.push_back("image/bmp");
            candidates.push_back("image/x-bmp");
            candidates.push_back("image/x-MS-bmp");
        } else if (req == "text/plain") {
            candidates.push_back("text/plain");
            candidates.push_back("text/plain;charset=utf-8");
            candidates.push_back("UTF8_STRING");
            candidates.push_back("STRING");
            candidates.push_back("TEXT");
        } else if (req == "text/html") {
            candidates.push_back("text/html");
        } else {
            candidates.push_back(req);
        }

        std::vector<uint8_t> buffer;
        for (const auto& mime : candidates) {
            std::string cmd = "wl-paste --no-newline --type " + mime + " 2>/dev/null";
            FILE* pipe = popen(cmd.c_str(), "r");
            if (!pipe) continue;

            buffer.clear();
            char chunk[512];
            size_t n;
            while ((n = fread(chunk, 1, sizeof(chunk), pipe)) > 0) {
                buffer.insert(buffer.end(), chunk, chunk + n);
            }
            int status = pclose(pipe);
            if (status == 0 && !buffer.empty()) {
                break;
            }
        }

        if (!buffer.empty()) {
            if (strcmp(fmt_str, "text/file-uri") == 0) {
                std::string local_paths = UriListToLocalPaths((const char*)buffer.data(), buffer.size());
                DataInterchange_SelectionSet(data, format, (void*)local_paths.data(), local_paths.size());
            } else {
                DataInterchange_SelectionSet(data, format, buffer.data(), buffer.size());
            }
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
