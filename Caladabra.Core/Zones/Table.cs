using Caladabra.Core.Cards;
using Caladabra.Core.Engine;

namespace Caladabra.Core.Zones;

/// <summary>
/// Wpis karty na stole - karta + jej licznik tur.
/// </summary>
public sealed class TableEntry
{
    /// <summary>Karta na stole.</summary>
    public required Card Card { get; init; }

    /// <summary>Pozostała liczba tur (null = permanentnie).</summary>
    public int? TurnsRemaining { get; set; }

    /// <summary>
    /// Czy karta została "transformowana" (np. przez "Było i nie ma").
    /// Gdy true, oryginalne efekty karty (OnTurnOnTable, OnTableCounterZero, OnLeaveTable) są ignorowane.
    /// </summary>
    public bool IsTransformed { get; set; }

    /// <summary>
    /// Nadpisana instrukcja karty (wyświetlana zamiast oryginalnej gdy IsTransformed = true).
    /// </summary>
    public string? OverriddenInstruction { get; set; }
}

/// <summary>
/// Stół - karty z efektami trwałymi, czasowymi lub odroczonymi.
/// Maksymalnie 3 karty.
/// </summary>
public sealed class Table : IZone
{
    private readonly List<TableEntry> _entries = [];

    public ZoneType Type => ZoneType.Table;
    public int Count => _entries.Count;
    public IReadOnlyList<Card> Cards => _entries.Select(e => e.Card).ToList();

    /// <summary>
    /// Wszystkie wpisy na stole.
    /// </summary>
    public IReadOnlyList<TableEntry> Entries => _entries;

    /// <summary>
    /// Czy stół jest pełny.
    /// </summary>
    public bool IsFull => _entries.Count >= GameRules.MaxTableSize;

    /// <summary>
    /// Dodaje kartę na stół.
    /// Jeśli stół jest pełny, zwraca najstarszą kartę (do usunięcia).
    /// </summary>
    /// <returns>Karta usunięta ze stołu (jeśli był pełny) lub null.</returns>
    public Card? Add(Card card, int? turnsRemaining)
    {
        Card? removed = null;

        // Jeśli stół pełny, usuń najstarszą kartę
        if (IsFull)
        {
            removed = _entries[0].Card;
            _entries.RemoveAt(0);
        }

        _entries.Add(new TableEntry
        {
            Card = card,
            TurnsRemaining = turnsRemaining
        });

        return removed;
    }

    /// <summary>
    /// Usuwa kartę ze stołu po indeksie.
    /// </summary>
    public Card? RemoveAt(int index)
    {
        if (index < 0 || index >= _entries.Count)
            return null;

        var card = _entries[index].Card;
        _entries.RemoveAt(index);
        return card;
    }

    /// <summary>
    /// Usuwa konkretną kartę ze stołu.
    /// </summary>
    public bool Remove(Card card)
    {
        var entry = _entries.FirstOrDefault(e => e.Card == card);
        if (entry == null)
            return false;

        _entries.Remove(entry);
        return true;
    }

    /// <summary>
    /// Pobiera wpis na stole po indeksie.
    /// </summary>
    public TableEntry? GetEntryAt(int index)
    {
        if (index < 0 || index >= _entries.Count)
            return null;

        return _entries[index];
    }

    /// <summary>
    /// Zmniejsza liczniki wszystkich kart o 1.
    /// Zwraca karty których licznik osiągnął 0.
    /// </summary>
    public List<Card> TickCounters()
    {
        var expired = new List<Card>();

        foreach (var entry in _entries)
        {
            if (entry.TurnsRemaining.HasValue)
            {
                // TurnsRemaining=0 oznacza "już wygasła, usuń przy tym tick"
                if (entry.TurnsRemaining.Value == 0)
                {
                    expired.Add(entry.Card);
                }
                // Dekrementuj gdy > 0 (pomijaj permanentne karty z -1)
                else if (entry.TurnsRemaining.Value > 0)
                {
                    entry.TurnsRemaining--;
                    if (entry.TurnsRemaining == 0)
                    {
                        expired.Add(entry.Card);
                    }
                }
            }
        }

        return expired;
    }

    /// <summary>
    /// Czyści stół.
    /// </summary>
    public void Clear()
    {
        _entries.Clear();
    }
}
