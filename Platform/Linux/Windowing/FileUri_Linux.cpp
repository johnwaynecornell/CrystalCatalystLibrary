#include "FileUri_Linux.h"

#include <algorithm>
#include <cctype>
#include <cstdint>
#include <iomanip>
#include <sstream>

namespace NewAge {

namespace {

// RFC 3986 unreserved characters and path separators.
// Unreserved = ALPHA / DIGIT / "-" / "." / "_" / "~"
bool IsUnreservedOrPathSep(unsigned char c) {
    if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
        return true;
    if (c == '-' || c == '_' || c == '.' || c == '~' || c == '/')
        return true;
    return false;
}

int HexDigitToValue(char c) {
    if (c >= '0' && c <= '9') return c - '0';
    if (c >= 'a' && c <= 'f') return c - 'a' + 10;
    if (c >= 'A' && c <= 'F') return c - 'A' + 10;
    return -1;
}

bool CaseInsensitiveStartsWith(const std::string& str, const std::string& prefix) {
    if (str.size() < prefix.size()) return false;
    for (size_t i = 0; i < prefix.size(); ++i) {
        if (std::tolower(static_cast<unsigned char>(str[i])) !=
            std::tolower(static_cast<unsigned char>(prefix[i]))) {
            return false;
        }
    }
    return true;
}

bool CaseInsensitiveEquals(const std::string& a, const std::string& b) {
    if (a.size() != b.size()) return false;
    for (size_t i = 0; i < a.size(); ++i) {
        if (std::tolower(static_cast<unsigned char>(a[i])) !=
            std::tolower(static_cast<unsigned char>(b[i]))) {
            return false;
        }
    }
    return true;
}

std::string PercentEncodePath(const std::string& path) {
    std::string result;
    result.reserve(path.size() * 3);
    const char hex_chars[] = "0123456789ABCDEF";

    for (unsigned char c : path) {
        if (IsUnreservedOrPathSep(c)) {
            result.push_back(static_cast<char>(c));
        } else {
            result.push_back('%');
            result.push_back(hex_chars[(c >> 4) & 0x0F]);
            result.push_back(hex_chars[c & 0x0F]);
        }
    }
    return result;
}

std::string PercentDecode(const std::string& str) {
    std::string result;
    result.reserve(str.size());
    for (size_t i = 0; i < str.size(); ++i) {
        if (str[i] == '%' && i + 2 < str.size()) {
            int h1 = HexDigitToValue(str[i + 1]);
            int h2 = HexDigitToValue(str[i + 2]);
            if (h1 >= 0 && h2 >= 0) {
                result.push_back(static_cast<char>((h1 << 4) | h2));
                i += 2;
                continue;
            }
        }
        result.push_back(str[i]);
    }
    return result;
}

} // anonymous namespace

std::string LocalPathToFileUri(const std::string& path) {
    if (path.empty()) return "";

    // If it is already a file URI, avoid double encoding.
    if (CaseInsensitiveStartsWith(path, "file://") || CaseInsensitiveStartsWith(path, "file:/")) {
        return path;
    }

    std::string encoded = PercentEncodePath(path);
    if (!encoded.empty() && encoded[0] == '/') {
        return "file://" + encoded; // Produces "file:///" + path after root slash
    }
    return "file:///" + encoded;
}

bool FileUriToLocalPath(const std::string& uri, std::string& path) {
    path.clear();
    if (uri.empty()) return false;

    // Must start with "file:"
    if (!CaseInsensitiveStartsWith(uri, "file:")) {
        return false;
    }

    std::string remainder;
    if (CaseInsensitiveStartsWith(uri, "file://localhost/")) {
        remainder = uri.substr(16); // Starts with '/'
    } else if (CaseInsensitiveEquals(uri, "file://localhost")) {
        remainder = "/";
    } else if (CaseInsensitiveStartsWith(uri, "file:///")) {
        remainder = uri.substr(7); // Starts with '/'
    } else if (CaseInsensitiveStartsWith(uri, "file:/") && !CaseInsensitiveStartsWith(uri, "file://")) {
        remainder = uri.substr(5); // Starts with '/'
    } else {
        // Unknown or remote host (e.g. file://remotehost/path or file:invalid)
        return false;
    }

    std::string decoded = PercentDecode(remainder);
    if (decoded.empty()) {
        return false;
    }

    path = decoded;
    return true;
}

std::string LocalPathsToUriList(const char* data, size_t size) {
    if (!data || size == 0) return "";
    std::string input(data, size);
    std::istringstream stream(input);
    std::string line;
    std::string uri_list;

    while (std::getline(stream, line)) {
        while (!line.empty() && (line.back() == '\r' || line.back() == '\n')) {
            line.pop_back();
        }
        size_t first = line.find_first_not_of(" \t");
        if (first == std::string::npos) continue;
        size_t last = line.find_last_not_of(" \t");
        std::string trimmed = line.substr(first, last - first + 1);
        if (trimmed.empty()) continue;

        std::string uri = LocalPathToFileUri(trimmed);
        if (!uri.empty()) {
            uri_list += uri + "\r\n";
        }
    }
    return uri_list;
}

std::string LocalPathsToUriList(const std::string& paths) {
    return LocalPathsToUriList(paths.data(), paths.size());
}

std::string UriListToLocalPaths(const char* data, size_t size) {
    if (!data || size == 0) return "";
    std::string input(data, size);
    std::istringstream stream(input);
    std::string line;
    std::string local_paths;

    while (std::getline(stream, line)) {
        while (!line.empty() && (line.back() == '\r' || line.back() == '\n')) {
            line.pop_back();
        }
        size_t first = line.find_first_not_of(" \t");
        if (first == std::string::npos) continue;
        size_t last = line.find_last_not_of(" \t");
        std::string trimmed = line.substr(first, last - first + 1);
        if (trimmed.empty()) continue;

        // Skip URI-list comments
        if (trimmed[0] == '#') {
            continue;
        }

        std::string path;
        if (FileUriToLocalPath(trimmed, path)) {
            if (!path.empty()) {
                local_paths += path + "\n";
            }
        }
    }
    return local_paths;
}

std::string UriListToLocalPaths(const std::string& uri_list) {
    return UriListToLocalPaths(uri_list.data(), uri_list.size());
}

} // namespace NewAge
