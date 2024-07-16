#include "TLS.h"
#include <stdlib.h>

TLS::TLS(TLS_INITIALIZE_VALUE initialize_value, TLS_DESTRUCTOR destructor) {
    this->initialize_value = initialize_value;
    this->destructor = destructor;
}

TLS::~TLS() {

}

void TLS_Free(P_INSTANCE(TLS) tls) {
    tls->tls_free();
    delete tls;
}

P_INSTANCE(void) TLS_get(P_INSTANCE(TLS) tls) {
    return tls->get();
}
