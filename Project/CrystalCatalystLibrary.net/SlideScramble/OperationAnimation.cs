namespace SlideScramble;

public enum OperationAnimationStyle
{
    Instant,
    Primitive,
    Logical
}

public readonly record struct OperationTileMotion(
    int TileId,
    int SourceX,
    int SourceY,
    int DestinationX,
    int DestinationY,
    int DeltaX,
    int DeltaY,
    int PartnerTileId);

public readonly record struct OperationAnimationFrame(int PrimitiveIndex, float Progress, bool IsComplete);

public readonly record struct OperationTilePose(float CenterX, float CenterY, float Scale);

/// <summary>Immutable visual snapshots derived from an authorized operation's exact primitive expansion.</summary>
public sealed class OperationAnimationPlan
{
    private readonly int[,] startingCells;
    private readonly int[,] finalCells;
    private readonly int[][,] primitiveBoundaries;
    private readonly IReadOnlyList<RecordedMove> primitiveMoves;
    private readonly IReadOnlyList<OperationTileMotion>[] primitiveMotions;
    private readonly IReadOnlyList<OperationTileMotion> logicalMotions;

    public string OperationId { get; }
    public int Width { get; }
    public int Height { get; }
    public int PrimitiveCount => primitiveBoundaries.Length - 1;
    public IReadOnlyList<RecordedMove> PrimitiveMoves => primitiveMoves;
    public IReadOnlyList<OperationTileMotion> LogicalMotions => logicalMotions;
    public bool SupportsLogicalAnimation => logicalMotions.Count > 0;
    public int? CenterBypassTileId { get; }

    public OperationAnimationPlan(OperationBoardSnapshot board, OperationPlan operation)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(operation);
        if (operation.Purpose != OperationPlanPurpose.Solve)
            throw new ArgumentException("Only authorized solve operations can be animated as player moves.",
                nameof(operation));
        if (operation.SourceBoard.Width != board.Width || operation.SourceBoard.Height != board.Height ||
            !BoardsEqual(operation.SourceBoard.Cells, board.Cells))
            throw new ArgumentException("The operation was resolved for a different starting board.",
                nameof(operation));

        OperationId = operation.OperationId;
        Width = board.Width;
        Height = board.Height;
        startingCells = board.Cells;
        var moves = operation.Expansion.ToArray();
        primitiveMoves = Array.AsReadOnly(moves);
        primitiveBoundaries = new int[operation.Expansion.Count + 1][,];
        primitiveBoundaries[0] = (int[,])startingCells.Clone();
        for (int i = 0; i < moves.Length; i++)
        {
            int[,] next = (int[,])primitiveBoundaries[i].Clone();
            Apply(next, moves[i]);
            primitiveBoundaries[i + 1] = next;
        }

