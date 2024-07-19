// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include "../Under.h"
#include "HasherClass.h"

#include <stdexcept>

HasherClass::~HasherClass()
{

}

void HasherClass::Compute_NillThrow(P_ELEMENTS(uint8_t) m, size_t element_size, size_t element_count)
{
    throw std::logic_error("Compute_NillThrow was called because either set_endian or set_swap must be called before a HasherClass can be used.");
}

void HasherClass::set_endian(bool big)
{
    uint32_t a = 1;
    set_Swap((*((P_ELEMENTS(uint8_t) )&a) != 1) == big);
}

bool HasherClass::get_endian()
{
    uint32_t a = 1;
    return m_swap == (*((P_ELEMENTS(uint8_t) )&a) != 1);
}

void HasherClass::set_Swap(bool swap)
{
    m_swap = swap;
    Under_Compute_Function = swap ? &HasherClass::Compute_Rev : &HasherClass::Compute_Raw;
}

bool HasherClass::get_Swap()
{
    return m_swap;
}

void HasherClass::Compute(P_ELEMENTS(uint8_t) m, size_t element_size, size_t element_count)
{
    (this->* HasherClass::Under_Compute_Function)(m, element_size, element_count);
}

// Optional: Set the seed value for hash functions that support seeding
void HasherClass::Hash_Begin()
{

}

void HasherClass::Hash_End()
{

}

void HasherClass_Initialize(P_INSTANCE(HasherClass) ctx)
{
    ctx->Initialize();
}


uint32_t HasherClass_get_bits(P_INSTANCE(HasherClass) ctx)
{
    return ctx->get_bits();
}

ReturnBuffer HasherClass_get_identifier(P_INSTANCE(HasherClass) ctx)
{
    return ctx->get_identifier();
}

void HasherClass_Compute(P_INSTANCE(HasherClass) ctx, P_ELEMENTS(uint8_t) m, size_t element_size, size_t element_count)
{
    (ctx->* ctx->HasherClass::Under_Compute_Function)(m, element_size, element_count);
}
void HasherClass_Compute_Raw(P_INSTANCE(HasherClass) ctx, P_ELEMENTS(uint8_t) m, size_t element_size, size_t element_count)
{
    ctx->Compute_Raw(m, element_size, element_count);
}
void HasherClass_Compute_Rev(P_INSTANCE(HasherClass) ctx, P_ELEMENTS(uint8_t) m, size_t element_size, size_t element_count)
{
    ctx->Compute_Rev(m, element_size, element_count);
}

void HasherClass_set_endian(P_INSTANCE(HasherClass) ctx, bool big)
{
    ctx->set_endian(big);
}

bool HasherClass_get_endian(P_INSTANCE(HasherClass) ctx)
{
    return ctx->get_endian();
}

void HasherClass_set_Swap(P_INSTANCE(HasherClass) ctx, bool swap)
{
    ctx->set_Swap(swap);
}
bool HasherClass_get_Swap(P_INSTANCE(HasherClass) ctx)
{
    return ctx->get_Swap();
}

void HasherClass_Hash_Begin(P_INSTANCE(HasherClass) ctx)
{
    ctx->Hash_Begin();
}
void HasherClass_Hash_End(P_INSTANCE(HasherClass) ctx)
{
    ctx->Hash_End();
}

void HasherClass_delete(P_INSTANCE(HasherClass) ctx)
{
    delete ctx;
}