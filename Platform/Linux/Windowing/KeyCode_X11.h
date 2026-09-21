// MIT License
// Copyright (c) 2026 John W. Cornell
#pragma once

#include <cstdint>
#include <limits>
#include <X11/Xlib.h>
#include <X11/Xutil.h>
#include <X11/keysym.h>

namespace NewAge {

inline int32_t NormalizeX11KeySym(KeySym symbol) {
    // The public enum has one Tab key; modifiers do not change that identity.
    if (symbol == XK_ISO_Left_Tab) return XK_Tab;
    // KeySyms are not Unicode scalars. In particular, Unicode-encoded KeySyms
    // carry a 0x01000000 prefix which must survive the callback unchanged.
    if (symbol > static_cast<KeySym>(std::numeric_limits<int32_t>::max())) return 0;
    return static_cast<int32_t>(symbol);
}

inline int32_t TranslateX11KeyCode(XKeyEvent* event) {
    char buffer[4];
    KeySym symbol = NoSymbol;
    XLookupString(event, buffer, sizeof(buffer), &symbol, nullptr);
    return NormalizeX11KeySym(symbol);
}

} // namespace NewAge
