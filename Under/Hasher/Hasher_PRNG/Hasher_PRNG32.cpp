// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include "../../Under.h"
#include "Hasher_PRNG32.h"

Hasher_PRNG32::Hasher_PRNG32()
{

}

Hasher_PRNG32::~Hasher_PRNG32()
{

}

ReturnBuffer Hasher_PRNG32::get_identifier()
{
    return ReturnBuffer_Static_String("PRNG:32");
}

void Hasher_PRNG32::Compute_Raw(P_ELEMENTS(uint8_t) m, size_t element_size, size_t element_count)
{
    int32_t val;

    size_t len = element_size * element_count;

    for (int32_t i=0; i<len; i++)
    {
        ProcessByte(m[i]);
    }
}

void Hasher_PRNG32::Compute_Rev(P_ELEMENTS(uint8_t) m, size_t element_size, size_t element_count)
{
    while (element_count > 0) {
        for (int64_t i = (int64_t) element_size - 1; i >= 0; i--) {

            ProcessByte(m[i]);
        }
        element_count--;
        m += element_size;
    }
}

void Hasher_PRNG32::set_seed(uint32_t seed)
{
    HasherClass32::set_seed(seed);
}

void Hasher_PRNG32::Hash_Begin()
{
    if (m_seed != m_lastseed)
    {
        for (int32_t i=0; i<table_size; i++)
        {
            rbyte[i].SetSeed (m_seed+i);
        }
        m_lastseed = m_seed;

    } else {
        for (int32_t i = 0; i < table_size; i++) {
            rbyte[i].Reset();
        }
    }

    m_code = rbyte[0].Get_uint32_t();
}

