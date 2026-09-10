#ifndef CLIPBOARD_X11_H
#define CLIPBOARD_X11_H

#include <X11/Xlib.h>
#include "CrystalCatalystLibrary/CrystalCatalystLibrary.h"

using namespace JWCEssentials;

namespace NewAge {
    bool FormatToAtom(Display* display, utf8_string_struct format, P_OUT(Atom) atom);
    bool ClipboardTargetWasAdvertised(Display* display, P_INSTANCE(DataInterchange) data, Atom requested, P_OUT(utf8_string_struct) out_format = nullptr);
}

#endif //CLIPBOARD_X11_H
