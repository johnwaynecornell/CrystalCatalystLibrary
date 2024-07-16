cmake_minimum_required(VERSION 3.25)
project(FromSMHasher C)

set(CMAKE_CXX_STANDARD 17)

add_library(FromSMHasher STATIC
        farmhash-c.h
        farmhash-c.c
)
