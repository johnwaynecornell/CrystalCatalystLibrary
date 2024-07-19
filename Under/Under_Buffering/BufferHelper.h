// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#ifndef FARMHASH_BUFFERHELPER_H
#define FARMHASH_BUFFERHELPER_H


#include <cstdio>
#include <cstring>
#include <cstdint>

class BufferHelper {

public:
    P_ELEMENTS(uint8_t) buffer;
    size_t buffer_len;
    size_t buffer_position;

    typedef void (*buffer_callback)(P_INSTANCE(void) This, P_ELEMENTS(uint8_t) buffer, size_t buffer_len);

    buffer_callback callback;
    P_INSTANCE(void) This;

    BufferHelper(P_INSTANCE(void) This, size_t bytes, buffer_callback callback);
    virtual void Process_Swap(P_ELEMENTS(uint8_t) mem, size_t element_size, size_t element_count);
    virtual void Process(P_ELEMENTS(uint8_t) mem, size_t element_size, size_t element_count);
    virtual void flush();
};


#endif //FARMHASH_BUFFERHELPER_H
