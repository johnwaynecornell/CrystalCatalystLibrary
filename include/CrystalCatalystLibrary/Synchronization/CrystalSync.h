// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#ifndef CRYSTALCATALYST__CRYSTALSYNC_H
#define CRYSTALCATALYST__CRYSTALSYNC_H

#include <atomic>
#include <cstddef>

namespace NewAge {
    class CrystalCatalystMutex
    {
    public:
        P_INSTANCE(void) spiderMutex;

        CrystalCatalystMutex();
        virtual ~CrystalCatalystMutex();

        virtual bool Init(utf8_string_struct name);
        virtual bool Free();
        virtual bool Lock();
        virtual bool Unlock();
    };

    class CrystalCatalystMutexGimp : public CrystalCatalystMutex
    {
    public:

        bool Init(utf8_string_struct name) override;
        bool Free() override;
        bool Lock() override;
        bool Unlock() override;
    };

    _EXPORT_ bool SubMutex_Size(P_OUT(size_t) sz);
    _EXPORT_ bool SubMutex_Init(P_INSTANCE(void) spiderMutex, utf8_string_struct name);
    _EXPORT_ bool SubMutex_Close(P_INSTANCE(void) spiderMutex);
    _EXPORT_ bool SubMutex_Lock(P_INSTANCE(void) spiderMutex);
    _EXPORT_ bool SubMutex_Unlock(P_INSTANCE(void) spiderMutex);
}

#endif //CRYSTALCATALYST__CRYSTALSYNC_H
