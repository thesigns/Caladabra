using Caladabra.Core.Cards;

namespace Caladabra.Core.Zones;

/// <summary>
/// Kibelek - stos kart odrzuconych.
/// Gracz może przeglądać, ale nie może wyciągać ani zmieniać kolejności.
/// </summary>
public sealed class Toilet : IZone
{
    private readonly List<Card> _cards = [];

    public ZoneType Type => ZoneType.Toilet;
    public int Count => _cards.Count;
    public IReadOnlyList<Card> Cards => _cards;

    /// <summary>
    /// Dodaje kartę do kibelka (na wierzch).
    /// </summary>
    public void Add(Card card)
    {
        _cards.Add(card);
    }

    /// <summary>
    /// Dodaje wiele kart do kibelka.
    /// </summary>
    public void AddRange(IEnumerable<Card> cards)
    {
        _cards.AddRange(cards);
    }

    /// <summary>
    /// Pobiera kartę z kibelka po indeksie (dla efektu "Grzebanie w kibelku").
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
    /// Usuwa konkretną kartę z kibelka.
    /// </summary>
    public bool Remove(Card card)
    {
        return _cards.Remove(card);
    }

    /// <summary>
    /// Wierzchnia karta (ostatnio dodana).
    /// </summary>
    public Card? TopCard => _cards.Count > 0 ? _cards[^1] : null;

    /// <summary>
    /// Czyści kibelek.
    /// </summary>
    public void Clear()
    {
        _cards.Clear();
    }
}
