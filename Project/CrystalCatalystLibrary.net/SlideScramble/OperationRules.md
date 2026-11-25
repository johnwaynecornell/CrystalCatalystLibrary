# Operation and rule architecture

`IPuzzleOperation` resolves an immutable `IOperationIntent` against an `OperationBoardSnapshot`. It returns an `OperationResolution` containing a stable operation ID and an ordered expansion in the existing `RecordedMove` row/column primitives. Resolution is deterministic and does not apply moves. The default registry includes the classic `SlideOperation` and verified `ShuffleOperation`; `ColumnSwap` and `RowSwap` remain reserved without resolvers.

`PuzzleRuleSet` snapshots three separate ID sets: `ScrambleAllowed`, `SolveAllowed`, and `UIEnabled`. The default constructor creates classic SlideScramble rules. Duplicate IDs are deduplicated; IDs not known by the supplied `OperationRegistry` are rejected. Adding an operation to `UIEnabled` does not authorize it as a solve move.

`OperationRuleEvaluator` is independent of `Window` and the live animation/scoring system. A UI request first requires `UIEnabled`. If the operation is directly `SolveAllowed`, its plan costs one logical move. Otherwise, it may fall back to its validated Slide expansion only when Slide is `SolveAllowed`, and its move cost is the number of primitive moves. Scramble requests use only `ScrambleAllowed` and carry no solve move cost. Missing resolvers, malformed parameters, invalid expansions, and unauthorized requests produce rejected evaluations rather than partial mutations.

An accepted `OperationPlan` contains its intent, source-board snapshot, ordered primitive expansion, purpose, and accounting policy. `SessionRecordingController.ApplyOperation` checks the live board against `SourceBoard` and commits the complete expansion transactionally; the window updates the score only after success. Animation count remains independent from the plan's `MoveCost`.

The shared lens routes Shuffle gestures through the evaluator; Classic dragging keeps its existing behavior. Version 3 recordings persist operation parameters, the rule-set snapshot, accounting, and exact primitive expansions for deterministic replay. See [ShuffleLogic](ShuffleLogic.md) for controls and atlas mapping.
