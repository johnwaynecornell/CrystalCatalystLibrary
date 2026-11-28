# The Traveling Transposition

*A SlideScramble challenge about carrying a disturbance until it disappears.*

**Board:** 6 × 3  
**Preset:** TwoWideSlide  
**Allowed operations:** ColumnSwap and RowSlide  
**Example solution:** 8 logical operations (4 ColumnSwaps, 4 RowSlides)  
**Challenge:** `The Traveling Transposition` (SessionRecording v5)

## What is the challenge?

The board is almost solved. Only the final two tiles of the bottom row are exchanged:

```text
Row 1:  1  2  3  4  5  6
Row 2:  1  2  3  4  5  6
Row 3:  1  2  3  4  6  5
```

The numbers above are the tiles' **home columns**; their home rows are identified on the left. Thus the misplaced tiles are `[6,3]` and `[5,3]`.

The player may use **ColumnSwap** and **RowSlide**. A ColumnSwap exchanges two neighboring columns *in both rows touched by the lens*. A RowSlide cyclically rotates one entire row, with wraparound. The challenge is to restore the bottom row without leaving the row above it damaged.

## Why is it interesting?

It looks as though one swap should finish the puzzle. But there is no operation that swaps just those two tiles: ColumnSwap also exchanges the corresponding tiles in row 2.

There is also a parity reason not to attempt a ColumnSwap-only solution. The starting position contains one transposition, which is **odd**. Each ColumnSwap performs two transpositions and is **even**. A six-cell RowSlide by one position is odd, so RowSlide provides the parity change that ColumnSwap cannot supply alone.

The key insight is constructive rather than merely algebraic:

> Don't insist that every move immediately fixes tiles. Carry the misplaced relationship leftward, use row 2 to cancel or absorb the side effects, and stop once both active rows are cyclically ordered.

Once a row is cyclically ordered, a single RowSlide can finish it.

## How does the traveling disturbance work?

Think of row 3 as the **traveling row** and row 2 as the **working row**.

1. **Swap at the disturbance.** Use ColumnSwap on the adjacent columns containing the displaced tile and its neighbor. This moves the displaced tile one column left, but also introduces a mismatch in row 2.
2. **Use row 2 as a cancellation buffer.** When a RowSlide can align that mismatch with a subsequent ColumnSwap, take the opportunity. You need not restore row 2 to its final positions after every move—cyclic order is enough.
3. **Repeat toward the left edge.** Move the disturbance to the next pair of columns and perform another ColumnSwap, choosing row-2 adjustments as needed.
4. **Finish with cyclic alignment.** When both rows contain their correct tiles in cyclic order, rotate each row into its home position.

This is a short loop with a little logic about *when* to rotate the working row. It is not a requirement to reproduce the same offsets in every puzzle.

### The recorded eight-operation example

The table shows the **home-column numbers** in rows 2 and 3, not their tile identities in full `[column,row]` notation. Row 1 stays solved.

| Step | Operation | Row 2 | Row 3 | Traveling tile `[6,3]` |
|---|---|---|---|---|
| Start | — | `1 2 3 4 5 6` | `1 2 3 4 6 5` | Column 5 |
| 1 | ColumnSwap, columns 4–5 | `1 2 3 5 4 6` | `1 2 3 6 4 5` | Column 4 |
| 2 | Row 2 Slide, δ = +1 | `2 3 5 4 6 1` | `1 2 3 6 4 5` | Column 4 |
| 3 | ColumnSwap, columns 3–4 | `2 3 4 5 6 1` | `1 2 6 3 4 5` | Column 3 |
| 4 | ColumnSwap, columns 2–3 | `2 4 3 5 6 1` | `1 6 2 3 4 5` | Column 2 |
| 5 | Row 2 Slide, δ = +1 | `4 3 5 6 1 2` | `1 6 2 3 4 5` | Column 2 |
| 6 | ColumnSwap, columns 1–2 | `3 4 5 6 1 2` | `6 1 2 3 4 5` | Column 1 |
| 7 | Row 2 Slide, δ = −2 | `1 2 3 4 5 6` | `6 1 2 3 4 5` | Column 1 |
| 8 | Row 3 Slide, δ = +1 | `1 2 3 4 5 6` | `1 2 3 4 5 6` | Solved |

*Slide deltas use the recording's convention: positive values read the next cell into the current position, so `δ = +1` appears visually as a one-cell shift to the left.*

Notice how the row-2 mistake introduced in step 1 is repaired in step 3 after one row rotation. The same idea recurs in steps 4–6. That is the **cancellation opportunity**: rather than pay to undo the helper row immediately, arrange for the next useful ColumnSwap to do the repair while moving the disturbance onward.

At step 6, both active rows are cyclically ordered:

```text
Row 2:  3  4  5  6  1  2
Row 3:  6  1  2  3  4  5
```

Neither row needs more local surgery. The final two RowSlides complete the solution.

## What should the player learn?

- **Progress need not look solved.** Correct tile counts may fall while the underlying arrangement becomes easier.
- **The other row is a resource.** A paired ColumnSwap creates a side effect, but the side effect can be deliberately repositioned and canceled.
- **Cyclic order is a useful intermediate goal.** A rotated solved row is only one RowSlide away from completion.
- **Parity suggests which operation families must cooperate.** ColumnSwap alone cannot remove an odd transposition; the available RowSlide changes what is reachable.

The example uses eight logical operations, each permitted by the TwoWideSlide preset and therefore scored as one logical move. It is a recorded *constructive solution*, not a claim of optimality.

**The traveling transposition is a small puzzle with a large idea: sometimes the quickest path to order is to let a disturbance travel until its side effects cancel.**
