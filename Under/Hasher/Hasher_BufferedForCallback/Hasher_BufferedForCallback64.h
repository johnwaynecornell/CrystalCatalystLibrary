#ifndef FARMHASH_HASHER_BUFFEREDFORCALLBACK64_H
#define FARMHASH_HASHER_BUFFEREDFORCALLBACK64_H

#include "../HasherClass/HasherClass64.h"
#include "../../Under_Buffering/BufferHelper.h"

class Hasher_BufferedForCallback64 : public HasherClass64 {

public:

    P_INSTANCE(BufferHelper)  MyBuffer = nullptr;
    typedef uint64_t (*Hash_fn)(P_ELEMENTS(const uint8_t)dta, size_t size);

    Hash_fn Under_HashFunction;

    utf8_string_const hashFunctionName;

    Hasher_BufferedForCallback64(utf8_string_const hashFunctionName, Hash_fn HashFunction, size_t buffersize);
    ~Hasher_BufferedForCallback64() override;

    ReturnBuffer get_identifier() override;

    void Compute_Rev(P_ELEMENTS(uint8_t) m, size_t element_size, size_t element_count) override;
    void Compute_Raw(P_ELEMENTS(uint8_t) m, size_t element_size, size_t element_count) override;

    void Under_Hash(P_ELEMENTS(const uint8_t)m, size_t len);

    void set_seed(uint64_t seed) override;
    void Hash_Begin() override;
    void Hash_End() override;
};


#endif //FARMHASH_HASHER_BUFFEREDFORCALLBACK64_H
