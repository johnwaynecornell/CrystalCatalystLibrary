# CrystalCatalystLibrary Standards

1. **Definition** A docket is a place for defining and referencing work that is to be done on the project and serves as a record of completion in a centralized location

2. **Path Normalization**
    - **Input Paths**: All paths provided from the command line should be converted to Unix-style paths if on Windows.
      - Example: A Windows path `C:\folder` should be converted to `/c/folder`.
    - **Output Paths**: All output paths should be converted back to Windows-style paths on Windows systems.
      - Example: A Unix-style path `/c/folder` should be converted to `C:\folder` on Windows.

3. **Dockets**
    - **Name**: Use the following regex pattern to identify docket paths:
      ```regex
      '.*(Docket.md)$'
      ```
      This pattern matches any path ending with `Docket.md`, ensuring that all dockets are correctly identified. Paths should be normalized to the Unix-style format used in Git Bash.

    - The 'STATUS' section is a record of work, to be completed and completed.
        - It contains nested ordered lists of lines like `1. [ ] **Identifier**` or `1. [x] **Identifier**` signaling completion.
            - These form a path that can then be used to reference work.
    - The 'IN PROGRESS' section is to be updated by running the command `python update_docket.py` in the CrystalCatalystLibrary directory.

    - **Docket**: [Docket.md](Docket.md)
        - This file shall serve as the root Docket.

