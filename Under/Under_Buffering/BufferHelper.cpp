// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include "../Under.h"

BufferHelper::BufferHelper(P_INSTANCE(void) This, size_t bytes, buffer_callback callback)
{
    this->callback = callback;
    buffer = new uint8_t[bytes];
    buffer_len = bytes;
    buffer_position = 0;
    this->This = This;
}

void BufferHelper::Process_Swap(P_ELEMENTS(uint8_t) mem, size_t element_size, size_t element_count)
{
    P_ELEMENTS(uint8_t) buf = buffer + buffer_position;
    P_ELEMENTS(uint8_t) end = buffer + buffer_len;

    for (int64_t e=0; e < (int64_t) element_count; e++)
    {
        for (int64_t b = (int64_t) element_size-1; b>=0; b--)
        {
            *buf = mem[b];
            buf++;
            if (buf == end)
            {
                flush();
                buf = buffer;
            }
        }

        mem += element_size;
        buffer_position += element_size;
    }

}

void BufferHelper::Process(P_ELEMENTS(uint8_t) mem, size_t element_size, size_t element_count)
{
    size_t remain;
    size_t l;
    remain = buffer_len - buffer_position;

    size_t len = element_size * element_count;

    while ((int64_t) len > 0)
    {
        l = len;
        if (l>remain) l = remain;

        memcpy(buffer + buffer_position, mem, l);

        mem += l;
        len -= l;

        buffer_position += l;

        if (buffer_position == buffer_len)
        {
            flush();
        }

        remain = buffer_len - buffer_position;
    };
}

void BufferHelper::flush()
{
    if (buffer_position != 0)
    {
        size_t l = buffer_position;
        buffer_position = 0;

        callback(This, buffer, l);
    }
}
