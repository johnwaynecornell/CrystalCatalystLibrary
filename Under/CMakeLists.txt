cmake_minimum_required(VERSION 3.25)
project(UnderCrystalCatalyst)

set(CMAKE_CXX_STANDARD 17)

add_library(UnderCrystalCatalyst SHARED
)

add_library(UnderCrystalCatalyst_Static STATIC)

include(Under_Buffering/Under_Buffering.cmake)
include(Random/Random.cmake)
include(Hasher/Hasher.cmake)

function(DoAdd target)
    
    target_link_libraries(${target} PUBLIC FromSMHasher)

    target_include_directories(${target} PUBLIC ${CMAKE_CURRENT_LIST_DIR}/FromSMHasher)
    
    Under_Buffering_add_sources(${target})
    
    Random_add_sources(${target})
    
    Hasheradd_sources(${target})

    target_sources("${target}" PUBLIC
            utf8_string.cpp utf8_string.h SingleLink_Node.cpp SingleLink_Node.h)

endfunction()

DoAdd(UnderCrystalCatalyst)
DoAdd(UnderCrystalCatalyst_Static)

add_subdirectory(test)
add_subdirectory(FromSMHasher)
