#include <ctime>
#include "../Under.h"
#include <typeinfo>

int32_t main(int32_t argc, P_ELEMENTS(utf8_string) argv) {

    P_INSTANCE(HasherClass32) h = (P_INSTANCE(HasherClass32)  )HasherFactory_Get("Buffered:UnderCrystalCatalyst:32");
    h->set_endian(false);
    printf("%s\n", h->get_endian() ? "BIG" : "LITTLE");

    ReturnBuffer R = h->get_identifier();
    printf("%s\n", (utf8_string_const ) R.memory);
    ReturnBuffer_Deallocate(R);

    h->Hash_Begin();
    double t = 0.9283748721;
    h->Compute((P_ELEMENTS(uint8_t) ) &t, sizeof(t), 1);
    h->Compute((P_ELEMENTS(uint8_t) ) &t, sizeof(t), 1);
    h->Compute((P_ELEMENTS(uint8_t) ) &t, sizeof(t), 1);
    h->Hash_End();

    printf("%08X\n", h->get_value());

    return 0;
}