// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
//
// XDG Desktop Portal screen capture via org.freedesktop.portal.Screenshot.
// Works on GNOME, KDE, wlroots compositors, and any Wayland compositor
// that ships the portal with Screenshot support.
//
// Requires: dbus-1, libpng

#include "CrystalOptics/CaptureAPI.h"

#ifdef HAVE_DBUS

#include <dbus/dbus.h>
#include <png.h>

#include <cstdio>
#include <cstdlib>
#include <cstring>
#include <string>
#include <vector>

namespace CrystalOptics {

static bool capture_pix_free(uint8_t* p) { delete[] p; return true; }

// ─── PNG decode via libpng ───────────────────────────────────────────────────

static PixData png_file_to_pixdata(const char* path) {
    FILE* fp = fopen(path, "rb");
    if (!fp) return PixData{};

    uint8_t sig[8];
    if (fread(sig, 1, 8, fp) != 8 || png_sig_cmp(sig, 0, 8)) {
        fclose(fp); return PixData{};
    }

    png_structp png = png_create_read_struct(PNG_LIBPNG_VER_STRING,
                                             nullptr, nullptr, nullptr);
    if (!png) { fclose(fp); return PixData{}; }

    png_infop info = png_create_info_struct(png);
    if (!info) {
        png_destroy_read_struct(&png, nullptr, nullptr);
        fclose(fp); return PixData{};
    }

    if (setjmp(png_jmpbuf(png))) {
        png_destroy_read_struct(&png, &info, nullptr);
        fclose(fp); return PixData{};
    }

    png_init_io(png, fp);
    png_set_sig_bytes(png, 8);
    png_read_info(png, info);

    int w    = (int)png_get_image_width(png, info);
    int h    = (int)png_get_image_height(png, info);
    int ct   = png_get_color_type(png, info);
    int bits = png_get_bit_depth(png, info);

    // Normalize to 8-bit RGBA
    if (bits == 16)                        png_set_strip_16(png);
    if (ct == PNG_COLOR_TYPE_PALETTE)      png_set_palette_to_rgb(png);
    if (ct == PNG_COLOR_TYPE_GRAY && bits < 8) png_set_expand_gray_1_2_4_to_8(png);
    if (png_get_valid(png, info, PNG_INFO_tRNS)) png_set_tRNS_to_alpha(png);
    if (ct == PNG_COLOR_TYPE_RGB  ||
        ct == PNG_COLOR_TYPE_GRAY ||
        ct == PNG_COLOR_TYPE_PALETTE)      png_set_filler(png, 0xFF, PNG_FILLER_AFTER);
    if (ct == PNG_COLOR_TYPE_GRAY ||
        ct == PNG_COLOR_TYPE_GRAY_ALPHA)   png_set_gray_to_rgb(png);

    png_read_update_info(png, info);

    size_t  row_bytes = png_get_rowbytes(png, info);
    size_t  size      = (size_t)h * row_bytes;
    uint8_t* buf      = new uint8_t[size];

    std::vector<png_bytep> rows(h);
    for (int y = 0; y < h; y++)
        rows[y] = buf + (size_t)y * row_bytes;

    png_read_image(png, rows.data());
    png_destroy_read_struct(&png, &info, nullptr);
    fclose(fp);

    // PNG is RGBA; convert to BGRA for PixData convention
    for (size_t i = 0; i < size; i += 4)
        std::swap(buf[i], buf[i + 2]);  // R <-> B

    PixData r{};
    r.pix_format      = utf8_string_struct("bgra:int8");
    r.pix_data        = buf;
    r.pix_data_length = size;
    r.width           = w;
    r.height          = h;
    r.pix_data_free   = capture_pix_free;
    return r;
}

// ─── D-Bus portal call ───────────────────────────────────────────────────────

// Extracts a string variant from a DBusMessageIter (a{sv} dict entry).
static std::string get_dict_string(DBusMessageIter* dict, const char* key) {
    DBusMessageIter entry;
    while (dbus_message_iter_get_arg_type(dict) == DBUS_TYPE_DICT_ENTRY) {
        dbus_message_iter_recurse(dict, &entry);

        const char* k = nullptr;
        dbus_message_iter_get_basic(&entry, &k);
        dbus_message_iter_next(&entry);

        if (k && strcmp(k, key) == 0) {
            DBusMessageIter variant;
            dbus_message_iter_recurse(&entry, &variant);
            if (dbus_message_iter_get_arg_type(&variant) == DBUS_TYPE_STRING) {
                const char* v = nullptr;
                dbus_message_iter_get_basic(&variant, &v);
                return v ? std::string(v) : "";
            }
        }
        dbus_message_iter_next(dict);
    }
    return "";
}

PixData Capture_Portal() {
    DBusError err;
    dbus_error_init(&err);

    DBusConnection* conn = dbus_bus_get(DBUS_BUS_SESSION, &err);
    if (dbus_error_is_set(&err) || !conn) {
        dbus_error_free(&err);
        return PixData{};
    }

    // Unique token for matching the response signal
    static int token_counter = 0;
    std::string token = "crystaloptics_" + std::to_string(++token_counter);

    // Build the Screenshot method call
    DBusMessage* msg = dbus_message_new_method_call(
        "org.freedesktop.portal.Desktop",
        "/org/freedesktop/portal/desktop",
        "org.freedesktop.portal.Screenshot",
        "Screenshot");
    if (!msg) { dbus_connection_unref(conn); return PixData{}; }

    // Arguments: parent_window (empty string) + options dict {interactive: false, handle_token: token}
    DBusMessageIter args, options, entry, variant;
    dbus_message_iter_init_append(msg, &args);

    const char* parent = "";
    dbus_message_iter_append_basic(&args, DBUS_TYPE_STRING, &parent);

    dbus_message_iter_open_container(&args, DBUS_TYPE_ARRAY, "{sv}", &options);

    // interactive = false
    {
        const char* k = "interactive";
        dbus_message_iter_open_container(&options, DBUS_TYPE_DICT_ENTRY, nullptr, &entry);
        dbus_message_iter_append_basic(&entry, DBUS_TYPE_STRING, &k);
        dbus_message_iter_open_container(&entry, DBUS_TYPE_VARIANT, "b", &variant);
        dbus_bool_t interactive = FALSE;
        dbus_message_iter_append_basic(&variant, DBUS_TYPE_BOOLEAN, &interactive);
        dbus_message_iter_close_container(&entry, &variant);
        dbus_message_iter_close_container(&options, &entry);
    }

    // handle_token = token
    {
        const char* k = "handle_token";
        const char* v = token.c_str();
        dbus_message_iter_open_container(&options, DBUS_TYPE_DICT_ENTRY, nullptr, &entry);
        dbus_message_iter_append_basic(&entry, DBUS_TYPE_STRING, &k);
        dbus_message_iter_open_container(&entry, DBUS_TYPE_VARIANT, "s", &variant);
        dbus_message_iter_append_basic(&variant, DBUS_TYPE_STRING, &v);
        dbus_message_iter_close_container(&entry, &variant);
        dbus_message_iter_close_container(&options, &entry);
    }

    dbus_message_iter_close_container(&args, &options);

    // Send and get the reply (contains the handle object path)
    DBusMessage* reply = dbus_connection_send_with_reply_and_block(
        conn, msg, 10000, &err);
    dbus_message_unref(msg);

    if (dbus_error_is_set(&err) || !reply) {
        dbus_error_free(&err);
        dbus_connection_unref(conn);
        return PixData{};
    }

    // Extract handle path from reply
    const char* handle_path_raw = nullptr;
    dbus_message_get_args(reply, &err, DBUS_TYPE_OBJECT_PATH, &handle_path_raw,
                          DBUS_TYPE_INVALID);
    std::string handle_path = handle_path_raw ? handle_path_raw : "";
    dbus_message_unref(reply);

    if (handle_path.empty()) {
        dbus_connection_unref(conn);
        return PixData{};
    }

    // Subscribe to the Response signal on the handle object
    std::string match =
        "type='signal',"
        "interface='org.freedesktop.portal.Request',"
        "member='Response',"
        "path='" + handle_path + "'";
    dbus_bus_add_match(conn, match.c_str(), &err);
    dbus_connection_flush(conn);
    if (dbus_error_is_set(&err)) {
        dbus_error_free(&err);
        dbus_connection_unref(conn);
        return PixData{};
    }

    // Poll for the Response signal (up to 30s — user may need to confirm)
    PixData result{};
    const int timeout_ms = 30000;
    int elapsed = 0;

    while (elapsed < timeout_ms) {
        dbus_connection_read_write(conn, 100);
        elapsed += 100;

        DBusMessage* sig = dbus_connection_pop_message(conn);
        if (!sig) continue;

        if (dbus_message_is_signal(sig, "org.freedesktop.portal.Request", "Response") &&
            strcmp(dbus_message_get_path(sig), handle_path.c_str()) == 0) {

            DBusMessageIter sig_iter;
            dbus_message_iter_init(sig, &sig_iter);

            uint32_t response_code = 1;
            dbus_message_iter_get_basic(&sig_iter, &response_code);
            dbus_message_iter_next(&sig_iter);

            if (response_code == 0 &&
                dbus_message_iter_get_arg_type(&sig_iter) == DBUS_TYPE_ARRAY) {
                DBusMessageIter results;
                dbus_message_iter_recurse(&sig_iter, &results);
                std::string uri = get_dict_string(&results, "uri");

                // uri is file:///path/to/screenshot.png
                std::string path = uri;
                if (path.substr(0, 7) == "file://")
                    path = path.substr(7);

                result = png_file_to_pixdata(path.c_str());
            }

            dbus_message_unref(sig);
            break;
        }
        dbus_message_unref(sig);
    }

    dbus_bus_remove_match(conn, match.c_str(), &err);
    dbus_error_free(&err);
    dbus_connection_unref(conn);
    return result;
}

} // namespace CrystalOptics

#else // !HAVE_DBUS

namespace CrystalOptics {
PixData Capture_Portal() { return PixData{}; }
}

#endif
