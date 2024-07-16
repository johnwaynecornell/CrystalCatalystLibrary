#include "CrystalCatalystLibrary.h"

P_INSTANCE(CrystalCatalystLibrary)  TheCrystalCatalystLibrary = nullptr;

P_INSTANCE(CrystalCatalystMutex) initMutex()
{
    P_INSTANCE(CrystalCatalystMutex) R = new CrystalCatalystMutexGimp();
    R->Init(nullptr);
    return R;
}

P_INSTANCE(CrystalCatalystMutex)  TheCrystalCatalystMutex = initMutex();

bool CrystalCatalystLibrary_Initialize()
{
    bool rc = true;
    if (TheCrystalCatalystLibrary == nullptr) {
        if (!TheCrystalCatalystMutex->Lock()) return false;
        P_INSTANCE(CrystalCatalystMutex) origMutex = TheCrystalCatalystMutex;
        P_INSTANCE(CrystalCatalystMutex) newMutex = nullptr;
        if (TheCrystalCatalystLibrary == nullptr) {
            TheCrystalCatalystLibrary = new CrystalCatalystLibrary();
            TheCrystalCatalystLibrary->initializationCount.store(0);
            rc = TheCrystalCatalystLibrary->Initialize();
            newMutex = new CrystalCatalystMutex();
            newMutex->Init(nullptr);
            newMutex->Lock();
            TheCrystalCatalystMutex = newMutex;
        }
        if (origMutex->Unlock()) rc = false;
        if (newMutex != nullptr) if (!newMutex->Unlock()) rc = false;
    }
    if (rc) TheCrystalCatalystLibrary->initializationCount.fetch_add(1, std::memory_order_relaxed);
    return rc;
}

bool CrystalCatalystLibrary_Close()
{
    if (TheCrystalCatalystLibrary == nullptr) return false;

    bool rc = true;

    if (TheCrystalCatalystLibrary->initializationCount.fetch_add(-1, std::memory_order_relaxed) == 1) {

        P_INSTANCE(CrystalCatalystMutex) m = nullptr;

        if (!TheCrystalCatalystMutex->Lock()) return false;
        if (TheCrystalCatalystLibrary == nullptr) {
            rc = TheCrystalCatalystLibrary->Free();
            if (rc) {
                delete TheCrystalCatalystLibrary;

                TheCrystalCatalystLibrary = nullptr;
                m = TheCrystalCatalystMutex;
                if (!m->Unlock()) return false;
            }
        }
        if (TheCrystalCatalystMutex != nullptr &&!TheCrystalCatalystMutex->Unlock()) return false;
        return rc;
    }
    return rc;
}


P_INSTANCE(TLS)  _initialize_errors_();

bool CrystalCatalystLibrary::Initialize()
{
    errors = _initialize_errors_();
    return true;
}

bool CrystalCatalystLibrary::Free()
{
    return true;
}

bool StartingWith(utf8_string_const prefix, utf8_string_const str) {
    if (prefix == nullptr) return str == nullptr;
    if (str == nullptr) return false;

    for (int32_t i = 0; prefix[i] != 0; i++) {
        if (str[i] != prefix[i]) return false;
    }

    return true;
}