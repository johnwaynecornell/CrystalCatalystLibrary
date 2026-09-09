# ClipFlow.Smoke

`ClipFlow.Smoke` is a black-box, multi-process behavioral test harness for the `ClipFlow` CLI executable.

---

## Testing Philosophy

Unit test suites (such as `ClipFlow.Tests`) validate internal data structures, type serialization, and formatting logic within a single managed process. However, in-process testing cannot verify real-world clipboard mechanics because in-process memory sharing masks clipboard persistence failures.

`ClipFlow.Smoke` executes the real, compiled `ClipFlow` binary across isolated operating system processes to enforce the out-of-process persistence contract:

```text
┌─────────────────────────┐
│ Process A: ClipFlow copy│ ---> Writes data & triggers platform persistence
└────────────┬────────────┘
             │
      (Process A Exits)
             │
┌────────────┴────────────┐
│ Process B: ClipFlow paste│ ---> Reads & verifies data from OS clipboard
└─────────────────────────┘
```

This ensures:
1. **True Out-of-Process Persistence**: Data survives process termination via Windows OLE, X11 `CLIPBOARD_MANAGER` / `SAVE_TARGETS`, or Wayland `wl-copy`/`wl-paste`.
2. **End-to-End System Integration**: Validates actual OS IPC boundaries, file system path resolution, image rasterization, and standard stream piping without mocks or memory shims.
3. **Black-box Contract**: Validates CLI return codes, standard output, and standard error streams against production specifications.

---

## Environment Detection

At startup, `ClipFlow.Smoke` inspects the host runtime environment and logs session diagnostics:
- **Operating System**: Linux (X11 / Wayland) or Windows (NT).
- **Display Server**: `DISPLAY` and `WAYLAND_DISPLAY` environment variables.
- **Wayland Utilities**: Availability of `wl-copy` and `wl-paste` binary fallbacks.
- **Persistence Route**: The active platform strategy used for persistent data retention.

---

## Canonical Smoke Cases

The suite runs 13 end-to-end integration test scenarios:

1. **`text/string -> file`**: Verifies UTF-8 string copy (with Unicode, spaces, and multi-line content) into an output file.
2. **`text/file -> console`**: Verifies copying text from a source file and pasting directly to console stdout.
3. **`console/stdin -> text clipboard`**: Verifies piping stdin streams into clipboard and pasting into a destination file.
4. **`html/string -> file`**: Verifies HTML fragment copy, Windows `CF_HTML` envelope handling, and clean file extraction.
5. **`files/directory -> file list`**: Verifies directory copy and normalized file list generation.
6. **`files/file-list -> console`**: Verifies file-list copy and console stdout output.
7. **`directory wildcard`**: Verifies literal wildcard directory copy (e.g. `*.txt`) matching only relevant files.
8. **`image advertisement`**: Verifies `ClipFlow copy image file` registers and advertises `image/png` and `image/bmp` formats in `ClipFlow show avail`.
9. **`image full persistence round trip`**: Verifies deterministic SkiaSharp image copy, process exit, paste into a new PNG image file, and pixel-for-pixel decoded equivalence.
10. **`files/console relative stdin -> file`**: Verifies relative paths piped to stdin normalize to absolute paths upon paste.
11. **`files/file-list relative entries -> file`**: Verifies relative entries in file-lists correctly resolve against the working directory.
12. **`files/console invalid path error`**: Verifies invalid or missing paths piped to stdin fail cleanly with non-zero exit code and error diagnostics.
13. **`files/directory expansion -> directory`**: Verifies recursive directory copy and merge behavior into destination directories.

---

## Usage

### Run All Smoke Tests
```bash
dotnet run --project Project/ClipFlowPack/ClipFlow.Smoke/ClipFlow.Smoke.csproj
```

### Run by Category or Substring Filter
```bash
# Run only image test cases
dotnet run --project Project/ClipFlowPack/ClipFlow.Smoke/ClipFlow.Smoke.csproj -- --case image

# Run only text test cases
dotnet run --project Project/ClipFlowPack/ClipFlow.Smoke/ClipFlow.Smoke.csproj -- --case text

# Run only file/directory test cases
dotnet run --project Project/ClipFlowPack/ClipFlow.Smoke/ClipFlow.Smoke.csproj -- --case files
```

### Command-Line Flags
- `--case <filter>`: Substring filter to execute a specific test case or category.
- `--verbose`, `-v`: Display child process invocation arguments, execution duration, exit codes, stdout, and stderr for all test runs.
- `--diag`: Pass -diag to every ClipFlow process.
- `--keep-temp`: Retain temporary test workspace and artifacts on disk for inspection after test execution.
- `--timeout <sec>`: Per-process execution timeout in seconds (default: 10s).
- `--clipflow <path>`: Explicitly specify the path to the `ClipFlow` binary under test.
- `--help`, `-h`: Display command-line usage instructions.

---

## Failure Diagnostics

When a test fails, `ClipFlow.Smoke` generates a detailed diagnostic summary:
- The exact failure reason and stage (e.g. copy process failed, content mismatch, timeout).
- Child process commands executed, execution times, and exit codes.
- Complete standard output and standard error dumps from each process step.
- The path to the isolated temporary test workspace (if preserved with `--keep-temp`).
