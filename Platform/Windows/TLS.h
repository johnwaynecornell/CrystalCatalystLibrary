#ifndef TLS_Windows_H
#define TLS_Windows_H


#include "../../CrystalCatalystLibrary.h"

class TLS_Windows : public TLS {
public:
    TLS_Windows(TLS_INITIALIZE_VALUE initialize_value, TLS_DESTRUCTOR destructor);
    ~TLS_Windows() override;

    P_INSTANCE(void) get() override;
    void tls_free() override;

protected:
    DWORD fls_index;
};



#endif //TLS_Windows_H
