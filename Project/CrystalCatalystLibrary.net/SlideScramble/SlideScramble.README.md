# SlideScramble

**SlideScramble** is a wraparound row-and-column puzzle built as a showcase application for **CrystalCatalyst**.

Unlike the traditional missing-tile sliding puzzle, SlideScramble has no empty space.

Instead, the player may grab an entire row or column and slide it in either direction. Tiles wrap around the opposite edge of the board, allowing every move to transform the puzzle while preserving the complete grid.

<p align="center">
  <img src="SlideScramble.png"
       alt="SlideScramble running on a 4x4 grid"
       width="900">
</p>

## The Puzzle

Each tile has a home coordinate.

The goal is simple:

> Return every tile to its original position.

The mechanism is equally simple:

- drag a row left or right;
- drag a column up or down;
- movement wraps continuously around the grid;
- release to commit the move.

From those rules comes a surprisingly rich puzzle.

A single move affects an entire line of tiles, and fixing one relationship may disturb another. The player therefore works with the structure of the board rather than moving isolated pieces.

## Build Your Own Puzzle

SlideScramble is not limited to a single board size.

The grid can be configured independently in each dimension, making boards such as:

```text
2 x 4
3 x 3
3 x 5
4 x 4
5 x 7
9 x 9
```

possible from the same game.

This changes more than the amount of work required.

A narrow rectangular board behaves differently from a square one. Row and column cycles interact differently, visual strategies change, and patterns that are useful on one geometry may be much less useful on another.

For that reason, grid dimensions act almost like **puzzle parameters or levels**.

SlideScramble can therefore be thought of not only as one puzzle, but as a small generator for a family of related puzzles.

## Controls

The current HUD exposes the main game operations directly.

```text
S       Scramble
R       Toggle session panel
X       Reset
G       Grid Setup
C       Challenges
1-4     Grid presets
Esc     Cancel
```

Rows and columns are manipulated directly with the pointer.

The Grid Setup interface allows the board dimensions to be selected independently.

## Challenge catalog

`challenges.json` is a JSON object keyed by challenge names, with session recordings
as values. The bundled file is
copied to the build and publish output; `ChallengeCatalog.LoadBundled()` reads it from
`AppContext.BaseDirectory`, not from the current working directory or `sessions`.
To add a challenge, add a named recording to the source file and rebuild or republish, or edit
the deployed app-base copy. For example, this complete catalog swaps the two bottom-right
tiles of a 4×4 board:

```json
{
  "Bottom-right swap": {
    "format": "SlideScramble",
    "version": 2,
    "width": 4,
    "height": 4,
    "initialCells": [
      [[1, 1], [2, 1], [3, 1], [4, 1]],
      [[1, 2], [2, 2], [3, 2], [4, 2]],
      [[1, 3], [2, 3], [3, 3], [4, 4]],
      [[1, 4], [2, 4], [3, 4], [4, 3]]
    ],
    "moves": []
  }
}
```

Each board is a rectangular permutation of its home coordinates; version 2 coordinates
are one-based. An empty `moves` array is valid: the initial arrangement alone describes
the challenge and there are no recorded moves to replay. Nonempty moves use the session
format below. Names must be nonblank and are case-sensitive. Invalid entries are skipped;
a missing, unreadable, malformed, or non-object catalog produces an empty catalog. The recording parser accepts version 1 and
2 and dimensions up to 256, but the current session-loading UI limits each dimension to
2–9; use 2–9 for challenges intended for the game.

Press **C** to open the challenge picker. Selecting a challenge previews it without
changing the board. **Load** starts play from its initial arrangement, while **Moves**
opens its recorded sequence for inspection. A zero-move challenge has no steps to inspect.
Use **Tab** or **Shift+Tab** to move between the editor and enabled actions, and **Enter**
to activate the focused action.
Only one selector can be open at a time. Stop recording and finish any board motion or
grid setup before opening it; existing playback pauses while the selector is open.

## Session recording and playback

Press **R** to show or hide the translucent session panel in the largest available gutter.
It may overlap the board when space is limited. Click the filename field to edit it;
Enter accepts and Escape restores the previous name. While it has focus, game shortcuts
are suppressed. Click **Rec** to capture the current arrangement, play normally, then
click **Stop Rec**. The recording is immediately available for inspection at its final
move; **Save** writes it to a file. **Load** opens the session selector; its **Load**
starts a playable board and **Moves** opens recorded steps. **|<**, **<**,
and **>** reset, step backward, and step forward. **Play/Pause** advances automatically;
steps animate and highlight the original drag-start cell until the transition finishes.
During manual play, the drag-start cell is highlighted through the release or cancel snap.
**Stop** leaves inspection and
preserves the session for resuming. Recording,
loading, and playback wait until the current drag, snap, or scramble finishes. Reset,
scramble, and grid changes are unavailable while recording or inspecting.

