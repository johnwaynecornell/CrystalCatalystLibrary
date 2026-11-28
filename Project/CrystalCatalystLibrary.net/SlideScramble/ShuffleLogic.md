# ShuffleLogic and the Portable Lens

Classic SlideScramble is the startup preset. Open the Space command menu and select **Game Mode (Classic / ShuffleLogic)** to switch presets; switching resets the board. ShuffleLogic requires at least a 3×3 board. **Generate Challenge** retains its 1–10 scale and generates Shuffle-only scrambles with a verified inverse solution, available through the session controls.

Choose **Position Portable Lens** in the Space menu, click the upper-left anchor cell, and press Enter. The four marked cells wrap across either board edge. Drag from one marked symbol to a horizontally or vertically adjacent symbol; the accepted lens stays in place for subsequent drags. Escape cancels the lens; focus loss abandons an unfinished drag. Ctrl selects ColumnSwap on a horizontal drag or RowSwap on a vertical drag when the active rules allow that operation.

The canonical symbol layout is `○ ☆ / △ □`. The tile at the start moves to the target. The source’s other adjacent tile stays fixed, and the target tile moves to the source’s diagonal corner. The diagonal tile returns to the source. The four-step [Shuffle Atlas formulas](PortableLens.md#shuffle-atlas) retain their mathematical definitions.

| Gesture | Stationary symbol | Atlas entry | Inverse gesture |
| --- | --- | --- | --- |
| Circle → Star | Triangle | `Lens.CircleSquare.A` | Square → Star |
| Star → Circle | Square | `Lens.StarTriangle.A` | Triangle → Circle |
| Star → Square | Circle | `Lens.StarTriangle.B` | Triangle → Square |
| Square → Star | Triangle | `Lens.SquareCircle.A` | Circle → Star |
| Circle → Triangle | Star | `Lens.CircleSquare.B` | Square → Triangle |
| Triangle → Circle | Square | `Lens.TriangleStar.A` | Star → Circle |
| Triangle → Square | Circle | `Lens.TriangleStar.B` | Star → Square |
| Square → Triangle | Star | `Lens.SquareCircle.B` | Circle → Triangle |

The inverse drag starts at the opposite corner and ends at the same target. Applying the same gesture three times also restores the board. Each resolver follows the four atlas rotations using the original tile identities as they move. The rule evaluator checks the complete expansion, and the recording controller applies it atomically. ShuffleLogic counts each Shuffle as one logical move. Other rules can allow a Shuffle to expand into four solve-allowed Slides.

[Recording v5](SessionRecording.md) introduces this gesture convention for new Shuffles. Historical gesture labels stay with their stored primitive rotations, which determine playback, reversal, and the stationary cell in the animation. Saving a historical session as v5 preserves its operation boundaries and move accounting. A generated challenge includes a verified inverse solution. Board editing is disabled in ShuffleLogic to avoid introducing arbitrary unsolvable arrangements.
