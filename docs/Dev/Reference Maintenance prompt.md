You are maintaining a repository-level Reference.md file.

Goal:
Create or update Reference.md as an AI-assistant orientation document for this repository. It should help future assistants and maintainers quickly understand exported entry points, important managed classes, architectural seams, and safe extension points.

Reference.md is not exhaustive API documentation. It is a practical map of the repository: what is here, what matters, where to look, and what not to casually change.

Repository parameters:
- Repository name: <REPO_NAME>
- Primary purpose: <PURPOSE>
- Main languages/runtimes: <LANGUAGES>
- Target platforms: <PLATFORMS>
- Primary solution/project files: <PROJECT_FILES>
- Special focus areas: <FOCUS_AREAS>
- Reference depth: <light | standard | deep>
- File path style: repo-relative only
- Export marker: _EXPORT_
- Existing Reference.md policy: <create | maintain | refresh | restructure>

Reference depth:
- light:
    - Document only major exported symbols, top-level managed classes, and the most important architectural seams.
    - Use for small repositories or quick refreshes.

- standard:
    - Document major _EXPORT_ entries, important managed classes, subsystems, extension points, and cautions.
    - Use as the default maintenance depth.

- deep:
    - Build a fuller subsystem inventory.
    - Include secondary helper classes when they explain architecture, data flow, interop, rendering, parsing, transforms, state, or safe modification paths.
    - Use for first-time Reference.md generation, complex repositories, interop-heavy repositories, or repositories intended for AI handoff.

File path rules:
- All file paths must be repo-relative paths from the repository root.
- Example: `Project/CrystalCatalystLibrary.net/CrystalOpenGL/GLRenderer.cs`
- Do not use absolute local paths.
- Do not use machine-specific paths.
- Do not use GitHub URLs unless specifically requested.

_EXPORT_ convention:
- `_EXPORT_` is intentionally grep-friendly.
- Treat single-line `_EXPORT_` definitions as a repository convention that helps humans and agents locate native/interop entry points quickly.
- Search for the literal text `_EXPORT_`.
- Prefer entries where the exported declaration is on a single line.
- Do not reformat, split, rename, or relocate `_EXPORT_` definitions unless explicitly asked.
- If an `_EXPORT_` declaration is not single-line, document it and add a caution note rather than “fixing” it.

Scan rules:
1. Inspect the repository structure before writing.
2. Identify native/exported entry points, especially symbols marked with `_EXPORT_`.
3. Identify important managed classes, especially public or internal classes that define architecture, APIs, parsing, rendering, transforms, interop, state management, build flow, or testing harnesses.
4. Group managed classes by subsystem rather than alphabetically when possible.
5. Include repo-relative file paths for every documented symbol, class, or important file.
6. Keep descriptions brief but useful.
7. Do not create exhaustive API documentation.
8. Do not document build artifacts, bin/obj folders, generated IDE files, or trivial classes unless they are architectural landmarks.
9. Do not infer design intent from names alone when code contradicts it.
10. Prefer concrete observations from source files over broad architectural speculation.

_EXPORT_ entry handling:
For each exported entry point, include:
- Symbol/function name
- Repo-relative file path
- One-sentence purpose
- Ownership/lifetime notes if visible
- Platform or ABI cautions if visible

Do not invent ABI, threading, ownership, or lifetime semantics that are not explicit in code.

Suggested table:

| Symbol | File | Purpose | Notes |
|---|---|---|---|
| `_EXPORT_ ExampleFunction` | `Project/Example.Native/example.cpp` | Brief description. | Lifetime/platform notes, if visible. |

Managed class handling:
For each important managed class, include:
- Class name
- Repo-relative file path
- Responsibility
- Key collaborators
- Cautions or extension notes, if relevant

Prefer classes that define:
- public API boundaries
- rendering flow
- OpenGL or graphics helpers
- parser / transform / emitter pipelines
- native interop boundaries
- state machines
- reflection or generated-code systems
- build/runtime coordination
- important tests or harnesses

Avoid listing every method or property unless it is central to understanding the class.

Suggested managed class format:

### Subsystem Name

#### `ClassName`
File: `Project/SomeProject/Path/ClassName.cs`

Purpose:
- Brief responsibility summary.

Collaborates with:
- `OtherClass`
- `AnotherClass`

Notes:
- Cautions, version assumptions, extension points, or fragile behavior.

Maintenance behavior:
If Reference.md already exists:
- Preserve accurate user-authored commentary.
- Preserve useful structure unless it is clearly broken.
- Update stale sections.
- Add new sections only when useful.
- Prefer minimal, reviewable diffs.
- Mark uncertain claims as “Needs verification” instead of guessing.
- Do not rewrite the whole document unless the existing structure is unusable.
- Do not remove warnings, TODOs, or caveats unless verified against source.

If Reference.md does not exist:
- Create it using the structure below.
- Keep the first version useful, navigable, and conservative.
- Prefer accurate coverage over completeness.
- Use “Needs verification” for uncertain areas.

Output structure:
Use this structure unless the existing Reference.md has a better established structure.

# Reference.md

## Purpose
Explain that this is an orientation document for AI assistants and maintainers, not exhaustive API documentation.

## Repository Overview
Summarize the repository’s purpose, major projects, languages, and platforms.

## Orientation for AI Assistants
Give practical rules for future AI agents working in this repository.

Examples:
- Prefer local conventions over generic framework advice.
- Check existing helper classes before introducing new abstractions.
- Use repo-relative paths in notes and generated documentation.
- Do not assume Windows-only or Linux-only behavior unless the code path is explicitly platform-specific.
- Do not reformat `_EXPORT_` declarations casually; they are grep-friendly by design.

## Build / Runtime Assumptions
Summarize visible build, runtime, platform, native-library, graphics, or framework assumptions.

## Native / Exported Entry Points
Document `_EXPORT_` entries in a table.

Include:
- Symbol
- Repo-relative file path
- Purpose
- Notes

## Managed Class Map
Group important managed classes by subsystem.

For each class, include:
- File
- Purpose
- Collaborators
- Notes

## Architectural Seams
Document important boundaries, such as:
- managed/native interop
- renderer/helper separation
- parser/transform/emitter separation
- generated/reflected code boundaries
- platform-specific seams
- test harness boundaries

## Extension Points
Document where future changes should usually hook in.

Examples:
- Add new rendering behavior through existing renderer/helper patterns.
- Add new parser behavior through existing parse/transform stages.
- Add native exports only through the established `_EXPORT_` convention.

## Known Cautions
Document fragile or important areas, such as:
- OpenGL version checks
- platform-specific APIs
- generated files
- ownership/lifetime-sensitive native handles
- reflection-generated code
- tests that should be run after changes

## Generated / Reflected / Derived Files
Mention files that appear generated, reflected, or derived.

Explain:
- what generates them, if visible
- whether they should be edited manually
- how agents should treat them

## Maintenance Notes
Briefly explain how to keep Reference.md current.

Include:
- Refresh after adding exported entry points.
- Refresh after adding major managed classes or subsystems.
- Refresh after changing architectural seams.
- Keep repo-relative paths.
- Keep `_EXPORT_` entries grep-friendly and single-line unless there is a strong reason not to.

Quality bar:
- Be concrete.
- Use repo-relative file paths.
- Prefer “what this does” over “what it might do.”
- Use cautious language for inferred intent.
- Keep the document useful to a future AI assistant with limited context.
- Do not overfit to one assistant or one IDE.
- Do not document private implementation details as stable public API unless the code clearly treats them that way.