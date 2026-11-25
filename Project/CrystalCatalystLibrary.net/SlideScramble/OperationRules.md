# Operation and rule architecture

`IPuzzleOperation` resolves an immutable `IOperationIntent` against an `OperationBoardSnapshot`. It returns an `OperationResolution` containing a stable operation ID and an ordered expansion in the existing `RecordedMove` row/column primitives. Resolution is deterministic and does not apply moves. `SlideOperation` is the classic adapter; the registry reserves `Shuffle`, `ColumnSwap`, and `RowSwap` identities, but does not register unverified lens resolvers.

`PuzzleRuleSet` snapshots three separate ID sets: `ScrambleAllowed`, `SolveAllowed`, and `UIEnabled`. The default constructor creates classic SlideScramble rules. Duplicate IDs are deduplicated; IDs not known by the supplied `OperationRegistry` are rejected. Adding an operation to `UIEnabled` does not authorize it as a solve move.

`OperationRuleEvaluator` is independent of `Window` and the live animation/scoring system. A UI request first requires `UIEnabled`. If the operation is directly `SolveAllowed`, its plan costs one logical move. Otherwise, it may fall back to its validated Slide expansion only when Slide is `SolveAllowed`, and its move cost is the number of primitive moves. Scramble requests use only `ScrambleAllowed` and carry no solve move cost. Missing resolvers, malformed parameters, invalid expansions, and unauthorized requests produce rejected evaluations rather than partial mutations.

An accepted `OperationPlan` contains its intent, source-board snapshot, ordered primitive expansion, purpose, and accounting policy. Applying the plan remains a separate future integration boundary: an executor should confirm the live board still matches `SourceBoard` and commit the complete expansion transactionally before updating the score. Animation count remains independent from the plan's `MoveCost`.

This merge does not route existing input through the evaluator, change gameplay scoring, or alter the version-2 recording format. Future recording version 3 can persist an operation identity, a typed immutable intent, the rule-set snapshot, and the chosen accounting semantics while retaining primitive expansions for deterministic replay.
