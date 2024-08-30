
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

        Platform/Linux/TLS.cpp
        Platform/Linux/TLS.h

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

        Platform/Linux/Fonts/Fonts.cpp
        Platform/Linux/Fonts/Fonts.h
)

target_link_libraries(CrystalCatalystLibrary PUBLIC  pthread X11 GL)

#include_directories(${CMAKE_CURRENT_LIST_DIR})