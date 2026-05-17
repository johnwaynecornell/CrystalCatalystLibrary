 cmake_minimum_required(VERSION 3.25)

 set(CMAKE_CXX_STANDARD 17)

 include($ENV{NewAge}/include/JWCEssentials/JWCEssentials.cmake)

 add_executable(clipboard_html
         clipboard_html.cpp
 )


 newage_configure_native_paths(clipboard_html)

 find_library(LIBJWCEssentials JWCEssentials)

 target_link_libraries(clipboard_html PUBLIC CrystalCatalystLibrary ${LIBJWCEssentials})

 shuttle(clipboard_html bin)