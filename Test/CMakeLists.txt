 cmake_minimum_required(VERSION 3.25)

 set(CMAKE_CXX_STANDARD 17)

 include( "$ENV{NewAge}/include/JWCEssentials/JWCEssentials.cmake")

 add_executable(test_client
         test_client.cpp
 )

 add_executable(test_multi_thread
         test_multi_thread.cpp
 )

 add_executable(test_data_interchange
         test_data_interchange.cpp
 )

 add_executable(test_window_lifecycle
         test_window_lifecycle.cpp
 )

 find_library(LIBJWCEssentials JWCEssentials)
 find_package(Threads)

 target_link_libraries(test_client PUBLIC CrystalCatalystLibrary ${LIBJWCEssentials})
 target_link_libraries(test_multi_thread PUBLIC CrystalCatalystLibrary ${LIBJWCEssentials} ${CMAKE_THREAD_LIBS_INIT})
 target_link_libraries(test_data_interchange PUBLIC CrystalCatalystLibrary ${LIBJWCEssentials} ${CMAKE_THREAD_LIBS_INIT})
 target_link_libraries(test_window_lifecycle PUBLIC CrystalCatalystLibrary ${LIBJWCEssentials} ${CMAKE_THREAD_LIBS_INIT})

 add_executable(test_clipboard_persistence test_clipboard_persistence.cpp)
 target_link_libraries(test_clipboard_persistence PUBLIC CrystalCatalystLibrary ${LIBJWCEssentials})
 # Assertions are the native regression contract, including Release builds.
 if(MSVC)
     target_compile_options(test_clipboard_persistence PRIVATE /UNDEBUG)
 else()
     target_compile_options(test_clipboard_persistence PRIVATE -UNDEBUG)
 endif()
