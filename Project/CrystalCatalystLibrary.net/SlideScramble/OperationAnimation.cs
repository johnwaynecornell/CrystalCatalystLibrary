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

/// <summary>Immutable visual snapshots derived from an operation's exact primitive expansion.</summary>
public sealed class OperationAnimationPlan
{
    private readonly record struct LensEdgeRoute(
        float SourceX,
        float SourceY,
        float ExitX,
        float ExitY,
        float EntryX,
        float EntryY,
        float DestinationX,
        float DestinationY);

    private enum LensSide
    {
        Top,
        Right,
        Bottom,
        Left
    }

    private readonly int[,] startingCells;
    private readonly int[,] finalCells;
    private readonly int[][,] primitiveBoundaries;
    private readonly IReadOnlyList<RecordedMove> primitiveMoves;
    private readonly IReadOnlyList<OperationTileMotion>[] primitiveMotions;
    private readonly IReadOnlyList<OperationTileMotion> logicalMotions;
    private readonly Dictionary<int, LensEdgeRoute> lensEdgeRoutes = new();

    public string OperationId { get; }
    public int Width { get; }
    public int Height { get; }
    public int PrimitiveCount => primitiveBoundaries.Length - 1;
    public IReadOnlyList<RecordedMove> PrimitiveMoves => primitiveMoves;
    public IReadOnlyList<OperationTileMotion> LogicalMotions => logicalMotions;
    public bool SupportsLogicalAnimation => logicalMotions.Count > 0;
    public int? CenterBypassTileId { get; }
    public int? LensAnchorX { get; }
    public int? LensAnchorY { get; }
    public IReadOnlyList<(int X, int Y)> LensCells { get; }