        finalCells = (int[,])primitiveBoundaries[^1].Clone();
        primitiveMotions = Enumerable.Range(0, moves.Length)
            .Select(index => (IReadOnlyList<OperationTileMotion>)Array.AsReadOnly(BuildPrimitiveMotions(
                primitiveBoundaries[index], primitiveBoundaries[index + 1], moves[index], Width, Height)))
            .ToArray();
        OperationTileMotion[] logical = BuildMotions(startingCells, finalCells, Width, Height);
        int expectedLogicalTiles = OperationId switch
        {
            PuzzleOperationIds.Shuffle => 3,
            PuzzleOperationIds.ColumnSwap or PuzzleOperationIds.RowSwap => 4,
            _ => 0
        };
        logicalMotions = Array.AsReadOnly(logical.Length == expectedLogicalTiles
            ? logical.OrderBy(motion => motion.TileId).ToArray()
            : Array.Empty<OperationTileMotion>());
        CenterBypassTileId = OperationId == PuzzleOperationIds.Shuffle
            ? logicalMotions.OrderByDescending(motion => motion.DeltaX * motion.DeltaX + motion.DeltaY * motion.DeltaY)
                .ThenBy(motion => motion.TileId).Select(motion => (int?)motion.TileId).FirstOrDefault()
            : null;
    }

    public int[,] StartingCells => (int[,])startingCells.Clone();
    public int[,] FinalCells => (int[,])finalCells.Clone();

    public int TileAtStart(int x, int y) => startingCells[y, x];
    public int TileAtFinal(int x, int y) => finalCells[y, x];

    public int TileAtPrimitiveStart(int primitiveIndex, int x, int y)
    {
        if (primitiveIndex < 0 || primitiveIndex >= PrimitiveCount)
            throw new ArgumentOutOfRangeException(nameof(primitiveIndex));
        return primitiveBoundaries[primitiveIndex][y, x];
    }

    public int[,] PrimitiveStartCells(int primitiveIndex)
    {
        if (primitiveIndex < 0 || primitiveIndex >= PrimitiveCount)
            throw new ArgumentOutOfRangeException(nameof(primitiveIndex));
        return (int[,])primitiveBoundaries[primitiveIndex].Clone();
    }

    public int[,] PrimitiveEndCells(int primitiveIndex)
    {
        if (primitiveIndex < 0 || primitiveIndex >= PrimitiveCount)
            throw new ArgumentOutOfRangeException(nameof(primitiveIndex));
        return (int[,])primitiveBoundaries[primitiveIndex + 1].Clone();
    }

    public IReadOnlyList<OperationTileMotion> GetPrimitiveMotions(int primitiveIndex)
    {
        if (primitiveIndex < 0 || primitiveIndex >= PrimitiveCount)
            throw new ArgumentOutOfRangeException(nameof(primitiveIndex));
        return primitiveMotions[primitiveIndex];
    }

    public OperationTilePose GetLogicalPose(int tileId, float progress)
    {
        OperationTileMotion motion = logicalMotions.FirstOrDefault(item => item.TileId == tileId);
        if (!logicalMotions.Any(item => item.TileId == tileId))
            throw new ArgumentOutOfRangeException(nameof(tileId), "The tile does not move in this operation.");

        float t = Math.Clamp(progress, 0, 1);
        float eased = Ease(t);
        float fraction;
        float scale;
        if (OperationId is PuzzleOperationIds.ColumnSwap or PuzzleOperationIds.RowSwap)
        {
            if (t <= 0.5f)
            {
                float phase = Ease(t * 2);
                fraction = phase * 0.5f;
                scale = 1f + (0.22f - 1f) * phase;
            }
            else
            {
                float phase = Ease((t - 0.5f) * 2);
                fraction = 0.5f + phase * 0.5f;
                scale = 0.22f + (1f - 0.22f) * phase;
            }
        }
        else
        {
            fraction = eased;
            scale = motion.TileId == CenterBypassTileId
                ? 1f - 0.3f * MathF.Sin(MathF.PI * eased)
                : 1f;
        }

        float centerX = motion.SourceX + 0.5f + motion.DeltaX * fraction;
        float centerY = motion.SourceY + 0.5f + motion.DeltaY * fraction;
        if (OperationId is PuzzleOperationIds.ColumnSwap or PuzzleOperationIds.RowSwap)
        {
            OperationTileMotion canonicalMotion = tileId < motion.PartnerTileId
                ? motion
                : logicalMotions.First(item => item.TileId == motion.PartnerTileId);
            float length = MathF.Sqrt(canonicalMotion.DeltaX * canonicalMotion.DeltaX +
                                      canonicalMotion.DeltaY * canonicalMotion.DeltaY);
            if (length > 0)
            {
                float separation = 0.16f * MathF.Sin(MathF.PI * t) *
                                   (tileId < motion.PartnerTileId ? -1 : 1);
                centerX -= canonicalMotion.DeltaY / length * separation;
                centerY += canonicalMotion.DeltaX / length * separation;
            }
        }

        return new OperationTilePose(centerX, centerY, scale);
    }

    internal static float Ease(float progress)
    {
        float value = Math.Clamp(progress, 0, 1);
        return value * value * (3f - 2f * value);
    }

    private static OperationTileMotion[] BuildMotions(int[,] before, int[,] after, int width, int height)
    {
        var beforePositions = new (int X, int Y)[width * height];
        var afterPositions = new (int X, int Y)[width * height];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            beforePositions[before[y, x]] = (x, y);
            afterPositions[after[y, x]] = (x, y);
        }

        var motions = new List<OperationTileMotion>();
        for (int tileId = 0; tileId < beforePositions.Length; tileId++)
        {
            var source = beforePositions[tileId];
            var destination = afterPositions[tileId];
            if (source == destination) continue;
            int partnerTileId = before[destination.Y, destination.X];
            motions.Add(new OperationTileMotion(tileId, source.X, source.Y, destination.X, destination.Y,
                OperationAnimationGeometry.ShortestDelta(source.X, destination.X, width),
                OperationAnimationGeometry.ShortestDelta(source.Y, destination.Y, height), partnerTileId));
        }

        return motions.ToArray();
    }

    private static OperationTileMotion[] BuildPrimitiveMotions(int[,] before, int[,] after, RecordedMove move,
        int width, int height)
    {
        int length = move.Axis == RecordedMoveAxis.Row ? width : height;
        int displacement = (int)(-((long)move.Delta % length));
        return BuildMotions(before, after, width, height).Select(motion => motion with
        {
            DeltaX = move.Axis == RecordedMoveAxis.Row ? displacement : 0,
            DeltaY = move.Axis == RecordedMoveAxis.Column ? displacement : 0
        }).ToArray();
    }

    private static void Apply(int[,] cells, RecordedMove move)
    {
        int lineCount = move.Axis == RecordedMoveAxis.Row ? cells.GetLength(0) : cells.GetLength(1);
        if (move.Index < 0 || move.Index >= lineCount)
            throw new ArgumentException("Operation expansion contains a line outside the board.", nameof(move));
        if (move.Axis == RecordedMoveAxis.Row) LogicalMoves.MoveRow(cells, move.Index, move.Delta);
        else if (move.Axis == RecordedMoveAxis.Column) LogicalMoves.MoveColumn(cells, move.Index, move.Delta);
        else throw new ArgumentException("Operation expansion contains an unknown axis.", nameof(move));
    }

    private static bool BoardsEqual(int[,] left, int[,] right)
    {
        if (left.GetLength(0) != right.GetLength(0) || left.GetLength(1) != right.GetLength(1)) return false;
        for (int y = 0; y < left.GetLength(0); y++)
        for (int x = 0; x < left.GetLength(1); x++)
            if (left[y, x] != right[y, x])
                return false;
        return true;
    }
}

