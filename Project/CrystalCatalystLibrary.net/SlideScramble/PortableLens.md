# The SlideScramble Portable Lens

> **Working guide.** This document records the portable lens demonstrated in the author's handwritten diagrams. The base arrangements are established below; directional formulas and the Shuffle Atlas are being transcribed and checked against the original pages before being marked canonical.

## Purpose

SlideScramble moves complete rows or columns cyclically, with wrapping. The *portable lens* is a method for following four distinguished tiles through a localized sequence of full-row and full-column moves. The shapes identify tiles; their assigned coordinates locate them on the board.

The same formulas are intended to work at different board positions when their coordinates and affected row/column indices are translated consistently.

## Symbol and blank-cell convention

| Symbol | Name |
| --- | --- |
| ○ | Circle |
| ☆ | Star |
| △ | Triangle |
| □ | Square |

**An unmarked (blank) cell in a lens diagram means its original cell value. It does not mean an empty position.** SlideScramble has no empty cells.

The symbols represent the four distinguished tiles, not fixed screen positions. To apply a lens, write the four shapes together with the corresponding current cell coordinates and follow the shape-directed formula.

## Given lens

![Given: circle and star above triangle and square](images/lens-given.svg)

```text
○  ☆
△  □
```

## Column swap — four verified eight-move formulas

![Column swap result: star and circle above square and triangle](images/lens-column-swap.svg)

```text
☆  ○
□  △
```

The original page lists four named entry orientations. **Each table row below is an entire eight-move formula read left to right**; the columns are successive moves, not four concurrent operations. A direction indicates a **one-cell cyclic rotation of the entire row or column currently containing that marked tile**.

| Orientation on handwritten page | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Up left | △↑ | □↑ | □← | □↓ | △→ | △→ | △↓ | ☆← |
| Down left | ○↓ | ☆↓ | ☆← | ☆↑ | ○→ | ○→ | ○↑ | □← |
| Up right | □↑ | △↑ | △→ | △↓ | □← | □← | □↓ | ○→ |
| Down right | ☆↓ | ○↓ | ○→ | ○↑ | ☆← | ☆← | ☆↑ | △→ |

All four formulas were independently replayed on the centered 4×4 wraparound board described below. Each produced the depicted column swap in the 2×2 lens **and restored every other board cell to its starting value**.

## Row swap — four verified eight-move formulas

![Row swap result: triangle and square above circle and star](images/lens-row-swap.svg)

```text
△  □
○  ☆
```

| Orientation on handwritten page | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Left up | ☆← | □← | □↑ | □→ | ☆↓ | ☆↓ | ☆→ | △↑ |
| Right up | ○→ | △→ | △↑ | △← | ○↓ | ○↓ | ○← | ☆↑ |
| Left down | □← | ☆← | ☆↓ | ☆→ | □↑ | □↑ | □→ | ○↓ |
| Right down | △→ | ○→ | ○↓ | ○← | △↑ | △↑ | △← | □↓ |

All four formulas independently produced the depicted row swap in the 2×2 lens and restored all other cells.

### Reading a shape-directed move

For example, `△↑` means: find the current coordinate of the **triangle tile**, then rotate that tile's **entire column upward by one cell**, including every other tile in that column. Resolve the next symbol's current position *after* executing this move.

Repeated instructions such as `△→ △→` are **two one-cell moves**, not a claim that a two-cell rotation counts as two game moves under every input scheme. They preserve the actual order of the handwritten formula.

## Shuffle Atlas — corrected transcription

![Eight shuffle entry configurations, with arrows and resulting two-by-two marked cells](images/lens-shuffle-atlas.svg)

The corrected handwritten page arranges eight **entry configurations** in a deliberate **two-column, four-row** grid. The second entry in the upper-right quadrant replaces a crossed-out drawing; the SVG above shows only the corrected diagram. Each direction is the move of the indicated shape, **not an instruction to move just one tile**: SlideScramble still rotates entire rows or columns.

A **blank diagram cell denotes that cell's original value**. No cell is empty.

The labels below are provisional mnemonic identifiers: ordered first two shape names, followed by A or B. They do not claim a proven classification of all possible local states.

| Position in page | Working identifier | Shape-directed moves | Marked result (top / bottom) |
| --- | --- | --- | --- |
| Upper left A | `Lens.CircleSquare.A` | ○→, □↑, □←, ○↓ | `□ ○ / · ☆` |
| Upper right A **(corrected)** | `Lens.StarTriangle.A` | ☆←, △↑, △→, ☆↓ | `☆ △ / ○ ·` |
| Upper left B | `Lens.CircleSquare.B` | ○↓, □←, □↑, ○→ | `□ · / ○ △` |
| Upper right B | `Lens.StarTriangle.B` | ☆↓, △→, △↑, ☆← | `· △ / □ ☆` |
| Lower left A | `Lens.TriangleStar.A` | △→, ☆↓, ☆←, △↑ | `· □ / ☆ △` |
| Lower right A | `Lens.SquareCircle.A` | □←, ○↓, ○→, □↑ | `△ · / □ ○` |
| Lower left B | `Lens.TriangleStar.B` | △↑, ☆←, ☆↓, △→ | `△ ○ / ☆ ·` |
| Lower right B | `Lens.SquareCircle.B` | □↑, ○→, ○↓, □← | `☆ □ / · ○` |

