namespace SlideScramble;

public enum GameMode
{
    Ready,
    Scrambling,
    InGame,
    Solved
}

public class GameState
{
    public GameMode Mode { get; private set; } = GameMode.Ready;
    public int MoveCount { get; private set; } = 0;
    public double StartTime { get; private set; } = 0;
    public double FinalTime { get; private set; } = 0;

    public void StartScrambling()
    {
        Mode = GameMode.Scrambling;
        MoveCount = 0;
        StartTime = 0;
        FinalTime = 0;
    }

    public void FinishScrambling(double currentTime)
    {
        Mode = GameMode.InGame;
        MoveCount = 0;
        StartTime = currentTime;
        FinalTime = 0;
    }

    public void RecordMove(double currentTime = 0)
    {
        if (Mode == GameMode.Solved)
        {
            TakeBackSolved(currentTime);
            MoveCount++;
        }
        else if (Mode == GameMode.InGame)
        {
            MoveCount++;
        }
        else if (Mode == GameMode.Ready)
        {
            Mode = GameMode.InGame;
            StartTime = currentTime;
            FinalTime = 0;
            MoveCount = 1;
        }
    }

    public void TakeBackSolved(double currentTime = 0)
    {
        if (Mode == GameMode.Solved)
        {
            Mode = GameMode.InGame;
            if (FinalTime > 0 && currentTime > 0)
            {
                StartTime = currentTime - FinalTime;
            }
            else if (currentTime > 0)
            {
                StartTime = currentTime;
            }
            FinalTime = 0;
        }
    }

    public bool CheckSolved(Grid grid, double currentTime)
    {
        if (Mode == GameMode.InGame && grid.IsSolved())
        {
            Mode = GameMode.Solved;
            FinalTime = currentTime - StartTime;
            return true;
        }
        return false;
    }

    public void Reset()
    {
        Mode = GameMode.Ready;
        MoveCount = 0;
        StartTime = 0;
        FinalTime = 0;
    }

    public double GetElapsedTime(double currentTime)
    {
        return Mode switch
        {
            GameMode.InGame => Math.Max(0, currentTime - StartTime),
            GameMode.Solved => FinalTime,
            _ => 0
        };
    }

    public string FormatTime(double currentTime)
    {
        double elapsed = GetElapsedTime(currentTime);
        int minutes = (int)(elapsed / 60);
        double seconds = elapsed % 60;
        return $"{minutes:00}:{seconds:05.2}";
    }
}
