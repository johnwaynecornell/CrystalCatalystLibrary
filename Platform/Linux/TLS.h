#ifndef TLS_LINUX_H
#define TLS_LINUX_H

#include "../../CrystalCatalystLibrary.h"

class TLS_Posix : public TLS {
public:
    TLS_Posix(TLS_INITIALIZE_VALUE initialize_value, TLS_DESTRUCTOR destructor);
    ~TLS_Posix() override;

    P_INSTANCE(void) get() override;
    void tls_free() override;

protected:
    pthread_key_t key;
};


#endif //TLS_LINUX_H