Files are stored as `.json` in the `sessions` directory next to the executable. Names
cannot contain paths or reserved filename characters. Errors are displayed in the footer.
Loading validates the file before replacing the current board. Playback does not add
game moves or record itself.

New recordings use version 2, with one-based coordinates matching the tile labels:
columns run left to right and rows top to bottom. Each initial cell is the tile's home
`[x,y]` coordinate. A move's `dragStart` is also `[x,y]`, and `index` is the one-based
row or column selected there. `delta` is unchanged: positive values follow the grid's
logical rotation convention (tiles move left for rows and up for columns).
Version 1 recordings use zero-based coordinates and indices and remain readable; saving
one again writes version 2. Older versions of the application cannot read version 2.

```json
{"format":"SlideScramble","version":2,"width":2,"height":2,
 "initialCells":[[[1,1],[2,1]],[[1,2],[2,2]]],
 "moves":[{"axis":"Row","index":2,"dragStart":[1,2],"delta":1}]}
```

`Grid` owns the rotations; `SessionRecording` validates and serializes the starting
arrangement and moves; `SessionRecordingController` manages recording and inspection.
`ICrystal`, `CrystalInteraction`, and `CrystalTextEditor` provide renderer-independent
input and editing models, while the window draws them with Skia.

The window binding currently exposes named keys, not committed Unicode text or IME
events. The reusable editor accepts Unicode text from hosts with such an input source,
but this window's filename entry is limited to printable ASCII key events and does not
support IME, clipboard, or complex text shaping. Long filenames are clipped. Font files
are tried in platform-specific order, with the Skia default as fallback.

## Scrambling

Starting a game performs a sequence of legal row and column transformations.

Because the scrambled state is produced through the same operations available to the player, the resulting puzzle remains reachable through normal play.

The move counter begins when the scramble is complete.

## Audio

SlideScramble uses **CrystalOpenAL** for lightweight game audio.

Sound is deliberately restrained and serves primarily as interaction feedback rather than as a continuous soundtrack.

The application also exercises the managed CrystalOpenAL infrastructure, including `AudioEngine`, synthesized PCM effects, and the pointerless OpenAL bridge layer.

## CrystalCatalyst Showcase

SlideScramble was created both as a game and as a practical consumer of the CrystalCatalyst libraries.

It exercises several parts of the platform together:

- CrystalCatalyst window creation and input;
- CrystalSkia rendering;
- pointer-driven interaction;
- responsive layout;
- HUD controls;
- game-state management;
- CrystalOpenAL audio;
- managed/native bridge infrastructure.

It is intentionally a complete interactive program rather than an isolated API test.

## Design

The program keeps the puzzle model separate from presentation where practical.

The core concepts include:

```text
Tile
    remembers its home coordinate

Grid
    owns the current tile arrangement
    performs row and column transformations

GameState
    tracks Ready, Scrambling, InGame, and completion state

Window
    handles presentation, input, animation, HUD, and integration
```

This makes the grid mechanics independently understandable and testable while allowing the showcase application to remain visually rich.

## Why Wraparound?

Wraparound movement removes the special empty-cell state of a conventional sliding puzzle.

Every row and every column is always available.

This creates a different kind of reasoning problem.

Instead of asking:

> Which tile can move into the empty space?

the player asks:

> Which cyclic transformation improves the relationships among several tiles at once?

The resulting puzzle is spatial, but it also has an algebraic flavor: every move is a reversible permutation of one row or column.

## Reversibility

Every legal SlideScramble move has an immediate inverse.

Moving a row one position to the right can be undone by moving it one position to the left. The same is true for columns and for larger shifts.

This gives the game a useful property:

**the puzzle changes through reversible transformations rather than destructive state changes.**

That makes experimentation natural. A player can try a move, observe its effect, and reason backward when necessary.

## Difficulty Is More Than Size

Larger boards generally provide more relationships to manage, but board area is not the only source of difficulty.

Geometry matters.

For example:

```text
2 x 4
```

and

```text
4 x 2
```

contain the same number of tiles, yet emphasize different row and column structures.

Likewise, a long narrow grid may encourage very different strategies from a compact square grid.

Future experimentation may reveal useful categories of board geometry, move efficiency, or characteristic solving strategies.

## Development Status

SlideScramble is an active CrystalCatalyst showcase and experimental puzzle.

Current work has focused on:

- polished row and column dragging;
- wraparound animation;
- configurable grid dimensions;
- presets;
- responsive fitting of different board geometries;
- restrained game audio;
- CrystalOpenAL integration;
- automated tests around puzzle and audio infrastructure.

The game is intentionally being allowed to develop through play as well as implementation.

## Project Location

```text
Project/CrystalCatalystLibrary.net/SlideScramble
```

The accompanying source is intended to remain readable as a practical CrystalCatalyst application example.