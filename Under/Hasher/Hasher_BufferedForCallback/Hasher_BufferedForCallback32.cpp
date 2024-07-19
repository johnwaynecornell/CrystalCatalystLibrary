// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include "../../Under.h"
#include "Hasher_BufferedForCallback32.h"

void BufferedForCallback32_buffer_callback(P_INSTANCE(void) This, P_ELEMENTS(uint8_t) buffer, size_t length)
{
    ((P_INSTANCE(Hasher_BufferedForCallback32)) This)->Under_Hash(buffer, length);
}

Hasher_BufferedForCallback32::Hasher_BufferedForCallback32(utf8_string_const hashFunctionName, Hash_fn HashFunction, size_t buffersize)
{
    MyBuffer = new BufferHelper(this, buffersize,
                                BufferedForCallback32_buffer_callback);
    this->Under_HashFunction = HashFunction;
    this->hashFunctionName = hashFunctionName;
}

Hasher_BufferedForCallback32::~Hasher_BufferedForCallback32()
{
}

ReturnBuffer Hasher_BufferedForCallback32::get_identifier()
{
    SingleLink_Node<utf8_string_const> dummy;
    P_INSTANCE(SingleLink_Node<utf8_string_const>)  cur = &dummy;



    cur = cur->set_next("Buffered:");
    cur = cur->set_next(hashFunctionName);
    cur = cur->set_next(":32");

    size_t sz = 0;

    for (cur = dummy.next; cur != nullptr; cur = cur->next)
    {
        size_t s = strlen(cur->value);
        cur->tag = (P_INSTANCE(void) ) s;
        sz += s;
    }

    sz++;
    utf8_string R = new utf8_char[sz];
    utf8_string O = R;
    size_t remain = sz;


    SingleLink_Node<utf8_string_const> *next;
    for (cur = dummy.next; cur != nullptr; cur = next)
    {
        next = cur->next;

        if (!utf8_string_copy(cur->value, ((size_t)cur->tag)+1, O, remain, &O, &remain)) {
            throw std::runtime_error("MEMORY DAMAGED");
        }

        delete cur;
    }

    return ReturnBuffer_Allocated_String(R, sz);
}

void Hasher_BufferedForCallback32::Compute_Rev(P_ELEMENTS(uint8_t) m, size_t element_size, size_t element_count)
{
    MyBuffer->Process_Swap(m, element_size, element_count);
}

void Hasher_BufferedForCallback32::Compute_Raw(P_ELEMENTS(uint8_t) m, size_t element_size, size_t element_count)
{
    MyBuffer->Process(m, element_size, element_count);
}

void Hasher_BufferedForCallback32::Under_Hash(P_ELEMENTS(const uint8_t)m, size_t len)
{
    //uint32_t hash1 = m_code;
    uint32_t hash2 = Under_HashFunction(m, len);

    m_code = hash2;
    (this->*HasherClass::Under_Compute_Function) ((P_ELEMENTS(uint8_t) ) &m_code, sizeof m_code, 1);

    /*
    uint64_t tmp;

    const uint32_t prime = 0x01000193; // FNV prime
    uint32_t hash = 0x811c9dc5;       // FNV offset basis
    hash = (hash ^ hash1);
    tmp = hash * (uint64_t) prime;
    hash = (uint32_t)((tmp>>32)+tmp);

    tmp = (hash ^ hash2) * (uint64_t) prime;
    m_code = (uint32_t)((tmp>>32)+tmp);
    */
    //P_ELEMENTS(uint8_t) hash_2 = hash+sizeof(uint32_t);


    /*
    uint8_t hash[sizeof(uint32_t) * 2];

    for (int32_t i=0; i<sizeof(int32_t); i++)
    {
        int32_t ii = i<<1;
        hash[ii] = (uint8_t) hash1;
        hash[ii+1] = (uint8_t) hash2;

        hash1>>=8;
        hash2>>=8;
    }

    m_code = Under_HashFunction((utf8_string )hash, sizeof(hash));
    */


}

void Hasher_BufferedForCallback32::set_seed(uint32_t seed)
{
    HasherClass32::set_seed(seed);
}

void Hasher_BufferedForCallback32::Hash_Begin()
{
    m_code = m_seed;
    MyBuffer->buffer_position = 0;
    (this->*HasherClass::Under_Compute_Function) ((P_ELEMENTS(uint8_t) ) &m_code, sizeof m_code, 1);
}

void Hasher_BufferedForCallback32::Hash_End()
{
    MyBuffer->flush();
}
