// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#ifndef HASHPRNG_HASHPRNG32_H
#define HASHPRNG_HASHPRNG32_H

#include "../HasherClass/HasherClass32.h"
#include "../../Random/Random_MT19937.h"

class Hasher_PRNG32 : public HasherClass32 {
  static const int32_t table_bits = 1;
  static const int32_t table_size = 1<<(table_bits);
  static const int32_t table_mask = table_size - 1;

  Random_MT19937 rbyte[table_size];

public:
    inline void Shuffle()
    {
        m_code = (m_code << 7) + (m_code >> (32 - 7));
    }

    inline void ProcessByte(uint8_t b) {
        int32_t val;

        if (table_size == 1) {
            for (int32_t bit = 0; bit < 7; bit ++) {

                val = b & 1;

                m_code ^= rbyte[0].Get_uint32_t();
                if (val) m_code ^= rbyte[0].Get_uint32_t();
                Shuffle();

                b >>= table_bits;
            }

            val = b & 1;

            m_code ^= rbyte[0].Get_uint32_t();
            if (val) m_code ^= rbyte[0].Get_uint32_t();
            Shuffle();
        } else {
            for (int32_t bit = 0; bit < 8 - table_bits; bit += table_bits) {
                val = b & table_mask;

                m_code ^= rbyte[val].Get_uint32_t();
                Shuffle();
                b >>= table_bits;
            }

            val = b & table_mask;

            m_code ^= rbyte[val].Get_uint32_t();
            Shuffle();
        }
    }

    Hasher_PRNG32();
    ~Hasher_PRNG32() override;

    ReturnBuffer get_identifier() override;

    void Compute_Raw(P_ELEMENTS(uint8_t) m, size_t element_size, size_t element_count) override;
    void Compute_Rev(P_ELEMENTS(uint8_t) m, size_t element_size, size_t element_count) override;

    void set_seed(uint32_t seed) override;
    void Hash_Begin() override;


};

#endif //HASHPRNG_HASHPRNG32_H
