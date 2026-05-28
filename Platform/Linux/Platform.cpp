// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include "CrystalCatalystLibrary/CrystalCatalystLibrary.h"

#include <fstream>
#include <map>
#include <mutex>

#include "CrystalApplication_X11.h"

using namespace JWCEssentials;

namespace NewAge {
    std::string get_distro_name() {
        std::ifstream file("/etc/os-release");
        if (!file.is_open()) {
            return "Unknown";
        }

        std::string line;
        while (std::getline(file, line)) {
            if (line.find("ID=") == 0) {
                std::string val = line.substr(3);
                if (val.size() >= 2 && val.front() == '"' && val.back() == '"') {
                    val = val.substr(1, val.size() - 2);
                }
                return val;
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

    static std::map<Atom, std::string> atom_cache;
    static std::mutex atom_cache_mutex;

    utf8_string_struct XGetAtomName_struct(Display *display, Atom A) {
        if (A == None) return nullptr;

        std::lock_guard<std::mutex> lock(atom_cache_mutex);
        auto it = atom_cache.find(A);
        if (it != atom_cache.end()) {
            utf8_string_struct R = (char*)it->second.c_str();
            return R;
        }

        char *val = XGetAtomName(display, A);
        if (val) {
            atom_cache[A] = val;
            XFree(val);
            utf8_string_struct R = (char*)atom_cache[A].c_str();
            return R;
        }

        return nullptr;
    }
}