# SlideScramble Session Recording v5

Version 5 recordings use `format: "SlideScramble"` and contain the board dimensions, a one-based `initialCells` arrangement, one immutable `ruleSet` snapshot, and an ordered `operations` array. The arrays in `ruleSet` (`scrambleAllowed`, `solveAllowed`, and `uiEnabled`) store stable operation IDs; they are snapshots, not references to mutable named presets. Board coordinates in JSON are one-based; the game and operation model remain zero-based.

## Single authoritative history

`operations` is the canonical execution history. Every operation record contains its stable `operationId`, operation-specific JSON-object `parameters`, `purpose` (`Solve` or `Scramble`), `accounting`, `moveCost`, `isInverse`, and its exact ordered `moves` expansion. Unlike the legacy v1–v2 format, v3–v5 have no top-level `moves` array. `SessionRecording.Moves` is a derived, flattened view for playback, never a second serialized history.

Each primitive move preserves its `axis` and signed `delta`. Primitive `index` and `[column, row]` `dragStart` coordinates use the established one-based convention in versions 2 and later; version 1 uses its historical zero-based convention. Operation-parameter conversion does not change these primitive coordinates. In logical operation parameters, `lensAnchor` is a structured `[column, row]` array, and the `index` parameter for `Slide` is zero-based in v3 and one-based in v4 and v5. Playback and goal verification advance by primitive moves; operation boundaries are derived from each record's expansion length.

## Accounting and replay

A solve operation authorized directly by `solveAllowed` has `LogicalOperation` accounting and costs one. A UI-enabled operation expanded through solve-allowed `Slide` primitives has `PrimitiveMoves` accounting and costs the expansion length. A scramble operation must appear in `scrambleAllowed`, uses `None`, and has a null cost. These values are validated when a recording is constructed or loaded and are retained during replay and reversal; scores are not recomputed from a later ruleset.

Saved primitive expansions are sufficient to replay a known operation identity even when its resolver is not installed. Loading never executes resolver or plugin code. Unknown operation IDs and invalid rule-set references are rejected. Operation-specific parameters remain data-only JSON objects; `lensAnchor` receives structural and board-bounds validation, while `startCell` and `targetCell` are symbolic cell names and are not coordinates. Slide `axis` and signed `delta` are unchanged; `index` is the only other positional parameter currently serialized. Counts, directions, variants, offsets, and symbolic values are not shifted. Resolver-specific semantic validation belongs to the operation layer when accepting new requests.

Reversal stores the inverse primitive sequence in reverse order and marks the operation `isInverse`, while preserving its identity, parameters, purpose, and original accounting. If negating a primitive `int.MinValue` delta would overflow, reversal uses a mathematically equivalent representable delta. Double reversal restores the original board transformation, operation boundaries, and accounting. A verified goal index is a primitive playback position.

## Legacy and continuation behavior

Versions 1 and 2 continue to load as classic `Slide` operations under the classic `Slide` rule set. Version 1 uses zero-based tile and primitive move coordinates; versions 2 and 3 use one-based tile and primitive move coordinates. Version 3 logical operation `lensAnchor` and `Slide.index` values are zero-based, while versions 4 and 5 use one-based values for those fields. All imports normalize positions to the existing zero-based internal model, and signed deltas are preserved. Saving any loaded recording writes version 5.

Version 5 establishes the [corrected Shuffle gesture convention](ShuffleLogic.md) for new operations. An imported Shuffle keeps its original gesture metadata and primitive history, even when the metadata describes an earlier convention. Playback, animation, and reversal follow the stored primitives. Re-export preserves logical operation boundaries, rule snapshots, and move costs.

Continue Recording is allowed only at an operation boundary. Cropping keeps complete operations before that boundary, along with the original initial board and rule snapshot; the source recording remains unchanged until the edited recording is saved. Zero-operation recordings retain their board and rule snapshot.

## API boundary

Use `SessionRecording.FromJson` to validate and load a recording, `ToJson` to save it, and `SessionRecordingController` for primitive playback, reversal, and boundary-aware continuation. Version-aware conversion is confined to the recording serialization boundary; it does not change gameplay math, animation geometry, input gestures, or internal operation indexing. The new clipboard JSON format is independent of session-recording versioning. `OperationRuleEvaluator` remains responsible for authorization and accounting plans; the controller validates the plan against the live board and recording rules before applying it.
