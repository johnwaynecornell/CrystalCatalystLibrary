#include "../../Under.h"
#include "Hasher_BufferedForCallback64.h"
#include "../../SingleLink_Node.h"


void BufferedForCallback64_buffer_callback(P_INSTANCE(void) This, P_ELEMENTS(uint8_t) buffer, size_t length)
{
    ((P_INSTANCE(Hasher_BufferedForCallback64)) This)->Under_Hash(buffer, length);
}

Hasher_BufferedForCallback64::Hasher_BufferedForCallback64(utf8_string_const hashFunctionName, Hash_fn HashFunction, size_t buffersize)
{
    MyBuffer = new BufferHelper(this, buffersize,
                                BufferedForCallback64_buffer_callback);
    this->Under_HashFunction = HashFunction;
    this->hashFunctionName = hashFunctionName;
}

Hasher_BufferedForCallback64::~Hasher_BufferedForCallback64()
{

}

ReturnBuffer Hasher_BufferedForCallback64::get_identifier() {
    SingleLink_Node<utf8_string_const> dummy;
    P_INSTANCE(SingleLink_Node<utf8_string_const>) cur = &dummy;

    cur = cur->set_next("Buffered:");
    cur = cur->set_next(hashFunctionName);
    cur = cur->set_next(":64");

    size_t sz = 0;

    for (cur = dummy.next; cur != nullptr; cur = cur->next) {
        size_t s = strlen(cur->value);
        cur->tag = (P_INSTANCE(void) ) s;
        sz += s;
    }

    sz++;
    utf8_string R = new utf8_char[sz];

    utf8_string O = R;

    size_t running_size = sz;

    SingleLink_Node<utf8_string_const> *next;
    for (cur = dummy.next; cur != nullptr; cur = next) {
        next = cur->next;

        if (!utf8_string_copy(cur->value, (size_t)(cur->tag) + 1, O, running_size, &O, &running_size)) {
            throw std::runtime_error("MEMORY DAMAGED");
        }

        delete cur;
    }

    return ReturnBuffer_Allocated_String(R, sz);
}

void Hasher_BufferedForCallback64::Compute_Rev(P_ELEMENTS(uint8_t) m, size_t element_size, size_t element_count)
{
    MyBuffer->Process_Swap(m, element_size, element_count);
}

void Hasher_BufferedForCallback64::Compute_Raw(P_ELEMENTS(uint8_t) m, size_t element_size, size_t element_count)
{
    MyBuffer->Process(m, element_size, element_count);
}

void Hasher_BufferedForCallback64::Under_Hash(const P_ELEMENTS(uint8_t) m, size_t len)
{
    //uint64_t hash1 = m_code;
    uint64_t hash2 = Under_HashFunction(m, len);

    m_code = hash2;
    (this->*HasherClass::Under_Compute_Function) ((P_ELEMENTS(uint8_t) ) &m_code, sizeof m_code, 1);

    /*
    uint64_t tmp;

    const uint64_t prime = 0x01000193; // FNV prime
    uint64_t hash = 0x811c9dc5;       // FNV offset basis
    hash = (hash ^ hash1);
    tmp = hash * (uint64_t) prime;
    hash = (uint64_t)((tmp>>64)+tmp);

    tmp = (hash ^ hash2) * (uint64_t) prime;
    m_code = (uint64_t)((tmp>>64)+tmp);
    */
    //P_ELEMENTS(uint8_t) hash_2 = hash+sizeof(uint64_t);


    /*
    uint8_t hash[sizeof(uint64_t) * 2];

    for (int32_t i=0; i<sizeof(int64_t); i++)
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

void Hasher_BufferedForCallback64::set_seed(uint64_t seed)
{
    HasherClass64::set_seed(seed);
}

void Hasher_BufferedForCallback64::Hash_Begin()
{
    m_code = m_seed;
    MyBuffer->buffer_position = 0;
    (this->*HasherClass::Under_Compute_Function) ((P_ELEMENTS(uint8_t) ) &m_code, sizeof m_code, 1);
}

void Hasher_BufferedForCallback64::Hash_End()
{
    MyBuffer->flush();
}
