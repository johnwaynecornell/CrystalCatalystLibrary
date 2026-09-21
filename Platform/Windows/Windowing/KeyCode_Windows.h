// MIT License
// Copyright (c) 2026 John W. Cornell
#pragma once

#include <cstdint>

namespace NewAge {

// Windows virtual-key values are spelled numerically to keep this pure mapping
// testable on either host. Results follow the existing managed KeyCode enum's
// X11 keysym values. Zero means the caller should try its printable-key path.
inline int32_t TranslateWindowsNamedKey(uint32_t virtualKey, uint32_t scanCode, bool extended) {
    if (virtualKey >= 0x70 && virtualKey <= 0x87) // VK_F1 .. VK_F24
        return 0xFFBE + static_cast<int32_t>(virtualKey - 0x70);
    if (virtualKey >= 0x60 && virtualKey <= 0x69) // VK_NUMPAD0 .. VK_NUMPAD9
        return 0xFFB0 + static_cast<int32_t>(virtualKey - 0x60);

    switch (virtualKey) {
        case 0x08: return 0xFF08; // VK_BACK -> BackSpace
        case 0x09: return 0xFF09; // VK_TAB -> Tab (including Shift+Tab)
        case 0x0C: return !extended && scanCode == 0x4C ? 0xFF9D : 0xFF0B; // VK_CLEAR -> KP_Begin / Clear
        case 0x0D: return extended ? 0xFF8D : 0xFF0D; // VK_RETURN -> KP_Enter / Return
        case 0x10: return scanCode == 0x36 ? 0xFFE2 : 0xFFE1; // VK_SHIFT -> right / left
        case 0x11: return extended ? 0xFFE4 : 0xFFE3; // VK_CONTROL -> right / left
        case 0x12: return extended ? 0xFFEA : 0xFFE9; // VK_MENU -> Alt right / left
        case 0x13: return 0xFF13; // VK_PAUSE -> Pause
        case 0x14: return 0xFFE5; // VK_CAPITAL -> CapsLock
        case 0x1B: return 0xFF1B; // VK_ESCAPE -> Escape
        // Non-extended keypad scan codes distinguish NumLock-off navigation
        // from the dedicated (extended) navigation cluster. A zero scan code
        // from synthetic messages keeps the ordinary navigation identity.
        case 0x21: return !extended && scanCode == 0x49 ? 0xFF9A : 0xFF55; // VK_PRIOR -> KP_PageUp / PageUp
        case 0x22: return !extended && scanCode == 0x51 ? 0xFF9B : 0xFF56; // VK_NEXT -> KP_PageDown / PageDown
        case 0x23: return !extended && scanCode == 0x4F ? 0xFF9C : 0xFF57; // VK_END -> KP_End / End
        case 0x24: return !extended && scanCode == 0x47 ? 0xFF95 : 0xFF50; // VK_HOME -> KP_Home / Home
        case 0x25: return !extended && scanCode == 0x4B ? 0xFF96 : 0xFF51; // VK_LEFT -> KP_Left / Left
        case 0x26: return !extended && scanCode == 0x48 ? 0xFF97 : 0xFF52; // VK_UP -> KP_Up / Up
        case 0x27: return !extended && scanCode == 0x4D ? 0xFF98 : 0xFF53; // VK_RIGHT -> KP_Right / Right
        case 0x28: return !extended && scanCode == 0x50 ? 0xFF99 : 0xFF54; // VK_DOWN -> KP_Down / Down
        case 0x2A: // VK_PRINT
        case 0x2C: return 0xFF61; // VK_SNAPSHOT -> Print
        case 0x2D: return !extended && scanCode == 0x52 ? 0xFF9E : 0xFF63; // VK_INSERT -> KP_Insert / Insert
        case 0x2E: return !extended && scanCode == 0x53 ? 0xFF9F : 0xFFFF; // VK_DELETE -> KP_Delete / Delete
        case 0x5B: return 0xFFEB; // VK_LWIN -> Super_Left
        case 0x5C: return 0xFFEC; // VK_RWIN -> Super_Right
        case 0x5D: return 0xFF67; // VK_APPS -> Menu
        case 0x6A: return 0xFFAA; // VK_MULTIPLY -> KP_Multiply
        case 0x6B: return 0xFFAB; // VK_ADD -> KP_Add
        case 0x6C: return 0xFFAC; // VK_SEPARATOR -> KP_Separator
        case 0x6D: return 0xFFAD; // VK_SUBTRACT -> KP_Subtract
        case 0x6E: return 0xFFAE; // VK_DECIMAL -> KP_Decimal
        case 0x6F: return 0xFFAF; // VK_DIVIDE -> KP_Divide
        case 0x90: return 0xFF7F; // VK_NUMLOCK -> NumLock
        case 0x91: return 0xFF14; // VK_SCROLL -> ScrollLock
        case 0xA0: return 0xFFE1; // VK_LSHIFT
        case 0xA1: return 0xFFE2; // VK_RSHIFT
        case 0xA2: return 0xFFE3; // VK_LCONTROL
        case 0xA3: return 0xFFE4; // VK_RCONTROL
        case 0xA4: return 0xFFE9; // VK_LMENU
        case 0xA5: return 0xFFEA; // VK_RMENU
        default: return 0;
    }
}

} // namespace NewAge
