#ifndef FARMHASH_HASHER_PRNG64_H
#define FARMHASH_HASHER_PRNG64_H


#include "../HasherClass/HasherClass64.h"
#include "../../Random/Random_MT19937.h"

class Hasher_PRNG64 : public HasherClass64 {
    static const int32_t table_bits = 0;
    static const int32_t table_size = 1<<(table_bits);
    static const int32_t table_mask = table_size - 1;

    Random_MT19937 rbyte[table_size];

public:
    inline void Shuffle()
    {
        m_code = (m_code << 7) + (m_code >> (64 - 7));
    }

    inline void ProcessByte(uint8_t b) {
        int32_t val;

        if (table_size == 1) {
            for (int32_t bit = 0; bit < 7; bit ++) {

                val = b & 1;

                m_code ^= rbyte[0].Get_uint64_t();
                if (val) m_code ^= rbyte[0].Get_uint64_t();
                Shuffle();

                b >>= table_bits;
            }

            val = b & table_mask;

            m_code ^= rbyte[0].Get_uint64_t();
            if (val) m_code ^= rbyte[0].Get_uint64_t();
            Shuffle();
        } else {
            for (int32_t bit = 0; bit < 8 - table_bits; bit += table_bits) {
                val = b & table_mask;

                m_code ^= rbyte[val].Get_uint64_t();
                Shuffle();
                b >>= table_bits;
            }

            val = b & table_mask;

            m_code ^= rbyte[val].Get_uint64_t();
            Shuffle();
        }
    }

    Hasher_PRNG64();
    ~Hasher_PRNG64() override;

    ReturnBuffer get_identifier() override;

    void Compute_Raw(P_ELEMENTS(uint8_t) m, size_t element_size, size_t element_count) override;
    void Compute_Rev(P_ELEMENTS(uint8_t) m, size_t element_size, size_t element_count) override;

    void set_seed(uint64_t seed) override;
    void Hash_Begin() override;
};

#endif //FARMHASH_HASHER_PRNG64_H
