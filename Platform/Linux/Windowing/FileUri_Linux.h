#pragma once

#include <string>

namespace NewAge {

// Encodes a single local filesystem path into a file:// URI.
// Handles spaces, '#', '%', UTF-8 characters via percent-encoding.
std::string LocalPathToFileUri(const std::string& path);

// Decodes a single file:// URI (e.g. file:///..., file://localhost/...) back to a local filesystem path.
// Rejects remote/unsupported hosts and non-file schemes.
// Returns true on success, false on failure.
bool FileUriToLocalPath(const std::string& uri, std::string& path);

// Converts a list of local paths (separated by newlines) to a text/uri-list payload with CRLF endings.
std::string LocalPathsToUriList(const char* data, size_t size);
std::string LocalPathsToUriList(const std::string& paths);

// Converts a text/uri-list payload (lines with URIs or comments) to a newline-separated list of local paths.
std::string UriListToLocalPaths(const char* data, size_t size);
std::string UriListToLocalPaths(const std::string& uri_list);

} // namespace NewAge
