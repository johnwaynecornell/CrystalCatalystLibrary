// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include "../Under.h"
#include "Hasher_SMHasher.h"

void Hasherproc32(P_INSTANCE(HasherClass32) hasher, P_INSTANCE(const void) buffer, int32_t len, uint32_t seed, P_ELEMENTS(void) out)
{
    hasher->set_seed (seed);
    hasher->Hash_Begin();
    if (len > 0)
    {
      hasher->Compute_Raw ((P_ELEMENTS(uint8_t) )buffer, 1, len);
    }
    hasher->Hash_End();
    *((P_INSTANCE(uint32_t)) out) = hasher->get_value();
}

void Hasherproc64(P_INSTANCE(HasherClass64) hasher, P_INSTANCE(const void) buffer, int32_t len, uint32_t seed, P_ELEMENTS(void) out)
{
    hasher->set_seed (seed);
    hasher->Hash_Begin();
    if (len>0)
    {
      hasher->Compute_Raw ((P_ELEMENTS(uint8_t) )buffer, 1, len);
    }
    hasher->Hash_End();
    *((P_INSTANCE(uint64_t)) out) = hasher->get_value();
}

P_INSTANCE(HasherClass)  Factory_Get(utf8_string_const name)
{
    P_INSTANCE(HasherClass)  R = HasherFactory_Get (name);
    if (R == nullptr) return R;
    R->set_endian (true);
    return R;
}

P_INSTANCE(HasherClass32)  myPRNG32 = (P_INSTANCE(HasherClass32) ) Factory_Get ("PRNG:32");
P_INSTANCE(HasherClass64)  myPRNG64 = (P_INSTANCE(HasherClass64) ) Factory_Get ("PRNG:64");


void SMHasher_Wrapper_Hasher_PRNG32(P_INSTANCE(const void) buffer, int32_t len, uint32_t seed, P_ELEMENTS(void) out)
{
    Hasherproc32 (myPRNG32, buffer, len, seed, out);
}

void SMHasher_Wrapper_Hasher_PRNG64(P_INSTANCE(const void) buffer, int32_t len, uint32_t seed, P_ELEMENTS(void) out)
{
    Hasherproc64 (myPRNG64, buffer, len, seed, out);
}

P_INSTANCE(HasherClass32)  myUnderCrystalCatalystB32 = (P_INSTANCE(HasherClass32) ) Factory_Get ("Buffered:UnderCrystalCatalyst:32");
P_INSTANCE(HasherClass64)  myUnderCrystalCatalystB64 = (P_INSTANCE(HasherClass64) ) Factory_Get ("Buffered:UnderCrystalCatalyst:64");


void SMHasher_Wrapper_Hasher_UnderCrystalCatalystB32(P_INSTANCE(const void) buffer, int32_t len, uint32_t seed, P_ELEMENTS(void) out)
{
    Hasherproc32 (myUnderCrystalCatalystB32, buffer, len, seed, out);

}

void SMHasher_Wrapper_Hasher_UnderCrystalCatalystB64(P_INSTANCE(const void) buffer, int32_t len, uint32_t seed, P_INSTANCE(void) out)
{
    Hasherproc64 (myUnderCrystalCatalystB64, buffer, len, seed, out);
}
