#include "../../Under.h"

HasherClass32::HasherClass32()
{
    m_seed = 0;
    m_code = 0;
}

void HasherClass32::Initialize()
{
    set_seed(0);
    set_endian(true);
}

int32_t HasherClass32::get_bits()
{
    return 32;
}

void HasherClass32::set_seed(uint32_t seed) {
    this->m_seed = seed;
}

uint32_t HasherClass32::get_seed()
{
    return m_seed;
}

void HasherClass32::Hash_Begin()
{
    m_code = m_seed;
}

uint32_t HasherClass32::get_value()
{
    return m_code;
}

void HasherClass32_set_seed(P_INSTANCE(HasherClass32) ctx, uint32_t seed)
{
    ctx->set_seed(seed);
}

uint32_t HasherClass32_get_seed(P_INSTANCE(HasherClass32) ctx)
{
    return ctx->get_seed();
}


uint32_t HasherClass32_get_value(P_INSTANCE(HasherClass32) ctx)
{
    return ctx->get_value();
}