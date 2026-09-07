namespace ClipFlow.Smoke;

public class SmokeFixture : IDisposable
{
    public string TempDirectory { get; }
    public bool KeepTemp { get; set; }

    public SmokeFixture(bool keepTemp = false)
    {
        KeepTemp = keepTemp;
        TempDirectory = Path.Combine(Path.GetTempPath(), "clipflow_smoke_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(TempDirectory);
    }

    public string GetPath(string relativePath)
    {
        return Path.Combine(TempDirectory, relativePath);
    }

    public string CreateTextFile(string relativePath, string content)
    {
        string fullPath = GetPath(relativePath);
        string? dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllText(fullPath, content);
        return fullPath;
    }

    public void Dispose()
    {
        if (!KeepTemp && Directory.Exists(TempDirectory))
        {
            try
            {
                Directory.Delete(TempDirectory, recursive: true);
            }
            catch
            {
                // Best effort cleanup
            }
        }
    }
}
