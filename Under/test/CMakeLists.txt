cmake_minimum_required(VERSION 3.25)
project(test CXX)

set(CMAKE_CXX_STANDARD 17)

add_executable(test main.cpp)
target_link_libraries(test UnderCrystalCatalyst)
