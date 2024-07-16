#ifndef FARMHASH_HASHER_BUFFEREDFORCALLBACK32_H
#define FARMHASH_HASHER_BUFFEREDFORCALLBACK32_H

#include "../HasherClass/HasherClass32.h"
#include "../../Under_Buffering/BufferHelper.h"

class Hasher_BufferedForCallback32 : public HasherClass32 {

public:

    P_INSTANCE(BufferHelper)  MyBuffer = nullptr;
    typedef uint32_t (*Hash_fn)(P_ELEMENTS(const uint8_t)dta, size_t size);

    Hash_fn Under_HashFunction;

    utf8_string_const hashFunctionName;

    Hasher_BufferedForCallback32(utf8_string_const hashFunctionName, Hash_fn HashFunction, size_t buffersize);
    ~Hasher_BufferedForCallback32() override;

    ReturnBuffer get_identifier() override;

    void Compute_Rev(P_ELEMENTS(uint8_t) m, size_t element_size, size_t element_count) override;
    void Compute_Raw(P_ELEMENTS(uint8_t) m, size_t element_size, size_t element_count) override;

    void Under_Hash(P_ELEMENTS(const uint8_t)m, size_t len);

    void set_seed(uint32_t seed) override;
    void Hash_Begin() override;
    void Hash_End() override;
};


#endif //FARMHASH_HASHER_BUFFEREDFORCALLBACK32_H
