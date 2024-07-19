// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include <iomanip>
#include <iostream>

#include "../../CrystalCatalystLibrary.h"

#ifdef mod_header
#undef mod_header
#endif

#define mod_header() "CrystalSync:"


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

bool CrystalCatalystMutex::Init(utf8_string_const name)
{
    size_t sz;

    if (!CrystalCatalyst_SubMutex_Size(&sz)) return false;
    spiderMutex = malloc(sz);
    if (spiderMutex == nullptr) return false;

    return CrystalCatalyst_SubMutex_Init(spiderMutex, name);
}

bool CrystalCatalystMutex::Free()
{
    bool RC = CrystalCatalyst_SubMutex_Close(spiderMutex);
    if (RC)
    {
        free(spiderMutex);
        spiderMutex = nullptr;
    }

    return RC;
}

bool CrystalCatalystMutex::Lock()
{
    return CrystalCatalyst_SubMutex_Lock(spiderMutex);
}

bool CrystalCatalystMutex::Unlock()
{
    return CrystalCatalyst_SubMutex_Unlock(spiderMutex);
}

bool CrystalCatalystMutexGimp::Init(utf8_string_const name)
{
    size_t sz;

    if (!CrystalCatalyst_SubMutex_Size(&sz)) return false;
    spiderMutex = malloc(sz);
    if (spiderMutex == nullptr) return false;

    return CrystalCatalyst_SubMutex_Init(spiderMutex, name);
}

bool CrystalCatalystMutexGimp::Free()
{
    return CrystalCatalyst_SubMutex_Close(spiderMutex);
}

bool CrystalCatalystMutexGimp::Lock()
{
    return CrystalCatalyst_SubMutex_Lock(spiderMutex);
}

bool CrystalCatalystMutexGimp::Unlock()
{
    return CrystalCatalyst_SubMutex_Unlock(spiderMutex);
}