# Hosting Model

CrystalCatalystLibrary follows a host-first model.

The host is responsible for interacting with the operating system. The application or UI framework is responsible for rendering and user interface behavior.

## Why Not Depend Directly on GTK?

GTK is a full UI toolkit. It provides widgets, layout, styling, accessibility, input handling, drawing, and platform integration.

For applications using GTK widgets, this is valuable.

For a custom composited UI system, however, a full toolkit can become too high-level. The UI framework may already provide its own:

- widget tree
- layout engine
- styling system
- renderer
- compositor
- event routing
- invalidation model

In that case, the host only needs to provide a native window and a way to receive input and present rendered output.

## Host Responsibilities

The host should provide:

- window creation
- window destruction
- show/hide
- resize notifications
- draw callbacks
- mouse input
- keyboard input
- clipboard operations
- drag/drop operations
- cursor management
- pixel or surface presentation

## Non-Responsibilities

The host should not own:

- widgets
- layout
- themes
- application styling
- scene graph design
- animation system
- focus traversal policy
- application-level commands

Those belong to the higher-level UI framework.

## Benefits

This approach allows the UI system to remain independent from any one desktop toolkit.

If a host backend changes in the future, the compositor and UI framework can remain largely unchanged.