    public OperationAnimationPlan(OperationBoardSnapshot board, OperationPlan operation, bool allowLogicalAnimation = true)
    {
        ArgumentNullException.ThrowIfNull(board);
        ArgumentNullException.ThrowIfNull(operation);
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
        logicalMotions = Array.AsReadOnly(allowLogicalAnimation && logical.Length == expectedLogicalTiles
            ? logical.OrderBy(motion => motion.TileId).ToArray()
            : Array.Empty<OperationTileMotion>());

        if (OperationId == PuzzleOperationIds.Shuffle && operation.Intent is ShuffleOperationIntent shuffle)
        {
            LensAnchorX = shuffle.AnchorX;
            LensAnchorY = shuffle.AnchorY;
            var lensCells = Enum.GetValues<PortableLensCell>()
                .Select(cell => LensBoardPosition(shuffle, cell, Width, Height)).ToArray();
            LensCells = Array.AsReadOnly(lensCells);
            BuildLensEdgeRoute(shuffle, lensCells);
        }
        else
        {
            LensCells = Array.Empty<(int X, int Y)>();
        }

        int? diagonalTileId = lensEdgeRoutes.Keys.Select(tileId => (int?)tileId).FirstOrDefault();
        CenterBypassTileId = OperationId == PuzzleOperationIds.Shuffle
            ? logicalMotions.Where(motion => motion.TileId != diagonalTileId)
                .OrderByDescending(motion => motion.DeltaX * motion.DeltaX + motion.DeltaY * motion.DeltaY)
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

    public IReadOnlyList<OperationTilePose> GetLogicalPoses(int tileId, float progress)
    {
        int poseCount = GetLogicalPoseCount(tileId);
        var poses = new OperationTilePose[poseCount];
        for (int i = 0; i < poseCount; i++) poses[i] = GetLogicalPose(tileId, progress, i);
        return Array.AsReadOnly(poses);
    }

    public int GetLogicalPoseCount(int tileId)
    {
        if (!logicalMotions.Any(motion => motion.TileId == tileId))
            throw new ArgumentOutOfRangeException(nameof(tileId), "The tile does not move in this operation.");
        return lensEdgeRoutes.ContainsKey(tileId) ? 2 : 1;
    }

    public OperationTilePose GetLogicalPose(int tileId, float progress, int poseIndex)
    {
        if (!lensEdgeRoutes.TryGetValue(tileId, out LensEdgeRoute route))
        {
            if (poseIndex != 0) throw new ArgumentOutOfRangeException(nameof(poseIndex));
            return GetLogicalPose(tileId, progress);
        }

        if (poseIndex is < 0 or > 1) throw new ArgumentOutOfRangeException(nameof(poseIndex));

        float eased = Ease(progress);
        return poseIndex == 0
            ? new OperationTilePose(
                route.SourceX + (route.ExitX - route.SourceX) * eased,
                route.SourceY + (route.ExitY - route.SourceY) * eased, 1)
            : new OperationTilePose(
                route.EntryX + (route.DestinationX - route.EntryX) * eased,
                route.EntryY + (route.DestinationY - route.EntryY) * eased, 1);
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

    private void BuildLensEdgeRoute(ShuffleOperationIntent intent, IReadOnlyList<(int X, int Y)> lensCells)
    {
        if (lensCells.Count != 4 || logicalMotions.Count != 3) return;

        var symbols = Enum.GetValues<PortableLensCell>();
        PortableLensCell[] fixedCells = symbols.Where((_, index) =>
        {
            var position = lensCells[index];
            return startingCells[position.Y, position.X] == finalCells[position.Y, position.X];
        }).ToArray();
        if (fixedCells.Length != 1) return;

        var positions = symbols.Select((cell, index) => (cell, position: lensCells[index]))
            .ToDictionary(entry => entry.cell, entry => entry.position);
        OperationTileMotion[] diagonalMotions = logicalMotions.Where(motion =>
            positions.Any(entry => entry.Value == (motion.SourceX, motion.SourceY)) &&
            positions.Any(entry => entry.Value == (motion.DestinationX, motion.DestinationY)) &&
            motion.SourceX != motion.DestinationX && motion.SourceY != motion.DestinationY).ToArray();
        if (diagonalMotions.Length != 1) return;

        OperationTileMotion diagonal = diagonalMotions[0];
        PortableLensCell source = positions.Single(entry => entry.Value == (diagonal.SourceX, diagonal.SourceY)).Key;
        PortableLensCell destination = positions.Single(entry => entry.Value ==
                                                                 (diagonal.DestinationX, diagonal.DestinationY)).Key;
        int sourceIndex = RingIndex(source);
        int destinationIndex = RingIndex(destination);
        int fixedIndex = RingIndex(fixedCells[0]);
        LensSide exitSide;
        LensSide entrySide;
        if (fixedIndex == (sourceIndex + 1) % 4 && destinationIndex == (sourceIndex + 2) % 4)
        {
            (exitSide, entrySide) = ClockwiseEdgeSides(sourceIndex);
        }
        else if (fixedIndex == (sourceIndex + 3) % 4 && destinationIndex == (sourceIndex + 2) % 4)
        {
            (exitSide, entrySide) = CounterClockwiseEdgeSides(sourceIndex);
        }
        else
        {
            return;
        }

        var sourceOffset = LensGestureOperationMapper.Coordinates(source);
        var destinationOffset = LensGestureOperationMapper.Coordinates(destination);
        float sourceX = intent.AnchorX + sourceOffset.X + 0.5f;
        float sourceY = intent.AnchorY + sourceOffset.Y + 0.5f;
        float destinationX = intent.AnchorX + destinationOffset.X + 0.5f;
        float destinationY = intent.AnchorY + destinationOffset.Y + 0.5f;
        var exit = OutsideCenter(intent.AnchorX, intent.AnchorY, sourceOffset, exitSide);
        var entry = OutsideCenter(intent.AnchorX, intent.AnchorY, destinationOffset, entrySide);
        lensEdgeRoutes[diagonal.TileId] = new LensEdgeRoute(
            sourceX, sourceY, exit.X, exit.Y, entry.X, entry.Y, destinationX, destinationY);
    }

    private static (int X, int Y) LensBoardPosition(ShuffleOperationIntent intent, PortableLensCell cell,
        int width, int height)
    {
        var offset = LensGestureOperationMapper.Coordinates(cell);
        return ((intent.AnchorX + offset.X) % width, (intent.AnchorY + offset.Y) % height);
    }

    private static int RingIndex(PortableLensCell cell) => cell switch
    {
        PortableLensCell.Circle => 0,
        PortableLensCell.Star => 1,
        PortableLensCell.Square => 2,
        PortableLensCell.Triangle => 3,
        _ => throw new ArgumentOutOfRangeException(nameof(cell))
    };

    private static (LensSide Exit, LensSide Entry) ClockwiseEdgeSides(int sourceIndex) => sourceIndex switch
    {
        0 => (LensSide.Top, LensSide.Right),
        1 => (LensSide.Right, LensSide.Bottom),
        2 => (LensSide.Bottom, LensSide.Left),
        3 => (LensSide.Left, LensSide.Top),
        _ => throw new ArgumentOutOfRangeException(nameof(sourceIndex))
    };

    private static (LensSide Exit, LensSide Entry) CounterClockwiseEdgeSides(int sourceIndex) => sourceIndex switch
    {
        0 => (LensSide.Left, LensSide.Bottom),
        1 => (LensSide.Top, LensSide.Left),
        2 => (LensSide.Right, LensSide.Top),
        3 => (LensSide.Bottom, LensSide.Right),
        _ => throw new ArgumentOutOfRangeException(nameof(sourceIndex))
    };

    private static (float X, float Y) OutsideCenter(int anchorX, int anchorY, (int X, int Y) cell,
        LensSide side) => side switch
    {
        LensSide.Top => (anchorX + cell.X + 0.5f, anchorY - 0.5f),
        LensSide.Right => (anchorX + 2.5f, anchorY + cell.Y + 0.5f),
        LensSide.Bottom => (anchorX + cell.X + 0.5f, anchorY + 2.5f),
        LensSide.Left => (anchorX - 0.5f, anchorY + cell.Y + 0.5f),
        _ => throw new ArgumentOutOfRangeException(nameof(side))
    };

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
        OperationAnimationStyle? preference = null, bool preferLogicalForScramble = false)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return Select(operation.Accounting, operation.Purpose, logicalAnimationAvailable, preference,
            preferLogicalForScramble);
    }

