#ifndef TLS_H
#define TLS_H

#ifdef _WIN32
#include <windows.h>
#else
#include <pthread.h>
#endif
#include "Under/Under.h"

typedef P_INSTANCE(void) (*TLS_INITIALIZE_VALUE)();
typedef void (*TLS_DESTRUCTOR)(P_INSTANCE(void) value);

class TLS {
public:
    TLS(TLS_INITIALIZE_VALUE initialize_value, TLS_DESTRUCTOR destructor);
    virtual ~TLS();

    virtual P_INSTANCE(void) get() = 0;
    virtual void tls_free() = 0;

    TLS_INITIALIZE_VALUE initialize_value;
    TLS_DESTRUCTOR destructor;

    struct TLSData {
        P_INSTANCE(TLS) tls_instance;
        P_INSTANCE(void) value;
    };
};

extern "C" {
    _EXPORT_ P_INSTANCE(TLS) TLS_Alloc(TLS_INITIALIZE_VALUE initialize_value, TLS_DESTRUCTOR destructor);
    _EXPORT_ void TLS_Free(P_INSTANCE(TLS) tls);
    _EXPORT_ P_INSTANCE(void) TLS_get(P_INSTANCE(TLS) tls);
}

#endif // TLS_H
