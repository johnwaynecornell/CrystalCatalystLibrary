using System.Text.Json;

namespace SlideScramble;

/// <summary>A named collection of validated session recordings.</summary>
public sealed class ChallengeCatalog
{
    private readonly Dictionary<string, SessionRecording> challenges = new(StringComparer.Ordinal);

    public IReadOnlyCollection<string> Names => Array.AsReadOnly(challenges.Keys.ToArray());

    public bool TryGet(string name, out SessionRecording? recording) => challenges.TryGetValue(name, out recording);

    public static ChallengeCatalog LoadBundled() => Load(Path.Combine(AppContext.BaseDirectory, "challenges.json"));

    private static string UserDirectory => Path.Combine(AppContext.BaseDirectory, "user-challenges");

    internal static ChallengeCatalog LoadWithUserChallenges()
    {
        var catalog = LoadBundled();
        if (!Directory.Exists(UserDirectory)) return catalog;

        foreach (string path in Directory.EnumerateFiles(UserDirectory, "*.json"))
        {
            try
            {
                if (new FileInfo(path).Length > 4_000_000) continue;
                var recording = SessionRecording.FromJson(File.ReadAllText(path));
                catalog.challenges.TryAdd(Path.GetFileNameWithoutExtension(path), recording);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (JsonException)
            {
            }
        }

        return catalog;
    }

    internal static string SaveEditedBoard(SessionRecording board)
    {
        Directory.CreateDirectory(UserDirectory);
        string name = $"Edited board {DateTime.Now:yyyy-MM-dd HHmmss} {Guid.NewGuid().ToString("N")[..6]}";
        string json = board.ToJson();
        string path = Path.Combine(UserDirectory, name + ".json");
        string temporaryPath = path + ".tmp";
        using (var stream = new FileStream(temporaryPath, FileMode.CreateNew))
        using (var writer = new StreamWriter(stream))
            writer.Write(json);
        File.Move(temporaryPath, path);
        return name;
    }

    public static ChallengeCatalog Load(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        try
        {
            return FromJson(File.ReadAllText(path));
        }
        catch (IOException)
        {
            return new ChallengeCatalog();
        }
        catch (UnauthorizedAccessException)
        {
            return new ChallengeCatalog();
        }
    }

    public static ChallengeCatalog FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        var catalog = new ChallengeCatalog();
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object) return catalog;
            foreach (var entry in document.RootElement.EnumerateObject())
            {
                var name = entry.Name;
                if (string.IsNullOrWhiteSpace(name) || catalog.challenges.ContainsKey(name)) continue;
                var jsonEntry = entry.Value.GetRawText();
                bool hasLegacyMoveList = entry.Value.ValueKind == JsonValueKind.Object &&
                                         entry.Value.TryGetProperty("version", out var version) &&
                                         version.ValueKind == JsonValueKind.Number &&
                                         version.TryGetInt32(out int versionNumber) && versionNumber is 1 or 2;
                if (hasLegacyMoveList && !entry.Value.TryGetProperty("moves", out _))
                    jsonEntry = jsonEntry.Insert(jsonEntry.Length - 1,
                        entry.Value.EnumerateObject().Any() ? ",\"moves\":[]" : "\"moves\":[]");
                try
                {
                    catalog.challenges.Add(name, SessionRecording.FromJson(jsonEntry));
                }
                catch (JsonException)
                {
                }
            }
        }
        catch (JsonException)
        {
        }

        return catalog;
    }
}