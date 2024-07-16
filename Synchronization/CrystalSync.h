#ifndef CRYSTALCATALYST__CRYSTALSYNC_H
#define CRYSTALCATALYST__CRYSTALSYNC_H

#include <atomic>
#include <cstddef>

extern "C"
{
_EXPORT_ bool CrystalCatalyst_SubMutex_Size(P_OUT(size_t) sz);
_EXPORT_ bool CrystalCatalyst_SubMutex_Init(P_INSTANCE(void) spiderMutex, utf8_string_const name);
_EXPORT_ bool CrystalCatalyst_SubMutex_Close(P_INSTANCE(void) spiderMutex);
_EXPORT_ bool CrystalCatalyst_SubMutex_Lock(P_INSTANCE(void) spiderMutex);
_EXPORT_ bool CrystalCatalyst_SubMutex_Unlock(P_INSTANCE(void) spiderMutex);
}

class CrystalCatalystMutex
{
public:
    P_INSTANCE(void) spiderMutex;

    CrystalCatalystMutex();
    ~CrystalCatalystMutex();

    virtual bool Init(utf8_string_const name);
    virtual bool Free();
    virtual bool Lock();
    virtual bool Unlock();
};

class CrystalCatalystMutexGimp : public CrystalCatalystMutex
{
public:
    P_INSTANCE(void) spiderMutex;

    virtual bool Init(utf8_string_const name);
    virtual bool Free();
    virtual bool Lock();
    virtual bool Unlock();
};


#endif //CRYSTALCATALYST__CRYSTALSYNC_H
