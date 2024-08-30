 cmake_minimum_required(VERSION 3.25)

 set(CMAKE_C_COMPILER "clang")
 set(CMAKE_CXX_COMPILER "clang++")
 set(CMAKE_CXX_STANDARD 17)

 add_executable(test_client
         test_client.cpp
 )

 isDebug($target_name ISDEBUG)

 if (ISDEBUG)
  set(PTH "bin/Debug")
 else()
  set(PTH "bin/Release")
 endif()

 set(PTH "${PTH}/${CMAKE_SYSTEM_NAME}/${CMAKE_SYSTEM_PROCESSOR}")

 target_link_directories(test_client BEFORE PRIVATE $ENV{NewAge}/C/Libs/${PTH})

 target_link_libraries(test_client PUBLIC CrystalCatalystLibrary.0.0.1 JWCEssentials)
