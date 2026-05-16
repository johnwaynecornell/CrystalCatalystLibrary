 cmake_minimum_required(VERSION 3.25)

 set(CMAKE_CXX_STANDARD 17)

 include( "$ENV{NewAge}/include/JWCEssentials/JWCEssentials.cmake")

 add_executable(test_client
         test_client.cpp
 )

 isDebug($target_name ISDEBUG)

 newage_configure_native_paths(clipboard_html)

 find_library(LIBJWCEssentials JWCEssentials)
 target_link_libraries(CrystalCatalystLibrary PUBLIC ${LIBJWCEssentials})

 target_link_libraries(test_client PUBLIC CrystalCatalystLibrary ${LIBJWCEssentials})
