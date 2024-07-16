#include "TLS.h"


void tls_cleanup(P_INSTANCE(void) data) {
    if (data) {
        P_INSTANCE(TLS::TLSData) tls_data = static_cast<P_INSTANCE(TLS::TLSData) >(data);
        if (tls_data->tls_instance->destructor) {
            tls_data->tls_instance->destructor(tls_data->value);
        }
        free(tls_data);
    }
}

P_INSTANCE(TLS) TLS_Alloc(TLS_INITIALIZE_VALUE initialize_value, TLS_DESTRUCTOR destructor) {
    return new TLS_Windows(initialize_value, destructor);
}

TLS_Windows::TLS_Windows(TLS_INITIALIZE_VALUE initialize_value, TLS_DESTRUCTOR destructor)
    : TLS(initialize_value, destructor) {
    fls_index = FlsAlloc(tls_cleanup);
}

TLS_Windows::~TLS_Windows() {
    FlsFree(fls_index);
}

void TLS_Windows::tls_free()
{
}

P_INSTANCE(void) TLS_Windows::get() {
    P_INSTANCE(TLSData) data = static_cast<P_INSTANCE(TLSData) >(FlsGetValue(fls_index));
    if (!data) {
        data = static_cast<P_INSTANCE(TLSData) >(malloc(sizeof(TLSData)));
        data->tls_instance = this;
        data->value = initialize_value();
        FlsSetValue(fls_index, data);
    }
    return data->value;
}
