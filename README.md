CrystalCatalystLibrary
======================

CrystalCatalyst Library Design Philosophy and Benefits
------------------------------------------------------

The design philosophy of the CrystalCatalyst library revolves around leveraging the strengths of C++ for its object-oriented and low-level system programming features while providing a platform-agnostic interface that is easily accessible from higher-level languages. The architecture employs a symmetrical approach where platform-specific implementations are encapsulated within distinct directories (e.g., <code>Platform__Linux</code> and <code>Platform__Windows</code>), each containing tailored code and CMake configurations. This ensures that only the relevant platform-specific code is compiled, reducing complexity and potential conflicts.

Benefits
--------
- **Platform Agnosticism**: By isolating platform-specific implementations, the library maintains a clear separation of concerns. This modularity facilitates easier maintenance and debugging, as changes in one platform's implementation do not affect others.
- **Code Reusability and Extensibility**: The use of C++ allows for the creation of abstract base classes and virtual functions, enabling polymorphism. This makes it straightforward to add support for new platforms without altering the existing codebase, thus promoting code reusability and extensibility.
- **Symmetry and Consistency**: The consistent structure across platform-specific directories simplifies navigation and understanding of the codebase. Developers can quickly locate and modify the necessary components for each platform, enhancing productivity and collaboration.
- **Interoperability**: The <code>extern "C"</code> block exposes the interface to higher-level languages, ensuring that the library can be easily integrated into projects written in various languages. This interoperability broadens the library's applicability and user base.
- **Conditional Compilation**: Using CMake to conditionally include platform-specific files ensures that the build system is efficient and only the necessary code is compiled. This reduces the build time and minimizes the potential for platform-specific compilation errors.
- **High Performance**: By utilizing C++'s low-level programming capabilities, the library can achieve high performance, making it suitable for demanding applications, such as those requiring real-time graphics or extensive computational tasks.


In summary, the CrystalCatalyst library's design philosophy and architecture offer a robust, flexible, and efficient framework for developing platform-agnostic applications, leveraging the best practices of modern C++ programming and build systems.

Dependencies
------------

CrystalCatalystLibrary depends on **JWCEssentials**, a foundational C++ utilities library that provides essential
cross-platform components and helper functions.

