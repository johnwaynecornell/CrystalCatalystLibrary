#include "../../Under.h"
#include "Hasher_PRNG64.h"

Hasher_PRNG64::Hasher_PRNG64()
{

}

Hasher_PRNG64::~Hasher_PRNG64()
{

}

ReturnBuffer Hasher_PRNG64::get_identifier()
{
    return ReturnBuffer_Static_String("PRNG:64");
}


void Hasher_PRNG64::Compute_Raw(P_ELEMENTS(uint8_t) m, size_t element_size, size_t element_count)
{
    int32_t val;

    size_t len = element_size * element_count;

    for (int32_t i=0; i<len; i++)
    {
        ProcessByte(m[i]);
    }
}

void Hasher_PRNG64::Compute_Rev(P_ELEMENTS(uint8_t) m, size_t element_size, size_t element_count)
{
    while (element_count > 0) {
        for (int64_t i = (int64_t) element_size - 1; i >= 0; i--)
        {

            ProcessByte(m[i]);
        }
        element_count--;
        m += element_size;
    }
}

void Hasher_PRNG64::set_seed(uint64_t seed)
{
    HasherClass64::set_seed(seed);
}

void Hasher_PRNG64::Hash_Begin()
{
    if (m_seed != m_lastseed)
    {
        for (int32_t i=0; i<table_size; i++)
        {
            uint64_t s = m_seed+i;

            uint32_t a = (uint32_t)(s>>32);
            uint32_t b = (uint32_t)s;

            uint32_t _s = ((b<<17)+(b>>(32-17))) ^ ((a<<13)+(b>>(32-13)));

            rbyte[i].SetSeed (_s);
        }
        m_lastseed = m_seed;
    } else {

        for (int32_t i = 0; i < table_size; i++) {
            rbyte[i].Reset();
        }
    }

    m_code = rbyte[0].Get_uint64_t();
}
