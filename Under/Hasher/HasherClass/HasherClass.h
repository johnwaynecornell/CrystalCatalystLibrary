// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#ifndef FARMHASH_HASHERCLASS_H
#define FARMHASH_HASHERCLASS_H

#include <cstdint>
#include <cstdlib>

class HasherClass
{
public:
    virtual ~HasherClass();

    typedef void (HasherClass::*Compute_fn) (P_ELEMENTS(uint8_t) m, size_t element_size, size_t element_count);

    void Compute_NillThrow(P_ELEMENTS(uint8_t) m, size_t element_size, size_t element_count);

    virtual void Compute_Rev(P_ELEMENTS(uint8_t) m, size_t element_size, size_t element_count) = 0;
    virtual void Compute_Raw(P_ELEMENTS(uint8_t) m, size_t element_size, size_t element_count) = 0;

    virtual int32_t get_bits() = 0;

    Compute_fn Under_Compute_Function = &HasherClass::Compute_NillThrow;

    void Compute(P_ELEMENTS(uint8_t) m, size_t element_size, size_t element_count);

    virtual void Initialize() = 0;

    virtual void set_endian(bool big);
    virtual bool get_endian();

    virtual void set_Swap(bool swap);
    virtual bool get_Swap();

    virtual ReturnBuffer get_identifier() = 0;

    // Optional: Set the seed value for hash functions that support seeding
    virtual void Hash_Begin();
    virtual void Hash_End();
private:
    bool m_swap;
};

extern "C"
{
_EXPORT_ uint32_t HasherClass_get_bits(P_INSTANCE(HasherClass) ctx);

_EXPORT_ ReturnBuffer HasherClass_get_identifier(P_INSTANCE(HasherClass) ctx);

_EXPORT_ void HasherClass_Compute(P_INSTANCE(HasherClass) ctx, P_ELEMENTS(uint8_t) m, size_t element_size, size_t element_count);
_EXPORT_ void HasherClass_Compute_Raw(P_INSTANCE(HasherClass) ctx, P_ELEMENTS(uint8_t) m, size_t element_size, size_t element_count);
_EXPORT_ void HasherClass_Compute_Rev(P_INSTANCE(HasherClass) ctx, P_ELEMENTS(uint8_t) m, size_t element_size, size_t element_count);

_EXPORT_ void HasherClass_set_endian(P_INSTANCE(HasherClass) ctx, bool big);
_EXPORT_ bool HasherClass_get_endian(P_INSTANCE(HasherClass) ctx);
_EXPORT_ void HasherClass_set_Swap(P_INSTANCE(HasherClass) ctx, bool swap);
_EXPORT_ bool HasherClass_get_Swap(P_INSTANCE(HasherClass) ctx);

_EXPORT_ void HasherClass_Hash_Begin(P_INSTANCE(HasherClass) ctx);
_EXPORT_ void HasherClass_Hash_End(P_INSTANCE(HasherClass) ctx);

_EXPORT_ void HasherClass_delete(P_INSTANCE(HasherClass) ctx);
}


#endif //FARMHASH_HASHERCLASS_H
