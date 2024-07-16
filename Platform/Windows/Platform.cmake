
#target_sources(CrystalCatalyst PUBLIC
#        Synchronization/CrystalSync.cpp
#        Synchronization/CrystalSync.h

#        Windowing/CrystalWindow.cpp
#        Windowing/CrystalWindow.h

#        Windowing/DragDrop.cpp
#        Windowing/DragDrop.h
#)

set(PLATFORM_SOURCES
        Platform/Windows/Platform.cpp
        Platform/Windows/Platform.h

        Platform/Windows/TLS.cpp
        Platform/Windows/TLS.h

        Platform/Windows/Synchronization/CrystalSync.cpp
        Platform/Windows/Synchronization/CrystalSync.h
        Platform/Windows/Windowing/CrystalWindow_Windows.cpp
        Platform/Windows/Windowing/CrystalWindow_Windows.h
        Platform/Windows/Windowing/DragDrop_Windows.cpp
        Platform/Windows/Windowing/DragDrop_Windows.h
        Platform/Windows/CrystalApplication_Windows.cpp
        Platform/Windows/CrystalApplication_Windows.h

        Platform/Windows/Fonts/Fonts.cpp
        Platform/Windows/Fonts/Fonts.h
)

add_definitions(-D_EXPORT_=__declspec\(dllexport\))
target_link_libraries(CrystalCatalystLibrary PUBLIC user32 opengl32)
