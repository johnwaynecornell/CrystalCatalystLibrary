// MIT License
// Copyright (c) 2024 John W. Cornell
// See LICENSE file in the project root for full license information.
#ifndef UTF8_STRING_H
#define UTF8_STRING_H

typedef char utf8_char;

typedef P_ELEMENTS(utf8_char) utf8_string ;
typedef P_ELEMENTS(const utf8_char) utf8_string_const;

bool utf8_string_copy(utf8_string_const src, size_t src_buffer_length_in_charachters,  utf8_string dest, size_t dest_buffer_length_in_charachters, P_OUT(utf8_string) end_ptr = nullptr, P_OUT(size_t) dest_buffer_remain = nullptr);

utf8_string_const utf8_string_dup(utf8_string_const string);
void utf8_string_replace_dup(utf8_string_const text, P_IN_OUT(utf8_string_const) target);

#endif //UTF8_STRING_H
