namespace SlideScramble;

public enum PortableLensInteractionPhase
{
    Inactive,
    Positioning,
    Accepted
}

public sealed record LensGestureRequest(int AnchorX, int AnchorY, PortableLensCell StartCell,
    PortableLensCell TargetCell, LensOperationFamily Family)
{
    public string OperationId => Family switch
    {
        LensOperationFamily.Shuffle => PuzzleOperationIds.Shuffle,
        LensOperationFamily.ColumnSwap => PuzzleOperationIds.ColumnSwap,
        LensOperationFamily.RowSwap => PuzzleOperationIds.RowSwap,
        _ => throw new ArgumentOutOfRangeException(nameof(Family))
    };

    public ShuffleOperationIntent? ShuffleIntent => Family == LensOperationFamily.Shuffle
        ? new ShuffleOperationIntent(AnchorX, AnchorY, StartCell, TargetCell)
        : null;
}

/// <summary>State machine for lens placement and completed directed-cell gestures.</summary>
public sealed class PortableLensInteractionController
{
    private int boardWidth;
    private int boardHeight;
    private PortableLensCell? gestureStart;
    private bool gestureControl;

    public PortableLensInteractionPhase Phase { get; private set; }
    public int AnchorX { get; private set; }
    public int AnchorY { get; private set; }
    public bool HasPendingGesture => gestureStart.HasValue;
    public (int X, int Y)? GestureStartPointer { get; private set; }
    public (int X, int Y)? GesturePointer { get; private set; }

    public bool BeginPlacement(int width, int height, int? initialX = null, int? initialY = null)
    {
        if (width < 3 || height < 3)
        {
            Cancel();
            return false;
        }
        boardWidth = width;
        boardHeight = height;
        AnchorX = initialX ?? 0;
        AnchorY = initialY ?? 0;
        if (AnchorX < 0 || AnchorX >= boardWidth || AnchorY < 0 || AnchorY >= boardHeight)
        {
            AnchorX = 0;
            AnchorY = 0;
        }

        gestureStart = null;
        Phase = PortableLensInteractionPhase.Positioning;
        return true;
    }

    public bool SetPlacementAnchor(int x, int y)
    {
        if (Phase != PortableLensInteractionPhase.Positioning || x < 0 || x >= boardWidth || y < 0 ||
            y >= boardHeight)
            return false;
        AnchorX = x;
        AnchorY = y;
        return true;
    }

    public bool AcceptPosition()
    {
        if (Phase != PortableLensInteractionPhase.Positioning) return false;
        Phase = PortableLensInteractionPhase.Accepted;
        return true;
    }

    public bool RepositionAndAccept(int x, int y)
    {
        if (Phase == PortableLensInteractionPhase.Inactive || x < 0 || x >= boardWidth || y < 0 || y >= boardHeight)
            return false;
        AnchorX = x;
        AnchorY = y;
        CancelGesture();
        Phase = PortableLensInteractionPhase.Accepted;
        return true;
    }

    public PortableLensCell? CellAt(int x, int y)
    {
        if (Phase != PortableLensInteractionPhase.Accepted || x < 0 || x >= boardWidth || y < 0 || y >= boardHeight)
            return null;
        int offsetX = (x - AnchorX + boardWidth) % boardWidth;
        int offsetY = (y - AnchorY + boardHeight) % boardHeight;
        return (offsetX, offsetY) switch
        {
            (0, 0) => PortableLensCell.Circle,
            (1, 0) => PortableLensCell.Star,
            (0, 1) => PortableLensCell.Triangle,
            (1, 1) => PortableLensCell.Square,
            _ => null
        };
    }

    public bool BeginGesture(PortableLensCell start, bool control, int pointerX = 0, int pointerY = 0)
    {
        if (Phase != PortableLensInteractionPhase.Accepted || gestureStart.HasValue || !Enum.IsDefined(start))
            return false;
        gestureStart = start;
        gestureControl = control;
        GestureStartPointer = (pointerX, pointerY);
        GesturePointer = (pointerX, pointerY);
        return true;
    }

    public void UpdateGesturePointer(int x, int y)
    {
        if (gestureStart.HasValue) GesturePointer = (x, y);
    }

    public LensGestureRequest? CompleteGesture(PortableLensCell target)
    {
        if (Phase != PortableLensInteractionPhase.Accepted || !gestureStart.HasValue) return null;
        PortableLensCell start = gestureStart.Value;
        bool control = gestureControl;
        CancelGesture();
        if (!LensGestureOperationMapper.TrySelectFamily(start, target, control, out var family)) return null;
        return new LensGestureRequest(AnchorX, AnchorY, start, target, family);
    }

    public void CancelGesture()
    {
        gestureStart = null;
        GestureStartPointer = null;
        GesturePointer = null;
    }

    public void Cancel()
    {
        gestureStart = null;
        Phase = PortableLensInteractionPhase.Inactive;
    }
}

/// <summary>Recognizes two nearby stationary board clicks using monotonic millisecond timestamps.</summary>
public sealed class PortableLensDoubleClickTracker
{
    private const long MaximumIntervalMilliseconds = 500;
    private const int MaximumMovement = 8;
    private (int X, int Y, long TimeMilliseconds)? previousClick;
    private (int X, int Y, long TimeMilliseconds)? pendingPress;

    public void BeginPress(int x, int y, long timeMilliseconds, bool eligible = true)
    {
        pendingPress = null;
        if (!eligible)
        {
            previousClick = null;
            return;
        }

        pendingPress = (x, y, timeMilliseconds);
    }

    public bool CompletePress(int x, int y, long timeMilliseconds)
    {
        if (!pendingPress.HasValue) return false;
        var press = pendingPress.Value;
        pendingPress = null;
        if (!AreNear(press.X, press.Y, x, y))
        {
            previousClick = null;
            return false;
        }

        if (previousClick is { } previous)
        {
            long interval = press.TimeMilliseconds - previous.TimeMilliseconds;
            if (interval >= 0 && interval <= MaximumIntervalMilliseconds &&
                AreNear(previous.X, previous.Y, press.X, press.Y))
            {
                previousClick = null;
                return true;
            }
        }

        previousClick = (press.X, press.Y, timeMilliseconds);
        return false;
    }

    public void Reset()
    {
        pendingPress = null;
        previousClick = null;
    }

    private static bool AreNear(int firstX, int firstY, int secondX, int secondY)
    {
        int deltaX = firstX - secondX;
        int deltaY = firstY - secondY;
        return deltaX * deltaX + deltaY * deltaY <= MaximumMovement * MaximumMovement;
    }
}