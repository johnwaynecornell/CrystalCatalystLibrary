#ifndef FARMHASH_BUFFER_RETURN_H
#define FARMHASH_BUFFER_RETURN_H

#include <cstdlib>
#include <cstring>
#include <stdexcept>

extern "C"
{

struct ReturnBuffer {
    P_ELEMENTS(void) memory;
    size_t element_size;
    size_t element_count;

    void (*UnderDeallocate_memory)(ReturnBuffer);
    void (*UnderDeallocate_elements)(ReturnBuffer);

    operator bool();
};

_EXPORT_ void ReturnBuffer_Deallocate(ReturnBuffer Buffer);
_EXPORT_ void ReturnBuffer_NillDeallocate(ReturnBuffer B);
_EXPORT_ void ReturnBuffer_NonDeallocate(ReturnBuffer B);

}

template<typename T>
static void ReturnBuffer_Delete_memory(ReturnBuffer B) {
    delete (P_ELEMENTS(T) ) B.memory;
}

template<typename T>
static void ReturnBuffer_Delete_array(ReturnBuffer B) {
    delete[] (P_ELEMENTS(T) ) B.memory;
}

template<typename T>
static void ReturnBuffer_Delete_elements(ReturnBuffer B) {
    for (size_t element = 0; element < B.element_count; element++) {
        delete ((P_ELEMENTS(T) ) B.memory)[element];
    }
}

template<typename T>
static void ReturnBuffer_Delete_elements_array(ReturnBuffer B) {
    for (size_t element = 0; element < B.element_count; element++) {
        delete[] ((P_ELEMENTS(T) ) B.memory)[element];
    }
}


template<typename T>
void ReturnBuffer_Configure(ReturnBuffer &R, P_ELEMENTS(const T) memory, bool memory_allocated = false, bool memory_is_array = false, size_t count = 1) {
    R.memory = (P_ELEMENTS(void) ) memory;

    R.element_size = sizeof(T);
    R.element_count = count;

    R.UnderDeallocate_memory = memory_allocated ?
                                   (memory_is_array ? ReturnBuffer_Delete_array<T> : ReturnBuffer_Delete_memory<T>)
                                                    : ReturnBuffer_NonDeallocate;

    R.UnderDeallocate_elements = ReturnBuffer_NonDeallocate;
}

template<typename T>
static ReturnBuffer ReturnBuffer_StaticMemory(P_ELEMENTS(const T) memory) {
    ReturnBuffer R;
    ReturnBuffer_Configure(R, memory);
    return R;
}

template<typename T>
static ReturnBuffer ReturnBuffer_AllocatedMemory(P_ELEMENTS(const T) memory) {
    ReturnBuffer R;
    ReturnBuffer_Configure(R, memory, true);
    return R;
}


template<typename T>
static ReturnBuffer ReturnBuffer_StaticArray_StaticElements(P_ELEMENTS(const T) array, size_t element_count) {
    ReturnBuffer R;
    ReturnBuffer_Configure(R, array, false, true, element_count);
    return R;
}

template<typename T>
static ReturnBuffer ReturnBuffer_AllocatedArray_StaticElements(P_ELEMENTS(const T) array, size_t element_count) {
    ReturnBuffer R;
    ReturnBuffer_Configure(R, array, true, true, element_count);
    return R;
}

template<typename T>
static ReturnBuffer ReturnBuffer_AllocatedArray_AllocatedElements(P_ELEMENTS(const T) array, size_t element_count) {
    ReturnBuffer R;
    ReturnBuffer_Configure(R, array, true, true, element_count);

    R.UnderDeallocate_elements = ReturnBuffer_Delete_elements<T>;

    return R;
}

template<typename T>
static ReturnBuffer ReturnBuffer_AllocatedArray_AllocatedElementsAreArrays(P_ELEMENTS(const T) array, size_t element_count) {
    ReturnBuffer R;
    ReturnBuffer_Configure(R, array, true, true, element_count, true);
    R.UnderDeallocate_elements = ReturnBuffer_Delete_elements_array<T>;

    return R;
}

template<typename T>
static ReturnBuffer ReturnBuffer_StaticArray_AllocatedElements(P_ELEMENTS(const T) array, size_t element_count) {
    ReturnBuffer R;
    ReturnBuffer_Configure(R, array, false, true, element_count);
    R.UnderDeallocate_elements = ReturnBuffer_Delete_elements<T>;

    return R;
}

template<typename T>
static ReturnBuffer ReturnBuffer_StaticArray_AllocatedElementsAreArrays(P_ELEMENTS(const T) array, size_t element_count) {
    ReturnBuffer R;
    ReturnBuffer_Configure(R, array, false, true, element_count);

    R.UnderDeallocate_elements = ReturnBuffer_Delete_elements_array<T>;

    return R;
}

static ReturnBuffer ReturnBuffer_Static_String(utf8_string_const string) {
    size_t len = strlen(string) + 1;
    ReturnBuffer R;
    ReturnBuffer_Configure(R, string, false, true, len);
    return R;
}

static ReturnBuffer ReturnBuffer_Allocated_String(utf8_string_const string) {
    size_t len = strlen(string) + 1;
    ReturnBuffer R;
    ReturnBuffer_Configure(R, string, true, true, len);
    return R;
}

static ReturnBuffer ReturnBuffer_Static_String(utf8_string_const string, size_t len) {
    ReturnBuffer R;
    ReturnBuffer_Configure(R, string, false, true, len);
    return R;
}

static ReturnBuffer ReturnBuffer_Allocated_String(utf8_string_const string, size_t len) {
    ReturnBuffer R;
    ReturnBuffer_Configure(R, string, true, true, len);
    return R;
}

#endif //FARMHASH_BUFFER_RETURN_H
