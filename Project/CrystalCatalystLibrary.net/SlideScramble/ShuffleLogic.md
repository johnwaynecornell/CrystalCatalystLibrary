# ShuffleLogic and the Portable Lens

Classic SlideScramble is the startup preset. Open the Space command menu and select **Game Mode (Classic / ShuffleLogic)** to switch presets; switching resets the board. ShuffleLogic requires at least a 3×3 board. **Generate Challenge** retains its 1–10 scale and generates Shuffle-only scrambles with a verified inverse solution, available through the session controls.

Choose **Position Portable Lens** in the Space menu, click the upper-left anchor cell, and press Enter. The four marked cells wrap across either board edge. Drag from one marked symbol to a horizontally or vertically adjacent symbol; the accepted lens stays in place for subsequent drags. Escape cancels the lens; focus loss abandons an unfinished drag. Ctrl selects a swap family but swaps are not enabled or executable in this release. In Classic, normal board dragging is unchanged.

The canonical symbol layout is `○ ☆ / △ □`. A gesture moves the original tile at the start symbol to the target symbol. The following mapping was derived from the marked results in the verified Shuffle Atlas, not from its historical formula names:

| Gesture | Atlas entry |
| --- | --- |
| Circle → Star | `Lens.TriangleStar.A` |
| Star → Circle | `Lens.StarTriangle.A` |
| Star → Square | `Lens.CircleSquare.A` |
| Square → Star | `Lens.SquareCircle.A` |
| Circle → Triangle | `Lens.CircleSquare.B` |
| Triangle → Circle | `Lens.SquareCircle.B` |
| Triangle → Square | `Lens.TriangleStar.B` |
| Square → Triangle | `Lens.StarTriangle.B` |

Each resolver follows the four atlas rotations using the original tile identities, even after they move. The resolver is pure; the rule evaluator checks the complete expansion and the recording controller applies it atomically. ShuffleLogic permits Shuffle for scrambling, solving, and UI input, counting each Shuffle as one logical move. Other rules can instead allow a UI Shuffle to expand into four solve-allowed Slides. Swaps remain reserved without resolvers.

Version 3 recordings store the symbolic gesture and exact primitive rotations with the rule snapshot and accounting. Playback uses primitive positions; continuation is allowed only at operation boundaries, and retained score is the sum of operation costs. A generated solution is a legal inverse sequence, not necessarily a shortest solution. Board editing is disabled in ShuffleLogic to avoid introducing arbitrary unsolvable arrangements.