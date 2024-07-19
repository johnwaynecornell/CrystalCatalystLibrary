// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#ifndef FARMHASH_HASHERCLASS64_H
#define FARMHASH_HASHERCLASS64_H

#include <cstdint>
#include <cstdlib>
#include "HasherClass.h"

class HasherClass64 : public HasherClass{
public:
    // Default constructor
    HasherClass64();
    void Initialize() override;


    int32_t get_bits() override;

    // Optional: Set the seed value for hash functions that support seeding
    virtual void set_seed(uint64_t seed);
    virtual uint64_t get_seed();

    void Hash_Begin() override;
    virtual uint64_t get_value();

protected:
    uint64_t m_code;
    uint64_t m_seed;
    uint64_t m_lastseed = 0xFFFFFFFFFFFFFFFFul;
};

extern "C"
{
_EXPORT_ uint64_t HasherClass64_get_seed(P_INSTANCE(HasherClass64) ctx);
_EXPORT_ void HasherClass64_set_seed(P_INSTANCE(HasherClass64) ctx, uint64_t seed);
_EXPORT_ uint64_t HasherClass64_get_value(P_INSTANCE(HasherClass64) ctx);
}


#endif //FARMHASH_HASHERCLASS64_H
