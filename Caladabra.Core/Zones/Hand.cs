using Caladabra.Core.Cards;
using Caladabra.Core.Engine;

namespace Caladabra.Core.Zones;

/// <summary>
/// Ręka gracza - karty które można zagrywać lub zjadać.
/// Pełni też funkcję paska życia - pusta ręka bez możliwości dobrania = przegrana.
/// </summary>
public sealed class Hand : IZone
{
    private readonly List<Card> _cards = [];

    public ZoneType Type => ZoneType.Hand;
    public int Count => _cards.Count;
    public IReadOnlyList<Card> Cards => _cards;

    /// <summary>
    /// Czy ręka jest pełna.
    /// </summary>
    public bool IsFull => _cards.Count >= GameRules.MaxHandSize;

    /// <summary>
    /// Dodaje kartę do ręki (na prawą stronę - najnowsza).
    /// </summary>
    /// <returns>True jeśli karta została dodana, false jeśli ręka pełna.</returns>
    public bool Add(Card card)
    {
        if (IsFull)
            return false;

        _cards.Add(card);
        return true;
    }

    /// <summary>
    /// Pobiera kartę z ręki (usuwa ją).
    /// </summary>
    public Card? RemoveAt(int index)
    {
        if (index < 0 || index >= _cards.Count)
            return null;

        var card = _cards[index];
        _cards.RemoveAt(index);
        return card;
    }

    /// <summary>
    /// Pobiera kartę z ręki po indeksie (bez usuwania).
    /// </summary>
    public Card? GetAt(int index)
    {
        if (index < 0 || index >= _cards.Count)
            return null;

        return _cards[index];
    }

    /// <summary>
    /// Czyści rękę.
    /// </summary>
    public void Clear()
    {
        _cards.Clear();
    }
}
