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

 file(READ "${CMAKE_CURRENT_SOURCE_DIR}/../Project/CrystalCatalystLibrary.net/CrystalCatalystLibrary.net/KeyCode.cs" MANAGED_KEYCODE_CONTRACT)
 set_property(DIRECTORY APPEND PROPERTY CMAKE_CONFIGURE_DEPENDS
     "${CMAKE_CURRENT_SOURCE_DIR}/../Project/CrystalCatalystLibrary.net/CrystalCatalystLibrary.net/KeyCode.cs")
 configure_file(keycode_contract.h.in keycode_contract.h @ONLY)
 add_executable(test_keycodes test_keycodes.cpp)
 target_include_directories(test_keycodes PRIVATE "${CMAKE_CURRENT_BINARY_DIR}")
 target_link_libraries(test_keycodes PUBLIC CrystalCatalystLibrary ${LIBJWCEssentials})
 if(MSVC)
     target_compile_options(test_keycodes PRIVATE /UNDEBUG)
 else()
     target_compile_options(test_keycodes PRIVATE -UNDEBUG)
 endif()
