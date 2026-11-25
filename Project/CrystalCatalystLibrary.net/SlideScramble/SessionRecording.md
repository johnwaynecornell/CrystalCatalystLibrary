# SlideScramble Session Recording v3

Version 3 recordings use `format: "SlideScramble"` and contain the board dimensions, a one-based `initialCells` arrangement, one immutable `ruleSet` snapshot, and an ordered `operations` array. The arrays in `ruleSet` (`scrambleAllowed`, `solveAllowed`, and `uiEnabled`) store stable operation IDs; they are snapshots, not references to mutable named presets.

## Single authoritative history

`operations` is the canonical execution history. Every operation record contains its stable `operationId`, operation-specific JSON-object `parameters`, `purpose` (`Solve` or `Scramble`), `accounting`, `moveCost`, `isInverse`, and its exact ordered `moves` expansion. There is deliberately no top-level v3 `moves` array. `SessionRecording.Moves` is a derived, flattened view for playback, never a second serialized history.

Each primitive move preserves the existing `axis`, one-based `index`, one-based `[column, row]` `dragStart`, and unchanged `delta`. Playback and goal verification advance by primitive moves; operation boundaries are derived from each record's expansion length.

## Accounting and replay

A solve operation authorized directly by `solveAllowed` has `LogicalOperation` accounting and costs one. A UI-enabled operation expanded through solve-allowed `Slide` primitives has `PrimitiveMoves` accounting and costs the expansion length. A scramble operation must appear in `scrambleAllowed`, uses `None`, and has a null cost. These values are validated when a recording is constructed or loaded and are retained during replay and reversal; scores are not recomputed from a later ruleset.

Saved primitive expansions are sufficient to replay a known operation identity even when its resolver is not installed. Loading never executes resolver or plugin code. Unknown operation IDs and invalid rule-set references are rejected. Operation-specific parameters remain data-only JSON objects; `lensAnchor`, `startCell`, and `targetCell` receive basic structural and bounds validation where present. Resolver-specific semantic validation belongs to the operation layer when accepting new requests.

Reversal stores the inverse primitive sequence in reverse order and marks the operation `isInverse`, while preserving its identity, parameters, purpose, and original accounting. Double reversal restores the original operation history. A verified goal index is a primitive playback position.

## Legacy and continuation behavior

Versions 1 and 2 continue to load as classic `Slide` operations under the classic `Slide` rule set. Version 1 retains its zero-based coordinates; version 2 and version 3 use the established one-based coordinate convention. Their move deltas and drag-start coordinates are preserved. Saving any loaded legacy recording writes version 3.

Continue Recording is allowed only at an operation boundary. Cropping keeps complete operations before that boundary, along with the original initial board and rule snapshot; the source recording remains unchanged until the edited recording is saved. Zero-operation recordings retain their board and rule snapshot.

## API boundary

Use `SessionRecording.FromJson` to validate and load a recording, `ToJson` to save it, and `SessionRecordingController` for primitive playback, reversal, and boundary-aware continuation. `OperationRuleEvaluator` remains responsible for authorization and accounting plans; the controller validates the plan against the live board and recording rules before applying it. Version 3 does not introduce ShuffleLogic UI, new gesture bindings, or lens resolvers.