public static class OperationAnimationPolicy
{
    public static OperationAnimationStyle Select(OperationPlan operation, bool logicalAnimationAvailable,
        OperationAnimationStyle? preference = null)
    {
        ArgumentNullException.ThrowIfNull(operation);
        if (preference == OperationAnimationStyle.Instant) return OperationAnimationStyle.Instant;
        if (preference == OperationAnimationStyle.Primitive) return OperationAnimationStyle.Primitive;
        if (preference == OperationAnimationStyle.Logical)
            return logicalAnimationAvailable ? OperationAnimationStyle.Logical : OperationAnimationStyle.Primitive;

        return operation.Accounting == OperationMoveAccounting.LogicalOperation && logicalAnimationAvailable
            ? OperationAnimationStyle.Logical
            : OperationAnimationStyle.Primitive;
    }
}

public static class OperationAnimationGeometry
{
    public static int ShortestDelta(int source, int destination, int length)
    {
        if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length));
        int delta = destination - source;
        if (delta > length / 2) delta -= length;
        else if (delta < -length / 2) delta += length;
        return delta;
    }

    public static IReadOnlyList<(float X, float Y)> GetVisibleWrappedCopies(float centerX, float centerY,
        float tileWidth, float tileHeight, float boardWidth, float boardHeight)
    {
        if (tileWidth <= 0 || tileHeight <= 0 || boardWidth <= 0 || boardHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(tileWidth), "Tile and board dimensions must be positive.");

        float[] xOffsets = centerX - tileWidth / 2 < 0 ? new[] { 0f, boardWidth } :
            centerX + tileWidth / 2 > boardWidth ? new[] { 0f, -boardWidth } : new[] { 0f };
        float[] yOffsets = centerY - tileHeight / 2 < 0 ? new[] { 0f, boardHeight } :
            centerY + tileHeight / 2 > boardHeight ? new[] { 0f, -boardHeight } : new[] { 0f };
        return Array.AsReadOnly((from xOffset in xOffsets
            from yOffset in yOffsets
            select (centerX + xOffset, centerY + yOffset)).ToArray());
    }
}

