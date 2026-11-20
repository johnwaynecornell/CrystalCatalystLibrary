using System.Text.Json;

namespace SlideScramble;

/// <summary>A named collection of validated session recordings.</summary>
public sealed class ChallengeCatalog
{
    private readonly Dictionary<string, SessionRecording> challenges = new(StringComparer.Ordinal);

    public IReadOnlyCollection<string> Names => Array.AsReadOnly(challenges.Keys.ToArray());

    public bool TryGet(string name, out SessionRecording? recording) => challenges.TryGetValue(name, out recording);

    public static ChallengeCatalog LoadBundled() => Load(Path.Combine(AppContext.BaseDirectory, "challenges.json"));

    public static ChallengeCatalog Load(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        try { return FromJson(File.ReadAllText(path)); }
        catch (IOException) { return new ChallengeCatalog(); }
        catch (UnauthorizedAccessException) { return new ChallengeCatalog(); }
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
                if (entry.Value.ValueKind == JsonValueKind.Object && !entry.Value.TryGetProperty("moves", out _))
                    jsonEntry = jsonEntry.Insert(jsonEntry.Length - 1, entry.Value.EnumerateObject().Any() ? ",\"moves\":[]" : "\"moves\":[]");
                try { catalog.challenges.Add(name, SessionRecording.FromJson(jsonEntry)); }
                catch (JsonException) { }
            }
        }
        catch (JsonException) { }
        return catalog;
    }
}