# ClipFlow.Smoke

`ClipFlow.Smoke` is a black-box, process-level behavioral smoke test harness for the `ClipFlow` CLI executable.

## Purpose

Unit tests in `ClipFlow.Tests` validate internal components and managed semantic structures in-process. In contrast, `ClipFlow.Smoke` executes the real `ClipFlow` binary as separate operating system processes to verify:

1. **Independent Process Persistence**: Proves clipboard contents survive the termination of the copying process before the pasting process launches.
2. **End-to-End System Integration**: Verifies true platform clipboard behavior across Linux (X11 `CLIPBOARD_MANAGER` / Wayland `wl-copy`/`wl-paste`), Windows (OLE), and endpoints (files, console/stdin, string, HTML, directories, wildcards, and images).
3. **Black-box Behavioral Contract**: Validates the actual product behavior without mocks or in-memory bypasses.

## Canonical Smoke Cases

1. `text/string -> file`: Verifies UTF-8 string copy with Unicode/multi-line formatting into a file.
2. `text/file -> console`: Verifies copying a file and pasting to console stdout.
3. `console/stdin -> text clipboard`: Verifies streaming stdin input to clipboard and retrieving it into a file.
4. `html/string -> file`: Verifies HTML payload copy and paste.
5. `files/directory -> file list`: Verifies directory copy and normalized file list paste.
6. `files/file-list -> console`: Verifies file-list copy and console stdout output.
7. `directory wildcard`: Verifies literal wildcard directory copy (e.g. `*.txt`) matching only relevant files.
8. `image advertisement`: Verifies `ClipFlow copy image file` persists image format advertised in `ClipFlow show avail`.
9. `image full persistence round trip`: Verifies deterministic SkiaSharp image copy, process exit, paste into new image, and pixel-for-pixel decoded equivalence.

## Usage

### Run all smoke tests
```bash
dotnet run --project Project/ClipFlowPack/ClipFlow.Smoke/ClipFlow.Smoke.csproj
```

### Run a specific category / case
```bash
dotnet run --project Project/ClipFlowPack/ClipFlow.Smoke/ClipFlow.Smoke.csproj -- --case image
dotnet run --project Project/ClipFlowPack/ClipFlow.Smoke/ClipFlow.Smoke.csproj -- --case text
dotnet run --project Project/ClipFlowPack/ClipFlow.Smoke/ClipFlow.Smoke.csproj -- --case files
```

### CLI Options
- `--clipflow <path>`: Specify explicit path to the `ClipFlow` executable.
- `--case <filter>`: Filter test cases by name substring.
- `--keep-temp`: Preserve temporary test fixture workspace on disk.
- `--verbose`, `-v`: Display process command lines, stdout, and stderr for passed tests.
- `--timeout <sec>`: Execution timeout in seconds per child process (default: 10s).
