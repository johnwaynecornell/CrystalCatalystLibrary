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

 add_library(CrystalCatalystLibrary SHARED
 )

 include(Platform/${CRYSTAL_PLATFORM}/Platform.cmake)

 target_sources(CrystalCatalystLibrary PUBLIC
     CrystalCatalystLibrary.cpp
     CrystalCatalystLibrary.h
     Synchronization/CrystalSync.cpp
     Synchronization/CrystalSync.h
     Windowing/CrystalWindow.cpp
     Windowing/CrystalWindow.h
     Windowing/Pixels.cpp
     Windowing/Pixels.h
     Windowing/DragDrop.cpp
     Windowing/DragDrop.h
     Fonts/Fonts.cpp
     Fonts/Fonts.h

     Application.cpp
     Application.h

     Platform.cpp
     Platform.h

     TLS.cpp
     TLS.h

     ErrorSystem.cpp
     ErrorSystem.h

     ${PLATFORM_SOURCES})

set_target_output(CrystalCatalystLibrary ${CRYSTAL_FUNDAMENTALS_VERSION_MAJOR} ${CRYSTAL_FUNDAMENTALS_VERSION_MINOR} ${CRYSTAL_FUNDAMENTALS_VERSION_BUILD})

target_link_libraries(CrystalCatalystLibrary PUBLIC UnderCrystalCatalyst)

add_subdirectory(Under)

add_subdirectory(Test)

