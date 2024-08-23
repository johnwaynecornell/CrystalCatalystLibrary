// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include "TLS.h"

using namespace JWCEssentials;

namespace NewAge {
    void tls_cleanup(P_INSTANCE(void) data) {
        if (data) {
            P_INSTANCE(TLS::TLSData) tls_data = static_cast<P_INSTANCE(TLS::TLSData) >(data);
            if (tls_data->tls_instance->destructor) {
                tls_data->tls_instance->destructor(tls_data->value);
            }
            free(tls_data);
        }
    }


    TLS_Posix::TLS_Posix(TLS_INITIALIZE_VALUE initialize_value, TLS_DESTRUCTOR destructor)
        : TLS(initialize_value, destructor) {
        pthread_key_create(&key, tls_cleanup);
    }

    TLS_Posix::~TLS_Posix() {
        pthread_key_delete(key);
    }

    void TLS_Posix::tls_free()
    {
    }

    P_INSTANCE(void) TLS_Posix::get() {
        P_INSTANCE(TLSData) data = static_cast<P_INSTANCE(TLSData) >(pthread_getspecific(key));
        if (!data) {
            data = static_cast<P_INSTANCE(TLSData) >(malloc(sizeof(TLSData)));
            data->tls_instance = this;
            data->value = initialize_value();
            pthread_setspecific(key, data);
        }
        return data->value;
    }

    P_INSTANCE(TLS) TLS_Alloc(TLS_INITIALIZE_VALUE initialize_value, TLS_DESTRUCTOR destructor) {
        return new TLS_Posix(initialize_value, destructor);
    }
}