#ifndef CLIPBOARD_WAYLAND_H
#define CLIPBOARD_WAYLAND_H

#include <string>
#include <vector>
#include <cstdint>
#include "CrystalCatalystLibrary/CrystalCatalystLibrary.h"

using namespace JWCEssentials;

namespace NewAge {

    class Clipboard_Wayland {
    public:
        std::vector<uint8_t> selected_data;

        void copyToClipboard();
        void pasteFromClipboard();
        bool isClipboardDataAvailable() const;
        void CrystalWindow_ClipboardCopyPersist();

        // CrystalCatalyst integration helpers
        static bool IsAvailable();
        static bool CopyPersist(P_INSTANCE(WindowHandle) handle, P_INSTANCE(DataInterchange) data);
        static bool Paste(P_INSTANCE(WindowHandle) handle, P_INSTANCE(DataInterchange) data);
        static bool Select(P_INSTANCE(DataInterchange) data, utf8_string_struct format);
        static bool Clear();
    };

}

#endif //CLIPBOARD_WAYLAND_H
