namespace SlideScramble;

internal static class LogicalMoves
{
    public static void MoveRow<T>(T[,] cells, int row, int delta)
    {
        int width = cells.GetLength(1);
        var work = new T[width];
        for (int x = 0; x < width; x++) work[x] = cells[row, x];
        int shift = Wrap(delta, width);
        for (int x = 0; x < width; x++) cells[row, x] = work[(int)(((long)x + shift) % width)];
    }

    public static void MoveColumn<T>(T[,] cells, int column, int delta)
    {
        int height = cells.GetLength(0);
        var work = new T[height];
        for (int y = 0; y < height; y++) work[y] = cells[y, column];
        int shift = Wrap(delta, height);
        for (int y = 0; y < height; y++) cells[y, column] = work[(int)(((long)y + shift) % height)];
    }

    private static int Wrap(int value, int length) => (int)(((long)value % length + length) % length);
}