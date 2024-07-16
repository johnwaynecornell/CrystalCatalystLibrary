#ifndef HASHPRNG_RANDOM_GENERATOR_H
#define HASHPRNG_RANDOM_GENERATOR_H

#include <cstdint>
#include <cstdlib>

class Random_Generator
{
public:

    Random_Generator();
    Random_Generator(uint32_t seed);
    virtual void SetSeed(uint32_t seed);
    virtual uint32_t Get_uint32_t() = 0;
    virtual uint64_t Get_uint64_t() = 0;
    virtual void Reset() = 0;

    uint32_t ByteRegister;
    int32_t ByesRemain;

    uint8_t GetByte();
};

#endif