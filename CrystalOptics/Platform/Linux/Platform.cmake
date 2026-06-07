set(CRYSTALOPTICS_PLATFORM_SOURCES
    Platform/Linux/Capture_X11.cpp
    Platform/Linux/Portal_Linux.cpp
)

set(CRYSTALOPTICS_PLATFORM_LIBS pthread X11)

find_package(PkgConfig QUIET)

# XRandR — multi-monitor enumeration
if(PkgConfig_FOUND)
    pkg_check_modules(XRANDR xrandr)
endif()
if(XRANDR_FOUND)
    add_definitions(-DHAVE_XRANDR)
    list(APPEND CRYSTALOPTICS_PLATFORM_LIBS ${XRANDR_LIBRARIES})
    target_include_directories(CrystalOptics PRIVATE ${XRANDR_INCLUDE_DIRS})
endif()

# D-Bus — XDG Desktop Portal (Wayland screenshot support)
if(PkgConfig_FOUND)
    pkg_check_modules(DBUS dbus-1)
endif()
if(DBUS_FOUND)
    add_definitions(-DHAVE_DBUS)
    list(APPEND CRYSTALOPTICS_PLATFORM_LIBS ${DBUS_LIBRARIES})
    target_include_directories(CrystalOptics PRIVATE ${DBUS_INCLUDE_DIRS})
    message(STATUS "CrystalOptics: D-Bus found — Wayland portal support enabled")
else()
    message(STATUS "CrystalOptics: D-Bus not found — Wayland portal support disabled")
endif()

# libpng — decode portal screenshot file to PixData
if(PkgConfig_FOUND)
    pkg_check_modules(LIBPNG libpng)
endif()
if(LIBPNG_FOUND)
    add_definitions(-DHAVE_LIBPNG)
    list(APPEND CRYSTALOPTICS_PLATFORM_LIBS ${LIBPNG_LIBRARIES})
    target_include_directories(CrystalOptics PRIVATE ${LIBPNG_INCLUDE_DIRS})
else()
    message(STATUS "CrystalOptics: libpng not found — portal PNG decode unavailable")
endif()
