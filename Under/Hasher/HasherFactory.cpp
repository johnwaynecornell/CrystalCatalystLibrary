// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include "../Under.h"
#include "farmhash-c.h"

template<typename T>
void _Compute(P_INSTANCE(HasherClass) h, T var)
{
    h->Compute((P_ELEMENTS(uint8_t) ) &var, sizeof(T), 1);
}

P_INSTANCE(HasherClass) HasherFactory_Get(utf8_string_const Name)
{
    P_INSTANCE(HasherClass) R = nullptr;
    if (strcmp(Name, "PRNG:32")==0) R= new Hasher_PRNG32();
    else if (strcmp(Name, "PRNG:64")==0) R= new Hasher_PRNG64();
    else if (strcmp(Name, "Buffered:UnderCrystalCatalyst:32")==0) R= new Hasher_BufferedForCallback32("UnderCrystalCatalyst", (Hasher_BufferedForCallback32::Hash_fn) farmhash_fingerprint32, 8192);
    else if (strcmp(Name, "Buffered:UnderCrystalCatalyst:64")==0) R= new Hasher_BufferedForCallback64("UnderCrystalCatalyst", (Hasher_BufferedForCallback64::Hash_fn) farmhash_fingerprint64, 8192);

    if (R != nullptr)
    {
        R->Initialize();
        R->Hash_Begin();

        _Compute(R, 0.9827487238479237);
        _Compute(R, 1);
        _Compute(R, 2398492389882384902ul);
        _Compute(R, (uint8_t)0xC5);

        R->Hash_End();

        ReturnBuffer rb = R->get_identifier();

        if (R->get_bits() == 32) printf("%s: sanity value = %08X\n", (utf8_string_const )rb.memory, ((P_INSTANCE(HasherClass32) )R)->get_value());
        else if (R->get_bits() == 64) printf("%s: sanity value = %016lX\n", (utf8_string_const )rb.memory, ((P_INSTANCE(HasherClass64) )R)->get_value());

        ReturnBuffer_Deallocate(rb);
    }

    return R;

}