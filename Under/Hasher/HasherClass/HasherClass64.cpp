#include "../../Under.h"

HasherClass64::HasherClass64()
{
    m_seed = 0;
    m_code = 0;
}

void HasherClass64::Initialize()
{
    set_seed(0);
    set_endian(true);
}

int32_t HasherClass64::get_bits()
{
    return 64;
}

void HasherClass64::set_seed(uint64_t seed) {
    this->m_seed = seed;
}

uint64_t HasherClass64::get_seed() {
    return this->m_seed;
}

void HasherClass64::Hash_Begin()
{
    m_code = m_seed;
}

uint64_t HasherClass64::get_value()
{
    return m_code;
}

void HasherClass64_set_seed(P_INSTANCE(HasherClass64) ctx, uint64_t seed)
{
    ctx->set_seed(seed);
}

uint64_t HasherClass64_get_seed(P_INSTANCE(HasherClass64) ctx)
{
    return ctx->get_seed();
}


uint64_t HasherClass64_get_value(P_INSTANCE(HasherClass64) ctx)
{
    return ctx->get_value();
}
