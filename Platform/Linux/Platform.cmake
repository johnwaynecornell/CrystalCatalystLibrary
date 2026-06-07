
#target_sources(CrystalCatalyst PUBLIC
#        Synchronization/CrystalSync.cpp
#        Synchronization/CrystalSync.h

#        Windowing/CrystalWindow.cpp
#        Windowing/CrystalWindow.h

#        Windowing/DragDrop.cpp
#        Windowing/DragDrop.h
#)

set(PLATFORM_SOURCES
        Platform/Linux/Platform.cpp
        Platform/Linux/Platform.h

        Platform/Linux/Synchronization/CrystalSync.cpp
        Platform/Linux/Synchronization/CrystalSync.h

        Platform/Linux/Windowing/CrystalWindow_X11.cpp
        Platform/Linux/Windowing/CrystalWindow_X11.h

        Platform/Linux/Windowing/Clipboard_X11.cpp
        Platform/Linux/Windowing/Clipboard_X11.h

        Platform/Linux/Windowing/DragDrop_X11.cpp
        Platform/Linux/Windowing/DragDrop_X11.h

        Platform/Linux/Windowing/DragProvide_X11.cpp
        Platform/Linux/Windowing/DragProvide_X11.h

        Platform/Linux/CrystalApplication_X11.cpp
        Platform/Linux/CrystalApplication_X11.h

        Platform/Linux/Windowing/ScreenCapture_X11.cpp

        Platform/Linux/Fonts/Fonts.cpp
        Platform/Linux/Fonts/Fonts.h
)

# Detect XRandR for multi-monitor display enumeration
find_package(PkgConfig QUIET)
if(PkgConfig_FOUND)
    pkg_check_modules(XRANDR xrandr)
endif()

if(XRANDR_FOUND)
    add_definitions(-DHAVE_XRANDR)
    target_link_libraries(CrystalCatalystLibrary PUBLIC pthread X11 GL Xcursor ${XRANDR_LIBRARIES})
    target_include_directories(CrystalCatalystLibrary PRIVATE ${XRANDR_INCLUDE_DIRS})
else()
    target_link_libraries(CrystalCatalystLibrary PUBLIC pthread X11 GL Xcursor)
endif()

#include_directories(${CMAKE_CURRENT_LIST_DIR})