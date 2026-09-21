#include "Clipboard_Wayland.h"
#include "FileUri_Linux.h"

#include <iostream>
#include <sstream>
#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <stdexcept>
#include <vector>
#include <chrono>
#include <cerrno>
#include <csignal>
#include <sys/wait.h>
#include <unistd.h>

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

    // Use files for the subprocess streams: no pipe deadlocks/SIGPIPE, no shell
    // interpretation of MIME types, and no inherited smoke-runner output pipes.
    static bool RunClipboardTool(const std::vector<std::string>& arguments,
                                 const void* input, size_t size,
                                 std::vector<uint8_t>& output, std::string& error) {
        FILE* in = tmpfile();
        FILE* out = tmpfile();
        FILE* err = tmpfile();
        auto closeFiles = [&]() {
            if (in) fclose(in);
            if (out) fclose(out);
            if (err) fclose(err);
        };
        if (!in || !out || !err || (size && fwrite(input, 1, size, in) != size) ||
            (in && fflush(in) != 0)) {
            error = std::string("clipboard subprocess I/O: ") + strerror(errno);
            closeFiles();
            return false;
        }
        rewind(in);
        std::vector<char*> argv;
        for (const auto& arg : arguments) argv.push_back(const_cast<char*>(arg.c_str()));
        argv.push_back(nullptr);
        pid_t pid = fork();
        if (pid == 0) {
            setpgid(0, 0);
            if (dup2(fileno(in), STDIN_FILENO) < 0 ||
                dup2(fileno(out), STDOUT_FILENO) < 0 ||
                dup2(fileno(err), STDERR_FILENO) < 0) _exit(126);
            close(fileno(in));
            close(fileno(out));
            close(fileno(err));
            execvp(argv[0], argv.data());
            _exit(127);
        }
        if (pid < 0) {
            error = std::string("fork: ") + strerror(errno);
            closeFiles();
            return false;
        }
        setpgid(pid, pid);
        int status = 0;
        bool completed = false;
        const auto deadline = std::chrono::steady_clock::now() + std::chrono::seconds(5);
        while (true) {
            pid_t result = waitpid(pid, &status, WNOHANG);
            if (result == pid) { completed = true; break; }
            if (result < 0 && errno != EINTR) break;
            if (std::chrono::steady_clock::now() >= deadline) {
                kill(-pid, SIGKILL);
                while (waitpid(pid, &status, 0) < 0 && errno == EINTR) {}
                error = "clipboard subprocess timed out";
                break;
            }
            usleep(1000);
        }
        char chunk[4096];
        size_t n;
        rewind(out);
        output.clear();
        while ((n = fread(chunk, 1, sizeof(chunk), out)) > 0)
            output.insert(output.end(), chunk, chunk + n);
        bool readOk = !ferror(out);
        rewind(err);
        while ((n = fread(chunk, 1, sizeof(chunk), err)) > 0) error.append(chunk, n);
        closeFiles();
        if (!completed || !WIFEXITED(status) || WEXITSTATUS(status) != 0 || !readOk) {
            if (error.empty()) error = "clipboard subprocess failed (status " + std::to_string(status) + ")";
            output.clear(); // A failed command's partial output is never clipboard data.
            return false;
        }
        return true;
    }

    static bool ToolError(WindowHandle* handle, DataInterchange* data,
                          const char* operation, const std::string& error) {
        handleDataInterchangeError(handle, data,
            std::string("Wayland clipboard ") + operation + " failed: " + error);
        return false;
    }

    bool Clipboard_Wayland::CopyPersist(P_INSTANCE(WindowHandle) handle, P_INSTANCE(DataInterchange) data) {
        if (!data) return false;

        std::vector<std::string> formats;
        for (P_INSTANCE(DragDropData::Node) node = DataInterchange_FormatEnum(data); node != nullptr; node = DataInterchange_FormatEnumNext(node)) {
            utf8_string_struct ty;
            DataInterchange_FormatEnumText(node, &ty);
            const char* s = (const char*)ty;
            if (s && s[0] != '\0') {
                formats.push_back(s);
            }
        }

        if (formats.empty()) return ToolError(handle, data, "copy", "no advertised formats");

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
            data->selected_format = nullptr;
            data->provide_chosen(data, (utf8_string_struct)target_format.c_str());
        }
        if (!data->selected_format.c_str || target_format != data->selected_format.c_str)
            return ToolError(handle, data, "copy", "provider did not supply the requested format");

        P_INSTANCE(void) d = nullptr;
        size_t sz = 0;
        DataInterchange_SelectionReveal(data, nullptr, &d, &sz);
        if (!d) return ToolError(handle, data, "copy", "provider supplied no data");

        std::string mime_type = target_format;
        if (target_format == "text/file-uri") {
            mime_type = "text/uri-list";
        }

        std::string uri_list;
        if (target_format == "text/file-uri") {
            uri_list = LocalPathsToUriList((const char*)d, sz);
            d = (void*)uri_list.data();
            sz = uri_list.size();
        }
        std::vector<uint8_t> output;
        std::string error;
        if (!RunClipboardTool({"wl-copy", "--type", mime_type}, d, sz, output, error))
            return ToolError(handle, data, "copy (wl-copy)", error);
        return true;
    }

    bool Clipboard_Wayland::Paste(P_INSTANCE(WindowHandle) handle, P_INSTANCE(DataInterchange) data) {
        if (!data) return false;
        std::vector<uint8_t> output;
        std::string error;
        if (!RunClipboardTool({"wl-paste", "--list-types"}, nullptr, 0, output, error))
            return ToolError(handle, data, "paste (wl-paste --list-types)", error);
        std::istringstream types(std::string(output.begin(), output.end()));
        std::vector<std::string> raw_types;
        bool has_uri_list = false;
        std::string line;
        while (std::getline(types, line)) {
            if (!line.empty() && line.back() == '\r') line.pop_back();
            if (line.empty()) continue;
            if (line == "text/uri-list") has_uri_list = true;
            raw_types.push_back(line);
        }

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
        if (!data || !fmt_str || fmt_str[0] == '\0') return false;

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
        std::string error;
        bool success = false;
        for (const auto& mime : candidates) {
            error.clear();
            if (RunClipboardTool({"wl-paste", "--no-newline", "--type", mime},
                                 nullptr, 0, buffer, error)) {
                success = true;
                break;
            }
            if (error.find("timed out") != std::string::npos) break;
        }

        if (success) {
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
        return ToolError(data->m_handle, data, "select (wl-paste)", error);
    }

    bool Clipboard_Wayland::Clear() {
        std::vector<uint8_t> output;
        std::string error;
        if (!RunClipboardTool({"wl-copy", "--clear"}, nullptr, 0, output, error))
            return ToolError(nullptr, nullptr, "clear (wl-copy)", error);
        return true;
    }

}
