// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include "CrystalCatalystLibrary/CrystalCatalystLibrary.h"

#include <fstream>

std::string get_distro_name() {
    std::ifstream file("/etc/os-release");
    if (!file.is_open()) {
        return "Unknown";
    }

    std::string line;
    while (std::getline(file, line)) {
        if (line.find("ID=") == 0) {
            return line.substr(3);
        }
    }
    return "Unknown";
}

bool is_rpm_based() {
    std::ifstream file("/etc/os-release");
    std::string line;
    while (std::getline(file, line)) {
        if (line.find("ID_LIKE=") == 0) {
            if (line.find("fedora") != std::string::npos || line.find("rhel") != std::string::npos) {
                return true;
            }
        }
    }
    return false;
}

P_INSTANCE(CrystalApplication) platform_initialize() {
    return new CrystalApplication_X11();
}

void platform_uninitialize()
{

}