/// <summary>Time-based presentation lifecycle; cancellation never mutates the operation's source board.</summary>
public sealed class OperationAnimationTimeline
{
    public const double PrimitiveStepDurationSeconds = 0.16;
    public const double LogicalDurationSeconds = 0.62;

    private bool completed;
    private bool cancelled;
    private bool commitAttempted;

    public OperationAnimationPlan Plan { get; }
    public OperationAnimationStyle Style { get; }
    public bool IsCompleted => completed;
    public bool IsCancelled => cancelled;

    public double DurationSeconds => Style switch
    {
        OperationAnimationStyle.Primitive => Plan.PrimitiveCount * PrimitiveStepDurationSeconds,
        OperationAnimationStyle.Logical => LogicalDurationSeconds,
        _ => 0
    };

    public OperationAnimationTimeline(OperationAnimationPlan plan, OperationAnimationStyle style)
    {
        Plan = plan ?? throw new ArgumentNullException(nameof(plan));
        if (style == OperationAnimationStyle.Instant)
            throw new ArgumentException("Instant presentation does not use an animation timeline.", nameof(style));
        if (plan.PrimitiveCount == 0)
            throw new ArgumentException("An animation needs at least one primitive move.", nameof(plan));
        if (style == OperationAnimationStyle.Logical && plan.LogicalMotions.Count == 0)
            throw new ArgumentException("This operation has no supported logical animation.", nameof(plan));
        Style = style;
    }

    public OperationAnimationFrame GetFrame(double elapsedSeconds)
    {
        if (cancelled) throw new InvalidOperationException("A cancelled animation has no active frame.");
        if (elapsedSeconds < 0) elapsedSeconds = 0;
        double duration = DurationSeconds;
        if (elapsedSeconds >= duration)
        {
            completed = true;
            return new OperationAnimationFrame(Plan.PrimitiveCount - 1, 1, true);
        }

        if (Style == OperationAnimationStyle.Primitive)
        {
            int index = Math.Min(Plan.PrimitiveCount - 1, (int)(elapsedSeconds / PrimitiveStepDurationSeconds));
            float progress = (float)((elapsedSeconds - index * PrimitiveStepDurationSeconds) /
                                     PrimitiveStepDurationSeconds);
            return new OperationAnimationFrame(index, OperationAnimationPlan.Ease(progress), false);
        }

        return new OperationAnimationFrame(0, OperationAnimationPlan.Ease((float)(elapsedSeconds / duration)), false);
    }

    public bool TryCommit(Action commit)
    {
        ArgumentNullException.ThrowIfNull(commit);
        if (!completed || cancelled || commitAttempted) return false;
        commitAttempted = true;
        commit();
        return true;
    }

    public bool Cancel()
    {
        if (completed || commitAttempted || cancelled) return false;
        cancelled = true;
        return true;
    }
}