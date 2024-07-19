// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include "Random_Generator.h"

Random_Generator::Random_Generator()
{

}

Random_Generator::Random_Generator(uint32_t seed)
{
    SetSeed(seed);
}

void Random_Generator::SetSeed(uint32_t seed)
{
    ByesRemain = 0;
}

uint8_t Random_Generator::GetByte()
{
    if (ByesRemain == 0)
    {
        ByteRegister = Get_uint64_t();
        ByesRemain = 4;
    }

    ByesRemain--;
    uint8_t R = (uint8_t)(ByteRegister & 0xFF);
    ByteRegister >>= 8;

    return R;
}