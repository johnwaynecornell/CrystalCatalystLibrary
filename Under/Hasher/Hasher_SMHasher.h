// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#ifndef HASHER_HASHES_H
#define HASHER_HASHES_H
#include "HasherFactory.h"

void SMHasher_Wrapper_Hasher_PRNG32(P_INSTANCE(const void) buffer, int32_t len, uint32_t seed, P_INSTANCE(void) out);
void SMHasher_Wrapper_Hasher_PRNG64(P_INSTANCE(const void) buffer, int32_t len, uint32_t seed, P_INSTANCE(void) out);
void SMHasher_Wrapper_Hasher_UnderCrystalCatalystB32(P_INSTANCE(const void) buffer, int32_t len, uint32_t seed, P_INSTANCE(void) out);
void SMHasher_Wrapper_Hasher_UnderCrystalCatalystB64(P_INSTANCE(const void) buffer, int32_t len, uint32_t seed, P_INSTANCE(void) out);

#endif // HASHER_HASHES_H
