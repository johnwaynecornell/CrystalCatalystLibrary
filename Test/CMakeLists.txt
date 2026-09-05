 cmake_minimum_required(VERSION 3.25)

 set(CMAKE_CXX_STANDARD 17)

 include( "$ENV{NewAge}/include/JWCEssentials/JWCEssentials.cmake")

 add_executable(test_client
         test_client.cpp
 )

 add_executable(test_multi_thread
         test_multi_thread.cpp
 )

 find_library(LIBJWCEssentials JWCEssentials)
 find_package(Threads)

 target_link_libraries(test_client PUBLIC CrystalCatalystLibrary ${LIBJWCEssentials})
 target_link_libraries(test_multi_thread PUBLIC CrystalCatalystLibrary ${LIBJWCEssentials} ${CMAKE_THREAD_LIBS_INIT})