Repository: [https://github.com/johnwaynecornell/JWCEssentials](https://github.com/johnwaynecornell/JWCEssentials)

JWCEssentials is integrated into the build system through the `NewAge` environment variable and CMake configuration. The
library is linked during the build process and provides core functionality that CrystalCatalystLibrary builds upon for
platform abstraction, synchronization primitives, and other foundational services.

Overview
--------
> A native windowing and interop substrate designed to expose a stable C-compatible surface for higher-level managed
> language bindings.

CrystalCatalystLibrary is a C++17 native library that provides a small host layer for custom-rendered applications. It is focused on native window creation, platform event dispatch, image presentation, clipboard integration, drag/drop integration, and exported interop functions that can be consumed by generated bindings.

The library is not intended to be a full widget toolkit. It does not attempt to own widgets, layout, styling, scene graph design, animation policy, or application-level command routing. Those responsibilities are expected to live in a higher-level UI framework or application layer.

Instead, CrystalCatalystLibrary acts as the native boundary between operating-system facilities and a custom UI or compositor. The native code owns platform windows and platform messages; the consumer owns rendering, application behavior, and higher-level interaction logic.

    Native C++ Library
            ↓
    C ABI / exported functions
            ↓
    Generated .NET bindings
            ↓
    Managed CrystalWindow / Application API
            ↓
    Application or custom UI framework

Project Layout
--------------

*   `include/CrystalCatalystLibrary/` contains the public native headers.
*   `Application.cpp` and `include/CrystalCatalystLibrary/Application.h` define the application lifecycle surface.
*   `Windowing/` contains the native windowing, pixel conversion, clipboard, data interchange, and drag/drop implementation files.
*   `Platform/` contains platform-specific implementation and CMake integration selected by the current system platform.
*   `Fonts/` contains native font-related support.
*   `Synchronization/` contains synchronization support used by the library.
*   `Test/` contains a native C++ test client that demonstrates window creation, callbacks, image presentation, clipboard, and drag/drop.
*   `Tools/` contains auxiliary tooling targets built by the root CMake project.
*   `Project/CrystalCatalystLibrary.net/` contains the generated and hand-written .NET wrapper project.
*   `Project/CrystalCatalystLibrary.net/docs/README.md` contains the managed/.NET documentation and should be read as the managed-side companion document.

Native Library Responsibilities
-------------------------------

The native library provides the lower-level services that a custom-rendered application needs from the host operating system:

*   Application initialization and event-loop execution.
*   Native window creation, showing, closing, and deferred close requests.
*   Dispatch of draw, keyboard, mouse, resize, focus, close, idle, clipboard, and drag/drop callbacks.
*   Raw image presentation through `CrystalWindow_PresentImage`.
*   Pixel format parsing and conversion between channel layouts and supported channel types.
*   Clipboard copy, paste, persistent copy, clear, and callback-based data provision.
*   Drag/drop target registration, drag start, format selection, status, and data transfer.
*   Opaque handle-based ownership boundaries suitable for generated bindings.

The native implementation uses C++ classes internally, while the public interop surface is organized around exported functions and opaque handles such as `P_INSTANCE(WindowHandle)`, `P_INSTANCE(DataInterchange)`, and `P_INSTANCE(DragDropData)`. This allows generated bindings to work with stable pointer-sized handles instead of depending directly on C++ object layout.

C ABI and Exported Interface
----------------------------

Public functions are exported from headers under `include/CrystalCatalystLibrary/`. The umbrella header `CrystalCatalystLibrary.h` configures import/export macros and includes the major library surfaces: platform, synchronization, windowing, fonts, and application.

The exported interface follows a prefix-based naming style. Examples include:

*   `CrystalCatalystLibrary_Initialize` and `CrystalCatalystLibrary_Close`
*   `Application_Init`, `Application_Run`, and `Application_SignalClose`
*   `CrystalWindow_Create`, `CrystalWindow_CreateSimple`, `CrystalWindow_Show`, `CrystalWindow_Close`, and `CrystalWindow_PostClose`
*   `CrystalWindow_SetMessageHandler`
*   `CrystalWindow_PresentImage` and `CrystalWindow_QueueRedraw`
*   `CrystalWindow_ClipboardPaste`, `CrystalWindow_ClipboardCopy`, `CrystalWindow_ClipboardCopyPersist`, and `CrystalWindow_ClipboardClear`
*   `CrystalWindow_RegisterDragTarget`, `CrystalWindow_DragStart`, and `CrystalWindow_DragChoose`
*   `DataInterchange_Create`, `DataInterchange_Free`, format enumeration, format selection, and selection reveal/set functions

Internally, `WindowHandle` contains a pointer to the native `CrystalWindow` instance. Managed or generated consumers should treat these handles as opaque native resources and should release them through the corresponding exported functions rather than by attempting to inspect or free native memory directly.

Application and Window Lifecycle
--------------------------------

The native application lifecycle begins with `Application_Init`, which accepts an argument array and initializes the platform application object. `Application_Run` enters the native event loop. `Application_SignalClose` requests application shutdown.

The application object tracks native windows and maintains a retain count. Windows can retain or release the application through:

*   `CrystalWindow_ApplicationRetain`
*   `CrystalWindow_ApplicationRelease`

A typical native usage pattern is:

    Application_Init(args);
    
    window = CrystalWindow_Create(800, 600, "Test Window");
    CrystalWindow_ApplicationRetain(window);
    
    CrystalWindow_SetMessageHandler(window, "on_draw", on_draw);
    CrystalWindow_SetMessageHandler(window, "on_close", on_close);
    
    CrystalWindow_Show(window, true);
    Application_Run();

Window creation is exposed through both `CrystalWindow_Create` and `CrystalWindow_CreateSimple`. The exact difference is platform-specific and should be treated cautiously by consumers unless documented by a platform implementation.

Window closure can be immediate or posted. `CrystalWindow_Close` delegates directly to the native window close implementation. `CrystalWindow_PostClose` requests a close through the platform event system where supported, such as by sending a window delete client message on X11.

Message / Event Handler Model
-----------------------------

Each native window owns a `WindowCallbacks` structure. Consumers register callbacks by name through `CrystalWindow_SetMessageHandler`. The native implementation looks up the string name and stores the provided function pointer in the matching callback slot.

Known callback names include:

*   `on_draw`
*   `on_key_down` and `on_key_up`
*   `on_mouse_move`, `on_mouse_down`, and `on_mouse_up`
*   `on_resize`
*   `on_close`
*   `on_focus_in` and `on_focus_out`
*   `on_drag_receive_start`, `on_drag_receive_enter`, `on_drag_receive_motion`, `on_drag_receive_leave`, `on_drag_receive_select`, and `on_drag_receive_drop`
*   `on_drag_provide_status`, `on_drag_provide_chosen`, and `on_drag_provide_finished`
*   `on_clipboard_provide_chosen` and `on_clipboard_receive_data`
*   `on_data_interchange_error`
*   `on_idle`

Platform message loops translate native operating-system messages into these callbacks. For example, the Windows implementation maps paint, keyboard, mouse, size, focus, and destroy messages to the callback structure. The X11 implementation maps expose, key, pointer, configure, focus, and client-message events similarly.

Generated bindings use this callback table to present managed event-handler properties. The managed layer must keep delegate instances alive for as long as the native code may call them.

Image Presentation and Pixel Formats
------------------------------------

CrystalCatalystLibrary presents rendered output with `CrystalWindow_PresentImage`. The caller provides a pixel format string, a pointer to pixel data, a byte length, and image dimensions.

Pixel format strings use the form:

    <CHANNELS>:<TYPE>

Examples include:

*   `RGBA:int8`
*   `BGRA:int8`
*   `RGBA:float32`
*   `RGBA:float64`
*   `RABG:float64`, as used by the native test client

The native pixel conversion code recognizes these channel types:

*   `int8`
*   `float32`
*   `float64`

Channel order is inferred from the characters before the colon. The conversion layer maps matching channels from the source format to the destination format. Current platform presentation paths convert input data to `bgra:int8` before passing it to platform-specific presentation APIs.

On Windows, image presentation uses a top-down bitmap and `StretchDIBits`. On X11, image presentation creates an `XImage` and sends it to the window with `XPutImage`. Both paths should be understood as host presentation services, not as a complete rendering engine.

Clipboard and Drag/Drop Facilities
----------------------------------

Clipboard and drag/drop are modeled through `DataInterchange` and `DragDropData`. A data interchange object contains advertised formats, selected format information, selected data, and optional platform-specific storage.

Clipboard operations include:

*   `CrystalWindow_ClipboardPaste` to request clipboard data.
*   `CrystalWindow_ClipboardCopy` to advertise clipboard data from a window.
*   `CrystalWindow_ClipboardCopyWithCallback` to provide data through a callback.
*   `CrystalWindow_ClipboardCopyPersist` to persist copied data where supported.
*   `CrystalWindow_ClipboardClear` to clear clipboard state.

Drag/drop operations include:

*   `CrystalWindow_RegisterDragTarget` to allow a window to receive drops.
*   `CrystalWindow_DragStart` to begin a drag operation from a window.
*   `CrystalWindow_DragChoose` to choose a transfer format.
*   `DragDropData_Create` to create drag/drop data.
*   `DragDropData_DragActionsString` to describe supported drag actions.

Formats are represented as UTF-8 strings such as `text/plain`, `text/html`, and `text/file-uri`. Consumers can add formats, enumerate available formats, select one, and reveal the resulting data through the `DataInterchange_*` functions.

The drag/drop data type extends the base data interchange model with status and action information. Supported drag actions include copy, move, and link.

Managed Binding Overview
------------------------

The .NET layer under `Project/CrystalCatalystLibrary.net/` is a managed wrapper over the native library. It is generated around the exported C-compatible functions and opaque handle types. Native handles are represented in managed code as `IntPtr` and wrapped by managed classes such as `CrystalWindow`, `DataInterchange`, and `DragDropData`.

Managed consumers interact with the windowing layer through higher-level methods and properties rather than by calling every native function manually. For example, `CrystalWindow.Create` wraps `CrystalWindow_Create`, `Show` wraps `CrystalWindow_Show`, `Close` wraps `CrystalWindow_Close`, and `PresentImage` wraps `CrystalWindow_PresentImage`.

The managed `CrystalWindow` wrapper exposes event-handler properties such as `OnDraw`, `OnKeyDown`, `OnMouseMove`, `OnResize`, `OnClose`, clipboard handlers, drag/drop handlers, and `OnIdle`. Setting one of these properties creates a managed delegate, translates it to a native callback pointer, and registers it through `CrystalWindow_SetMessageHandler`.

The generated code also keeps backing fields for native delegates and managed delegates. This is important because the native library stores raw callback pointers; the managed delegate must remain alive while the callback is registered.

Managed applications are expected to use the wrapper as a host boundary for a custom UI or compositor. A typical managed application creates an application context, creates a `CrystalWindow`, attaches event-handler properties, renders into an image buffer, and presents that buffer through `PresentImage`.

C# Documentation Link / Relationship
------------------------------------

The managed-side companion documentation is located at:

    Project/CrystalCatalystLibrary.net/docs/README.md

That document explains the .NET-facing view of the library, including the managed host model, pixel presentation from managed renderers such as SkiaSharp, recommended layering, and public repository notes for the .NET project.

This root README describes the native C++ library and its exported interop boundary. The nested C# README should remain the primary reference for managed consumers. The two documents are related, but they intentionally do not duplicate each other.

Build Notes
-----------

The root project is a CMake project targeting C++17. The current `CMakeLists.txt` explicitly selects `clang` and `clang++`, sets `CMAKE_CXX_STANDARD` to `17`, and builds `CrystalCatalystLibrary` as a shared library.

The build includes `PlatformConfig.cmake`, initializes platform variables from `CMAKE_SYSTEM_NAME` and `CMAKE_SYSTEM_PROCESSOR`, and then includes a platform-specific CMake file from:

    Platform/${CRYSTAL_PLATFORM}/Platform.cmake

The build also depends on a `NewAge` environment variable and includes:

    $ENV{NewAge}/include/JWCEssentials/JWCEssentials.cmake

The library links against `JWCEssentials`. Build output is versioned and copied into project-local `bin/Debug` or `bin/Release` paths and into a corresponding `$ENV{NewAge}/C/Libs/...` directory.

The root build currently also adds subdirectories for the native test project and tooling:

*   `Test`
*   `Tools/clipboard_html`
*   `Tools/htmlify_clipboard`

Because the project selects platform-specific code through CMake and platform directories, a working build requires the relevant native development libraries for the selected backend. For example, the visible native sources include Windows and X11-oriented implementation paths.

Development Notes / Current Status
----------------------------------

CrystalCatalystLibrary is an experimental native host layer for custom composited UI work. The design favors a host-first architecture: the host provides windows, native messages, pixel presentation, clipboard, drag/drop, and other platform boundaries, while higher-level UI frameworks provide widgets, layout, styling, rendering policy, and input routing.

Some areas are visibly still evolving. Several implementation comments refer to future error handling, platform completion work, or TODO items. Consumers should expect APIs and behavior to change as the native host boundary, pixel presentation model, data interchange model, and generated bindings mature.

The exported handle-based surface is the intended consumption layer for generated bindings. New higher-level language integrations should prefer using the exported functions and opaque handles instead of binding directly to C++ classes.

The project is licensed under the MIT License. See `LICENSE` in the project root for details.