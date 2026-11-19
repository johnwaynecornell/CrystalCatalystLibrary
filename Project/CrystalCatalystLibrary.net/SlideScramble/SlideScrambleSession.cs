namespace SlideScramble;

/// <summary>A renderer-independent puzzle session. Cells use zero-based row-major solved IDs.</summary>
public sealed class SlideScrambleSession
{
    private readonly int[,] initialCells;
    private readonly int[,] cells;
    private double startTime;
    private double finalTime;

    public int Width => cells.GetLength(1);
    public int Height => cells.GetLength(0);
    public GameMode Mode { get; private set; } = GameMode.Ready;
    public int MoveCount { get; private set; }
    public double FinalTime => finalTime;
    public int[,] Cells => (int[,])cells.Clone();

    public SlideScrambleSession(int[,] initialCells)
    {
        ArgumentNullException.ThrowIfNull(initialCells);
        int height = initialCells.GetLength(0);
        int width = initialCells.GetLength(1);
        if (height == 0 || width == 0) throw new ArgumentException("Board dimensions must be positive.", nameof(initialCells));
        int count = checked(height * width);
        var seen = new bool[count];
        foreach (int value in initialCells)
        {
            if (value < 0 || value >= count || seen[value])
                throw new ArgumentException("Cells must be a permutation of zero-based row-major IDs.", nameof(initialCells));
            seen[value] = true;
        }
        this.initialCells = (int[,])initialCells.Clone();
        cells = (int[,])initialCells.Clone();
    }

    public bool IsSolved
    {
        get
        {
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    if (cells[y, x] != y * Width + x) return false;
            return true;
        }
    }

    public void StartScrambling()
    {
        Mode = GameMode.Scrambling;
        MoveCount = 0;
        startTime = finalTime = 0;
    }

    public void FinishScrambling(double currentTime)
    {
        ValidateTime(currentTime);
        if (Mode != GameMode.Scrambling) throw new InvalidOperationException("The session is not scrambling.");
        Mode = GameMode.InGame;
        MoveCount = 0;
        startTime = currentTime;
        finalTime = 0;
    }

    public bool MoveRow(int row, int delta, double currentTime) => Move(row, delta, currentTime, true);
    public bool MoveColumn(int column, int delta, double currentTime) => Move(column, delta, currentTime, false);

    private bool Move(int index, int delta, double currentTime, bool row)
    {
        ValidateTime(currentTime);
        int lines = row ? Height : Width;
        int length = row ? Width : Height;
        if (index < 0 || index >= lines) throw new ArgumentOutOfRangeException(nameof(index));
        if (delta % length == 0) return false;
        if (row) LogicalMoves.MoveRow(cells, index, delta);
        else LogicalMoves.MoveColumn(cells, index, delta);
        if (Mode == GameMode.Scrambling) return true;
        if (Mode == GameMode.Ready) startTime = currentTime;
        else if (Mode == GameMode.Solved) startTime = currentTime - finalTime;
        Mode = GameMode.InGame;
        finalTime = 0;
        MoveCount++;
        if (IsSolved)
        {
            finalTime = Math.Max(0, currentTime - startTime);
            Mode = GameMode.Solved;
        }
        return true;
    }

    public double GetElapsedTime(double currentTime)
    {
        ValidateTime(currentTime);
        return Mode switch
        {
            GameMode.InGame => Math.Max(0, currentTime - startTime),
            GameMode.Solved => finalTime,
            _ => 0
        };
    }

    public void Reset()
    {
        Array.Copy(initialCells, cells, initialCells.Length);
        Mode = GameMode.Ready;
        MoveCount = 0;
        startTime = finalTime = 0;
    }

    private static void ValidateTime(double time)
    {
        if (!double.IsFinite(time) || time < 0) throw new ArgumentOutOfRangeException(nameof(time));
    }
}