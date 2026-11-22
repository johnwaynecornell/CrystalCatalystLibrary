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

## Column swap

![Column swap result: star and circle above square and triangle](images/lens-column-swap.svg)

```text
☆  ○
□  △
```

The resulting two-column exchange corresponds to two disjoint swaps among the four distinguished tiles. The original handwritten page also includes a directional formula; **verify its precise arrow sequence against the source before transcribing it here**.

## Row swap

![Row swap result: triangle and square above circle and star](images/lens-row-swap.svg)

```text
△  □
○  ☆
```

The resulting two-row exchange likewise corresponds to two disjoint swaps. The exact shape-directed formula remains to be transcribed and checked.

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

The dot (`·`) in the table is a *textual placeholder for the original value*, not a new lens symbol and not an empty tile. The SVG keeps those cells visually blank, matching the handwritten page.

### What the atlas establishes

The corrected page documents four paired groups with two orientations each. Treat these as observed and described portable entry configurations. A separate mathematical check would be needed before claiming they exhaust every possible case up to board translation, wrapping, rotation, or reflection.

### Source verification notes

- The upper-right A entry is taken from the **newly redrawn** diagram rather than the crossed-out one.
- The four arrow steps in each entry are transcribed in displayed left-to-right order.
- The source's two-column layout and horizontal quadrant divider are preserved.
- Before final publication, test any transcription with shape-to-coordinate bindings in SlideScramble and compare its marked outcome with the diagram.

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
- [ ] Transcribe and verify both complete directional formulas.
- [x] Transcribe eight Shuffle Atlas entries from the corrected diagram; verify their play behavior before claiming canonical completeness.
- [ ] Agree on the A/B naming and whether the initial moves alone uniquely identify an entry.
- [x] Render the corrected atlas as SVG in its original grid arrangement.
- [ ] Compare diagrams with actual SlideScramble recordings and coordinate bindings.
- [ ] Prepare a later JohnCornell.net article from the verified guide.
