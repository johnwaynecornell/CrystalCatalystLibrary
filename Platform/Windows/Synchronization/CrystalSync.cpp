#include "CrystalSync.h"


bool CrystalCatalyst_SubMutex_Size(P_OUT(size_t) Sz)
{

    *Sz = sizeof(HANDLE);
    return true;
}

bool CrystalCatalyst_SubMutex_Init(P_INSTANCE(void) spiderMutex, utf8_string_const name)
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
