using System.Text.Json;
using System.Text.Json.Serialization;
using Caladabra.Core.State;

namespace Caladabra.Desktop.Integration;

/// <summary>
/// Logger sesji gry - zapisuje stan po każdej akcji do session.json.
/// Przydatne do debugowania i analizy bugów.
/// </summary>
public sealed class SessionLogger
{
    private const string SessionPath = "session.json";
    private readonly List<SessionEntry> _entries = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public void LogState(GameState state, string action)
    {
        var entry = new SessionEntry
        {
            Timestamp = DateTime.Now,
            Action = action,
            Seed = state.Seed,
            Turn = state.Turn,
            Phase = state.Phase.ToString(),
            Fat = state.Fat,
            Willpower = state.Willpower,
            HandCount = state.Hand.Count,
            Hand = state.Hand.Cards.Select(c => c.Name).ToList(),
            Table = state.Table.Entries.Select(e =>
                e.TurnsRemaining.HasValue
                    ? $"{e.Card.Name} ({e.TurnsRemaining})"
                    : e.Card.Name
            ).ToList(),
            Stomach = state.Stomach.Cards.Select(c => c.Name).ToList(),
            PantryCount = state.Pantry.Count,
            ToiletCount = state.Toilet.Count,
            ActiveModifiers = state.ActiveModifiers.Select(m => $"{m.Type}: {m.Value}").ToList(),
            PendingChoice = state.PendingChoice?.Prompt
        };

        _entries.Add(entry);
        Save();
    }

    public void Clear()
    {
        _entries.Clear();
        if (File.Exists(SessionPath))
            File.Delete(SessionPath);
    }

    private void Save()
    {
        var json = JsonSerializer.Serialize(_entries, JsonOptions);
        File.WriteAllText(SessionPath, json);
    }
}

public class SessionEntry
{
    public DateTime Timestamp { get; set; }
    public string Action { get; set; } = "";
    public int? Seed { get; set; }
    public int Turn { get; set; }
    public string Phase { get; set; } = "";
    public int Fat { get; set; }
    public int Willpower { get; set; }
    public int HandCount { get; set; }
    public List<string> Hand { get; set; } = new();
    public List<string> Table { get; set; } = new();
    public List<string> Stomach { get; set; } = new();
    public int PantryCount { get; set; }
    public int ToiletCount { get; set; }
    public List<string> ActiveModifiers { get; set; } = new();
    public string? PendingChoice { get; set; }
}
