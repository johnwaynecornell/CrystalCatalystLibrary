 cmake_minimum_required(VERSION 3.25)

 set(CMAKE_CXX_STANDARD 17)

 include($ENV{NewAge}/include/JWCEssentials/JWCEssentials.cmake)

 add_executable(htmlify_clipboard
         htmlify_clipboard.cpp
 )

 isDebug($target_name ISDEBUG)

 if (ISDEBUG)
  set(PTH "bin/Debug")
 else()
  set(PTH "bin/Release")
 endif()

 set(PTH "${PTH}/${CMAKE_SYSTEM_NAME}/${CMAKE_SYSTEM_PROCESSOR}")

 target_link_directories(htmlify_clipboard BEFORE PRIVATE $ENV{NewAge}/C/Libs/${PTH})

 find_library(LIBJWCEssentials JWCEssentials)
 find_library(LIBJWCCommandSpawn JWCCommandSpawn)

 target_link_libraries(htmlify_clipboard PUBLIC CrystalCatalystLibrary.0.0.1 ${LIBJWCEssentials} ${LIBJWCCommandSpawn})

 shuttle(htmlify_clipboard bin)