The dot (`·`) in the table is a *textual placeholder for the original value at that lens cell*, not a new lens symbol and not an empty tile. **On the centered test board**, the eight marked results each resolve the dot to the corresponding original symbol, meaning all four distinguished tiles remain in place except those explicitly repositioned. The SVG keeps those cells visually blank, matching the handwritten page.

### What the atlas establishes

The corrected page documents four paired groups with two orientations each. Treat these as observed and described portable entry configurations. A separate mathematical check would be needed before claiming they exhaust every possible case up to board translation, wrapping, rotation, or reflection.

### Source verification notes

- The upper-right A entry is taken from the **newly redrawn** diagram rather than the crossed-out one.
- The four arrow steps in each entry are transcribed in displayed left-to-right order.
- The source's two-column layout and horizontal quadrant divider are preserved.
- Before final publication, test any transcription with shape-to-coordinate bindings in SlideScramble and compare its marked outcome with the diagram.

## Reproducible verification on the centered board

The source test recording is a 4×4 board with the four home-coordinate tiles `(1,1)`, `(2,1)`, `(1,2)`, and `(2,2)` aligned to ○, ☆, △, and □ respectively in the central 2×2 lens.

```text
(4,4) (1,4) (2,4) (3,4)
(4,1) (1,1) (2,1) (3,1)
(4,2) (1,2) (2,2) (3,2)
(4,3) (1,3) (2,3) (3,3)
```

The verifier [`verify_portable_lens.py`](verify_portable_lens.py) applies each arrow by locating the selected tile **in the current board**, cyclically rotating the whole indicated row/column by one step, and then checking the board state.

**Verified against the source drawings:**

- All four column-swap formulas: `☆ ○ / □ △` (with other cells restored).
- All four row-swap formulas: `△ □ / ○ ☆` (with other cells restored).
- All eight Shuffle Atlas entries, resolving each visually blank cell to its original value.
- All eight shuffle operations restore every cell *outside the central 2×2 lens* to its original board value.

**Translation and wraparound verification:** The same 16 formulas were also tested at every 2×2 lens placement (including border and corner wrapping) on 3×3, 4×4, 5×5, 5×3, and 7×6 boards. All **1,712 cases** passed: the expected marked-cell result was produced and all cells outside the 2×2 lens returned to their starting identities. This supports geometric portability across the tested placements and dimensions; it does not establish a universal completeness theorem.

Run: `python3 verify_portable_lens.py` (standard library only).

## Applying the portable lens

1. Choose the four distinguished tile identities.
2. Write the shape-to-current-cell-coordinate bindings.
3. Identify the matching shuffle/entry configuration.
4. Follow the shape-directed moves, using full-row and full-column rotations in SlideScramble.
5. Check the resulting arrangement, including the unchanged/original-value cells.

A shifted, wraparound test board has been recorded with the original `(1,1)` tile at board position `(2,2)`, aligned to the circle in the centered 2×2 lens. This is useful as a reproducible portability test, not proof of mathematical completeness for all possible configurations.

## Related examples

See [the SlideScramble README](SlideScramble.README.md) and [the bundled challenge catalog](challenges.json) for recordings such as `Lens-Column reversal`, `Lens-Row reversal`, and `Lens Parity`. These are useful worked examples for a later article; do not assert they implement a particular named atlas entry without verification.

## Verification and publication checklist

- [x] Preserve the given, column-swap, and row-swap outcomes.
- [x] Define blank-cell notation and shape identities correctly.
- [x] Transcribe and verify all four column-swap and all four row-swap directional formulas.
- [x] Transcribe and verify eight Shuffle Atlas entries from the corrected diagram on the centered 4×4 board; broader completeness remains open.
- [ ] Agree on the A/B naming and whether the initial moves alone uniquely identify an entry.
- [x] Render the corrected atlas as SVG in its original grid arrangement.
- [x] Verify all 16 sequences against the centered board's coordinates and the wraparound move convention.
- [x] Test all 16 formulas against every translated placement on five board dimensions, including wraparound boundaries (1,712 successful cases).
- [ ] Independently check mathematical completeness of the entry-pattern classification, if desired.
- [ ] Prepare a later JohnCornell.net article from the verified guide.
