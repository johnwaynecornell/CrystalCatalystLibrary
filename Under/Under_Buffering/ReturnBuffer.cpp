// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include "../Under.h"

void ReturnBuffer_Deallocate(ReturnBuffer Buffer)
{
    Buffer.UnderDeallocate_elements(Buffer);
    Buffer.UnderDeallocate_memory(Buffer);
}

void ReturnBuffer_NillDeallocate(ReturnBuffer B) {
    throw std::runtime_error("Deallocation function not set! Potential memory leak.");
}

void ReturnBuffer_NonDeallocate(ReturnBuffer B) {

}

ReturnBuffer::operator bool() {
    return memory != nullptr;
}
