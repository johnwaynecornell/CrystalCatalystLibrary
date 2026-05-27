 cmake_minimum_required(VERSION 3.25)

 set(CMAKE_CXX_STANDARD 17)

 include($ENV{NewAge}/include/JWCEssentials/JWCEssentials.cmake)

 add_executable(htmlify_clipboard
         htmlify_clipboard.cpp
 )

 newage_configure_native_paths(htmlify_clipboard)

 find_library(LIBJWCEssentials JWCEssentials)
 find_library(LIBJWCCommandSpawn JWCCommandSpawn)
 find_library(LIBCrystalCatalystLibrary CrystalCatalystLibrary)

 target_link_libraries(htmlify_clipboard PUBLIC ${LIBCrystalCatalystLibrary} ${LIBJWCEssentials} ${LIBJWCCommandSpawn})

 shuttle(htmlify_clipboard bin)