    public static OperationAnimationStyle Select(OperationMoveAccounting accounting, OperationPlanPurpose purpose,
        bool logicalAnimationAvailable, OperationAnimationStyle? preference = null, bool preferLogicalForScramble = false)
    {
        if (preference == OperationAnimationStyle.Instant) return OperationAnimationStyle.Instant;
        if (preference == OperationAnimationStyle.Primitive) return OperationAnimationStyle.Primitive;
        if (preference == OperationAnimationStyle.Logical)
            return logicalAnimationAvailable ? OperationAnimationStyle.Logical : OperationAnimationStyle.Primitive;

        bool preferLogical = accounting == OperationMoveAccounting.LogicalOperation ||
                             (preferLogicalForScramble && purpose == OperationPlanPurpose.Scramble);
        return preferLogical && logicalAnimationAvailable
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
        OperationAnimationStyle.Primitive => Plan.OperationId == PuzzleOperationIds.Slide && Plan.PrimitiveCount == 1
            ? LogicalDurationSeconds
            : Plan.PrimitiveCount * PrimitiveStepDurationSeconds,
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
            double stepDuration = DurationSeconds / Plan.PrimitiveCount;
            int index = Math.Min(Plan.PrimitiveCount - 1, (int)(elapsedSeconds / stepDuration));
            float progress = (float)((elapsedSeconds - index * stepDuration) / stepDuration);
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