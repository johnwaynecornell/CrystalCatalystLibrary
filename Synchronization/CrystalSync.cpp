// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include <iomanip>
#include <iostream>

#include "CrystalCatalystLibrary/CrystalCatalystLibrary.h"

#ifdef mod_header
#undef mod_header
#endif

#define mod_header() "CrystalSync:"

using namespace JWCEssentials;

namespace NewAge
{

    CrystalCatalystMutex::CrystalCatalystMutex()
    {
        this->spiderMutex = nullptr;
    }

    CrystalCatalystMutex::~CrystalCatalystMutex()
    {
        if (this->spiderMutex != nullptr)
        {
            std::cerr << "CrystalCatalystMutex::~CrystalCatalystMutex() 0x" << std::hex << std::setw(8) << std::setfill('0') << this << std::dec << " never freed";
            exit(-1);
        }
    }

    bool CrystalCatalystMutex::Init(utf8_string_struct name)
    {
        size_t sz;

        if (!SubMutex_Size(&sz)) return false;
        spiderMutex = malloc(sz);
        if (spiderMutex == nullptr) return false;

        return SubMutex_Init(spiderMutex, name);
    }

    bool CrystalCatalystMutex::Free()
    {
        bool RC = SubMutex_Close(spiderMutex);
        if (RC)
        {
            free(spiderMutex);
            spiderMutex = nullptr;
        }

        return RC;
    }

    bool CrystalCatalystMutex::Lock()
    {
        return SubMutex_Lock(spiderMutex);
    }

    bool CrystalCatalystMutex::Unlock()
    {
        return SubMutex_Unlock(spiderMutex);
    }

    bool CrystalCatalystMutexGimp::Init(utf8_string_struct name)
    {
        size_t sz;

        if (!SubMutex_Size(&sz)) return false;
        spiderMutex = malloc(sz);
        if (spiderMutex == nullptr) return false;

        return SubMutex_Init(spiderMutex, name);
    }

    bool CrystalCatalystMutexGimp::Free()
    {
        return SubMutex_Close(spiderMutex);
    }

    bool CrystalCatalystMutexGimp::Lock()
    {
        return SubMutex_Lock(spiderMutex);
    }

    bool CrystalCatalystMutexGimp::Unlock()
    {
        return SubMutex_Unlock(spiderMutex);
    }
}