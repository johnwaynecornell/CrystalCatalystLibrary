NewAge Environment
==================

> The NewAge environment is the shared workspace, staging, and build convention used by the NewAge-family repositories.

Purpose
-------

This document describes the common environment assumptions shared by repositories such as `NewAge`, `JWCEssentials`, `JWCCommandSpawn`, and `CrystalCatalystLibrary`.

Individual repositories may have their own build instructions, but they should agree on the environment contract described here.

Shared Repositories
-------------------

*   `NewAge` — main workspace and higher-level library/application tree.
*   `JWCEssentials` — foundational native and managed utility layer.
*   `JWCCommandSpawn` — command/process execution support.
*   `CrystalCatalystLibrary` — native platform layer and generated managed bindings.

Required Environment Variable
-----------------------------

The primary shared variable is `NewAge`.

It should point to the root of the main NewAge workspace.

### Linux / Bash

    export NewAge="$HOME/NewAge"

### Windows PowerShell

    [Environment]::SetEnvironmentVariable("NewAge", "C:\src\NewAge", "User")

### Temporary PowerShell Session

    $env:NewAge = "C:\src\NewAge"

Staged Library Layout
---------------------

Many .NET projects use a shared staged library path constructed from the `NewAge` environment variable.

    $(NewAge)\DotNet\Libs\lib\$(Configuration)\$(TargetFramework)

Example paths:

    $NewAge/DotNet/Libs/lib/Debug/net8.0 $NewAge/DotNet/Libs/lib/Debug/net10.0

Build Staging Script
--------------------

Many projects use a post-build step that invokes:

    bash NewAge_stage.sh

This means the build environment must provide:

*   `bash`
*   a resolvable `NewAge_stage.sh`
*   a valid `NewAge` environment variable
*   a writable staged library output directory

.NET SDK Expectations
---------------------

Current NewAge-family development uses modern .NET targets, especially:

*   `net8.0` for compatibility and older lanes
*   `net10.0` for active forward development

A fresh system should verify installed SDKs with:

    dotnet --list-sdks dotnet --list-runtimes

Native Build Expectations
-------------------------

Native projects may require:

*   CMake
*   a C or C++ compiler
*   platform-specific build tools
*   Bash-compatible staging scripts

On Windows, this may include Visual Studio Build Tools, CMake, Git, and a Bash provider such as Git Bash or MSYS2.

On Linux common X11 headers may include 'libx11-dev libxcb-dev libxext-dev libxinerama-dev libxrandr-dev libxi-dev libxcursor-dev libxrender-dev libxft-dev libxfixes-dev libxdamage-dev libxcomposite-dev
'

Repository-Specific Notes
-------------------------

This document defines only the shared environment. Repository-specific build instructions should remain in each repository's own README or docs folder.

*   `NewAge` should document bootstrap/build order.
*   `JWCEssentials` should document native and managed utility builds.
*   `JWCCommandSpawn` should document process/pipe/runtime assumptions.
*   `CrystalCatalystLibrary` should document native C++ builds, generated bindings, platform layers, and managed interop.

Fresh-System Verification
-------------------------

A fresh machine or VM should verify the shared environment before attempting full builds.

    echo "$NewAge" 
    dotnet --list-sdks 
    bash --version 
    cmake --version

On Windows PowerShell:

    echo $env:NewAge 
    dotnet --list-sdks 
    bash --version 
    cmake --version

Design Note
-----------

The NewAge environment is a shared development substrate. It is intended to make multiple repositories behave as one coherent workspace without forcing all source code into a single repository.

    NewAgeEnvironment → shared paths → staged libraries → Bash tooling → native and managed build continuity → reproducible fresh-system setup