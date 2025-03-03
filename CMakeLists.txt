 cmake_minimum_required(VERSION 3.25)

set(CMAKE_C_COMPILER "clang")
set(CMAKE_CXX_COMPILER "clang++")
set(CMAKE_CXX_STANDARD 17)

set(CRYSTAL_FUNDAMENTALS_VERSION_MAJOR 0)
set(CRYSTAL_FUNDAMENTALS_VERSION_MINOR 0)
set(CRYSTAL_FUNDAMENTALS_VERSION_BUILD 1)

project(CrystalCatalyst VERSION "${CRYSTAL_FUNDAMENTALS_VERSION_MAJOR}.${CRYSTAL_FUNDAMENTALS_VERSION_MINOR}.${CRYSTAL_FUNDAMENTALS_VERSION_BUILD}")
include(PlatformConfig.cmake)

initPlatformConfig()
 include($ENV{NewAge}/include/JWCEssentials/JWCEssentials.cmake)

 add_library(CrystalCatalystLibrary SHARED
 )

 include(Platform/${CRYSTAL_PLATFORM}/Platform.cmake)

 target_sources(CrystalCatalystLibrary PUBLIC
     CrystalCatalystLibrary.cpp
         include/CrystalCatalystLibrary/CrystalCatalystLibrary.h
     Synchronization/CrystalSync.cpp
         include/CrystalCatalystLibrary/Synchronization/CrystalSync.h
     Windowing/CrystalWindow.cpp
         include/CrystalCatalystLibrary/Windowing/CrystalWindow.h
     Windowing/Pixels.cpp
         include/CrystalCatalystLibrary/Windowing/Pixels.h
     Windowing/DataInterchange.cpp
         include/CrystalCatalystLibrary/Windowing/DataInterchange.h
     Windowing/Clipboard.cpp
         include/CrystalCatalystLibrary/Windowing/Clipboard.h
     Windowing/DragDrop.cpp
         include/CrystalCatalystLibrary/Windowing/DragDrop.h

     Fonts/Fonts.cpp
         include/CrystalCatalystLibrary/Fonts.h

     Application.cpp
         include/CrystalCatalystLibrary/Application.h

     Platform.cpp
         include/CrystalCatalystLibrary/Platform.h

     ${PLATFORM_SOURCES})

 add_definitions(-DBUILD_CRYSTALCATALYSTLIBRARY)

set_target_output(CrystalCatalystLibrary ${CRYSTAL_FUNDAMENTALS_VERSION_MAJOR} ${CRYSTAL_FUNDAMENTALS_VERSION_MINOR} ${CRYSTAL_FUNDAMENTALS_VERSION_BUILD})

find_library(LIBJWCEssentials JWCEssentials)
target_link_libraries(CrystalCatalystLibrary PUBLIC ${LIBJWCEssentials})

add_subdirectory(Test)
add_subdirectory(Tools/clipboard_html)
add_subdirectory(Tools/htmlify_clipboard)

target_compile_options(CrystalCatalystLibrary PUBLIC "-Wno-return-type-c-linkage")

 shuttle(CrystalCatalystLibrary lib)
 