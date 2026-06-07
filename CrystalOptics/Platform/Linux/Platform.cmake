set(CRYSTALOPTICS_PLATFORM_SOURCES
    Platform/Linux/Capture_X11.cpp
)

find_package(PkgConfig QUIET)
if(PkgConfig_FOUND)
    pkg_check_modules(XRANDR xrandr)
endif()

if(XRANDR_FOUND)
    add_definitions(-DHAVE_XRANDR)
    set(CRYSTALOPTICS_PLATFORM_LIBS pthread X11 ${XRANDR_LIBRARIES})
    target_include_directories(CrystalOptics PRIVATE ${XRANDR_INCLUDE_DIRS})
else()
    set(CRYSTALOPTICS_PLATFORM_LIBS pthread X11)
endif()
