cmake_minimum_required(VERSION 3.25)
set(CMAKE_CXX_STANDARD 17)

project(CrystalOptics)

# Inherit platform config from parent if built as subdirectory,
# otherwise load it standalone.
if(NOT COMMAND newage_configure_native_paths)
    include(${CMAKE_CURRENT_LIST_DIR}/../PlatformConfig.cmake)
    initPlatformConfig()
endif()

include($ENV{NewAge}/include/JWCEssentials/JWCEssentials.cmake)

add_library(CrystalOptics SHARED)

newage_configure_native_paths(CrystalOptics)

include(Platform/${CRYSTAL_PLATFORM}/Platform.cmake)

target_sources(CrystalOptics PUBLIC
    CaptureAPI.cpp
    include/CrystalOptics/CaptureAPI.h
    ${CRYSTALOPTICS_PLATFORM_SOURCES}
)

add_definitions(-DBUILD_CRYSTALOPTICS)

target_include_directories(CrystalOptics PUBLIC
    include
    $ENV{NewAge}/include
)

find_library(LIBJWCEssentials JWCEssentials)
find_library(LIBCrystalCatalyst CrystalCatalystLibrary)

target_link_libraries(CrystalOptics PUBLIC
    ${LIBJWCEssentials}
    ${LIBCrystalCatalyst}
    ${CRYSTALOPTICS_PLATFORM_LIBS}
)

newage_configure_native_paths(CrystalOptics)
shuttle(CrystalOptics lib)
