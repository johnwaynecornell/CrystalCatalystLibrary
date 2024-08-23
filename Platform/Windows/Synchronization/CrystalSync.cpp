// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include "CrystalSync.h"

using namespace JWCEssentials;

namespace NewAge {


bool CrystalCatalyst_SubMutex_Size(P_OUT(size_t) Sz)
{

    *Sz = sizeof(HANDLE);
    return true;
}

bool CrystalCatalyst_SubMutex_Init(P_INSTANCE(void) spiderMutex, utf8_string_struct name)
{
    *((P_INSTANCE(HANDLE))spiderMutex) = CreateMutexExA(nullptr, name,0, MUTEX_ALL_ACCESS);

    return true;
}

bool CrystalCatalyst_SubMutex_Close(P_INSTANCE(void) spiderMutex)
{
    CloseHandle(*((P_INSTANCE(HANDLE))spiderMutex));
    return true;
}

bool CrystalCatalyst_SubMutex_Lock(P_INSTANCE(void) spiderMutex)
{
    WaitForSingleObject(*((P_INSTANCE(HANDLE))spiderMutex), INFINITE);
    return true;
}

bool CrystalCatalyst_SubMutex_Unlock(P_INSTANCE(void) spiderMutex)
{

    ReleaseMutex(*((P_INSTANCE(HANDLE))spiderMutex));
    return true;
}
}