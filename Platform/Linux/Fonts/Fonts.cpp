// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include "Fonts.h"

#include <iostream>
#include <string>
#include <filesystem>

namespace fs = std::filesystem;

using namespace JWCEssentials;

namespace NewAge {
    struct StringKeyval {
        utf8_string_struct key;
        utf8_string_struct value;
    };

    StringKeyval distro_instructions[] = {
        { "ubuntu", "sudo apt update\nsudo apt install ttf-mscorefonts-installer\n"},
        {"debian", "sudo apt update\nsudo apt install ttf-mscorefonts-installer\n"},
        {"fedora", "sudo dnf install https://download1.rpmfusion.org/free/fedora/rpmfusion-free-release-(rpm -E %fedora).noarch.rpm\n"
                 "sudo dnf install https://download1.rpmfusion.org/nonfree/fedora/rpmfusion-nonfree-release-(rpm -E %fedora).noarch.rpm\n"
                 "sudo dnf install msttcore-fonts-installer\n"},

    {"arch", "Edit /etc/pacman.conf and uncomment the following lines:\n"
             "[multilib]\nInclude = /etc/pacman.d/mirrorlist\n"
             "Then install the package using yay:\nyay -S ttf-ms-fonts\n"},
    {"opensuse", "sudo zypper ar -f https://download.opensuse.org/repositories/M17N:/fonts/openSUSE_Leap_15.3/ M17N-fonts\n"
                 "sudo zypper in fetchmsttfonts\n"
                 "sudo fetchmsttfonts\n"},
        {nullptr, nullptr}
    };

    bool CrystalCatalyst_Fonts_HasMSCoreFonts(void (*callback)(utf8_string_struct OS, utf8_string_struct Instructions)) {
        // Check if the directory exists
        if (fs::exists("/usr/share/fonts/truetype/msttcorefonts/")) return true;

        std::string distro = get_distro_name();
        std::string message = "MS Core fonts not found at '/usr/share/fonts/truetype/msttcorefonts/'\n";

        int32_t i = 0;
        for (; distro_instructions[i].key.c_str != nullptr; i++) {
            std::string d = distro_instructions[i].key.c_str;
            if (distro == d) {
                message += "To perform the install, try:\n";
                message += distro_instructions[i].value;
                break;
            }
        }

        if (distro_instructions[i].key.c_str == nullptr) {
            message += "no instructions, try a web search for 'install Microsoft's TrueType core fonts' for your distro\n";

            if (is_rpm_based()) {
                message += "Or, seeing as it's RPM based. perhaps see https://corefonts.sourceforge.net/\n";
            }

        }

        if (callback) callback(distro.c_str(), message.c_str());
        else std::cerr << "your detected distro is " << distro << std::endl << message;

        return false;
    }
}