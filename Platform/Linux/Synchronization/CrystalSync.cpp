#include "CrystalSync.h"

bool CrystalCatalyst_SubMutex_Size(P_OUT(size_t) Sz)
{
    *Sz = sizeof(pthread_mutex_t);
    return true;
}

bool CrystalCatalyst_SubMutex_Init(P_INSTANCE(void) spiderMutex, utf8_string_const name)
{
    pthread_mutexattr_t attr;
    pthread_mutexattr_init(&attr);

    pthread_mutexattr_settype(&attr, PTHREAD_MUTEX_RECURSIVE);

    P_INSTANCE(pthread_mutex_t) R = new pthread_mutex_t();
    pthread_mutex_init(R, &attr);

    pthread_mutexattr_destroy(&attr);
    *((P_IN_OUT(P_INSTANCE(pthread_mutex_t)))spiderMutex) = R;
    return true;
}

bool CrystalCatalyst_SubMutex_Close(P_INSTANCE(void) spiderMutex)
{
    pthread_mutex_destroy(*((P_IN_OUT(P_INSTANCE(pthread_mutex_t)))spiderMutex));
    delete *((P_IN_OUT(P_INSTANCE(pthread_mutex_t)))spiderMutex);

    return true;
}

bool CrystalCatalyst_SubMutex_Lock(P_INSTANCE(void) spiderMutex)
{
    while (pthread_mutex_lock(*((P_IN_OUT(P_INSTANCE(pthread_mutex_t)))spiderMutex)) != 0);
    return true;
}

bool CrystalCatalyst_SubMutex_Unlock(P_INSTANCE(void) spiderMutex)
{
    pthread_mutex_unlock(*((P_IN_OUT(P_INSTANCE(pthread_mutex_t)))spiderMutex));
    return true;
}
