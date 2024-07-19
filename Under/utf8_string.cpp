// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#include "Under.h"

bool utf8_string_copy(utf8_string_const src, size_t src_buffer_length_in_charachters,  utf8_string dest, size_t dest_buffer_length_in_charachters, P_OUT(utf8_string) dest_accum, P_OUT(size_t) dest_buffer_remain)
{
    bool rc = true;
    size_t max_buffer = dest_buffer_length_in_charachters - 1;

    if (src_buffer_length_in_charachters != -1) {
        if (src_buffer_length_in_charachters - 1 < max_buffer) max_buffer = src_buffer_length_in_charachters - 1;
    }


    int i;
    for (i=0; i< max_buffer && src[i] != 0; i++) {
        dest[i] = src[i];
    }

    dest[i] = 0;

    if (i == dest_buffer_length_in_charachters -1 && src[i] != 0) rc = false;

    if (dest_accum != nullptr) *dest_accum = dest + i;
    if (dest_buffer_remain != nullptr) *dest_buffer_remain = dest_buffer_length_in_charachters - i;

    return rc;
}

utf8_string_const utf8_string_dup(utf8_string_const string) {
    if (string == nullptr) return nullptr;

    size_t l = strlen(string)+1;

    utf8_string R = (utf8_string ) new utf8_char[l];
    if (!utf8_string_copy(string, -1, R, l))
        throw std::runtime_error("MEMORY DAMAGED");

    return (utf8_string_const ) R;
}

void utf8_string_replace_dup(utf8_string_const text, P_IN_OUT(utf8_string_const) target) {
    if (*target != nullptr) {
        free((utf8_string ) *target);
        *target = nullptr;
    }

    if (text != nullptr) *target = (utf8_string_const ) utf8_string_dup(text);
}

