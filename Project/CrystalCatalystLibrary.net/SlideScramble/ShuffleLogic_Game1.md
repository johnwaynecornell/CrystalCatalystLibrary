# ShuffleLogic — Game 1: A Solving Journal

**Challenge:** `ShuffleLogic - Game 1 (13 shuffles)`  
**Date:** November 26, 2026  
**Board:** 4 × 4  
**Rules:** ShuffleLogic — Shuffle operations only  
**Result:** Solved in **13 logical shuffles** (17 originally recorded; two inverse pairs removed)

## Lens notation

The Portable Lens is always oriented:

```text
○ ☆
△ □
```

`○@(x,y)` places the Circle corner at board position `(x,y)`; positions are **one-based**, and the lens may wrap around the board edges. A gesture such as `△→□` moves from the Triangle symbol toward the Square symbol. In this journal each gesture is a **Shuffle**, not a Swap. A coordinate in `○@(x,y)` is a **board position**: the Circle corner of the lens is placed at that column and row. In contrast, a value inside a tile—such as `4,1`—is the tile’s **identity** (its destination column and row in the solved picture), not necessarily its present board position. The challenge stores `lensAnchor` zero-based, so JSON `[1,3]` corresponds to the journal’s `○@(2,4)`. `*,2` refers to tile identities belonging to solved row 2, regardless of where they currently sit.

The journal retains the observations made during play rather than rewriting them as a hindsight-perfect strategy. The original recording contained two mistaken inverse pairs; those operations were removed from the published 13-shuffle solution, and subsequent operations were renumbered.

## Initial board

The actual initial board from the named challenge is shown below. Each entry is a **tile identity**, while its place in this table is the **current board coordinate**.

| Board row | Column 1 | Column 2 | Column 3 | Column 4 |
|---|---|---|---|---|
| 1 | `1,1` | `1,4` | `2,1` | `4,2` |
| 2 | `2,4` | `2,2` | `4,3` | `2,3` |
| 3 | `4,4` | `1,2` | `3,4` | `3,3` |
| 4 | `1,3` | `4,1` | `3,1` | `3,2` |

## Solving journal

### Operation 0 — Initial observations

Taking a look at the board, the first things I see are that `1,1` and `2,1` are close. If I place the lens at `○@(2,4)`, it is a close step toward order, and the identity-row positions will be nearly done.

This may be a game of *most obvious*, because tiles being close may be akin to having been shuffled last. Looking at the board a while longer, it looks as though `3,2` and `4,2` can be made to fall into place.

At another level, I noticed that a **Copy Board as JSON** feature would be extremely helpful: it could naturally produce a nicely formatted JSON grid array.

### Operation 1 — `□→△  ○@(2,4)`

Correct the difference across the vertical split.

**Observation:** It's best to leave the lone `4,1` for a later move. The next move at `○@(1,2)` will help sort the `*,4` cells toward the bottom.

### Operation 2 — `○→△  ○@(1,2)`

Correct the `*,2` values and migrate the `*,4` values.

**Observation:** The `*,3` values in `○@(2,4)` should not be the next operation's target: that would be disruptive, bringing `3,4` up.

Looking at `4,2` and `3,2`, they should be the next focus. Projecting forward, `4,1` needs to reach the right position. So `△→□` at `○@(2,3)`, followed by `△→□` at `○@(3,3)`, should act as a **forward gear**.

### Operation 3 — `△→□  ○@(2,3)`

First step of the proposed forward gear.

### Operation 4 — `△→□  ○@(3,3)`

Second step of the forward gear.

**Observation:** The *gear* attempt left things more unsettled than I expected. I will now place `4,1`, even though it moves `4,2` to the bottom; I think it will work there.

### Operation 5 — `☆→□  ○@(3,4)`

### Operation 6 — `□→△  ○@(3,3)`

### Operation 7 — `□→☆  ○@(3,3)`

Migrate `*,2` cells into row 3, intending to move `4,3` and `2,3` down. I think this can be accomplished with one lens location and two operations with different gesture anchors.

### Operation 8 — `△→□  ○@(3,2)`

### Operation 9 — `□→☆  ○@(3,2)`

**Observation:** This leaves things in a nice orderly state. Visually, `○@(2,3)` will put the next two row-3 cells in place.

### Operation 10 — `□→☆  ○@(2,3)`

*Two mistaken inverse pairs occurred here in the original 17-operation session; they cancel one another and are omitted from this cleaned sequence.*

### Operation 11 — `△→○  ○@(1,3)`

### Operation 12 — `○→△  ○@(4,3)`

Attempt an equal-exchange operation leveraging `4,3`.

### Operation 13 — `△→□  ○@(3,3)`

**Solved.**

## Result and notes

The initial recorded solution had 17 logical Shuffles. Removing two inverse pairs leaves this 13-operation solution, while preserving the same final solved board.

The emerging approach was not simply to place one tile at a time. It involved keeping related tiles near one another, migrating groups between rows, and testing whether successive lens placements could act as a *forward gear*. Some steps looked less orderly at first but helped prepare later moves.

The 13-operation path is verified against the saved session; it is **not claimed to be mathematically shortest**.

### Transcription notes

- The actual named challenge is authoritative for gesture and lens placement. The journal uses one-based `○@(x,y)` coordinates, converted from the zero-based `parameters.lensAnchor` arrays.
- Operation 6 was written as `○@(3,4)` in the cleaned-up notes, but the challenge specifies `[2,2]`, so the correct placement is **`○@(3,3)`**. This is a lens-coordinate correction, not a change to the operation.
- Operations 1 and 2 replace the earlier written names `Shuffle.SquareTriangle` and `Shuffle.CircleTriangle` with equivalent shorthand `□→△` and `○→△`.
- The saved 13-operation recording identifies operation 11 as `△→○`, whereas the cleaned-up notes has `☆→○`.
- The saved recording identifies operation 12 as `○→△`, whereas the cleaned-up notes has `○→☆`.
- The operation 11 and 12 symbols above follow the recording; the original observations have been retained separately in spirit and wording.

**Companion challenge:** `ShuffleLogic - Game 1 (13 shuffles)` in `challenges.json`;