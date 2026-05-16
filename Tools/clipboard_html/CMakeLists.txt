 cmake_minimum_required(VERSION 3.25)

 set(CMAKE_CXX_STANDARD 17)

 include($ENV{NewAge}/include/JWCEssentials/JWCEssentials.cmake)

 add_executable(clipboard_html
         clipboard_html.cpp
 )

 isDebug($target_name ISDEBUG)

 if (ISDEBUG)
  set(PTH "bin/Debug")
 else()
  set(PTH "bin/Release")
 endif()

 set(PTH "${PTH}/${CMAKE_SYSTEM_NAME}/${CMAKE_SYSTEM_PROCESSOR}")

 target_link_directories(clipboard_html BEFORE PRIVATE $ENV{NewAge}/C/Libs/${PTH})

 find_library(LIBJWCEssentials JWCEssentials)
 target_link_libraries(CrystalCatalystLibrary PUBLIC ${LIBJWCEssentials})

 target_link_libraries(clipboard_html PUBLIC CrystalCatalystLibrary.0.0.1 ${LIBJWCEssentials})

 shuttle(clipboard_html bin)