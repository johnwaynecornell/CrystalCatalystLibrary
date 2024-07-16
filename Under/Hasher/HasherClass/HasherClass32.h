#ifndef HASHPRNG_OOHASHER_H
#define HASHPRNG_OOHASHER_H

#include <cstdint>
#include <cstdlib>
#include "HasherClass.h"

class HasherClass32 : public HasherClass{
public:
    // Default constructor
    HasherClass32();

    void Initialize() override;

    int32_t get_bits() override;

    // Optional: Set the seed value for hash functions that support seeding
    virtual void set_seed(uint32_t seed);
    virtual uint32_t get_seed();


    void Hash_Begin() override;
    virtual uint32_t get_value();

protected:
    uint32_t m_code;
    uint32_t m_seed;
    uint32_t m_lastseed = 0xFFFFFFFF;
};

extern "C"
{
_EXPORT_ void HasherClass32_set_seed(P_INSTANCE(HasherClass32) ctx, uint32_t seed);
_EXPORT_ uint32_t HasherClass32_get_seed(P_INSTANCE(HasherClass32) ctx);
_EXPORT_ uint32_t HasherClass32_get_value(P_INSTANCE(HasherClass32) ctx);
}


#endif //HASHPRNG_OOHASHER_H
