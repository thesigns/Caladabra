using Caladabra.Core.Cards;

namespace Caladabra.Core.Zones;

/// <summary>
/// Spiżarnia - talia kart używana w aktualnej rozgrywce.
/// Karty są odwrócone rewersem do góry - gracz widzi tylko smak wierzchniej karty.
/// </summary>
public sealed class Pantry : IZone
{
    private readonly List<Card> _cards = [];

    public ZoneType Type => ZoneType.Pantry;
    public int Count => _cards.Count;
    public IReadOnlyList<Card> Cards => _cards;

    /// <summary>
    /// Smak wierzchniej karty (widoczny dla gracza).
    /// </summary>
    public Flavor? TopCardFlavor => _cards.Count > 0 ? _cards[^1].Flavor : null;

    /// <summary>
    /// Dodaje karty do talii (na spód).
    /// </summary>
    public void AddToBottom(IEnumerable<Card> cards)
    {
        _cards.InsertRange(0, cards);
    }

    /// <summary>
    /// Dodaje kartę na wierzch talii.
    /// </summary>
    public void AddToTop(Card card)
    {
        _cards.Add(card);
    }

    /// <summary>
    /// Dobiera kartę z wierzchu talii.
    /// </summary>
    public Card? Draw()
    {
        if (_cards.Count == 0)
            return null;

        var card = _cards[^1];
        _cards.RemoveAt(_cards.Count - 1);
        return card;
    }

    /// <summary>
    /// Tasuje talię.
    /// </summary>
    public void Shuffle(Random? random = null)
    {
        random ??= Random.Shared;

        // Fisher-Yates shuffle
        for (int i = _cards.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
        }
    }

    /// <summary>
    /// Czyści talię.
    /// </summary>
    public void Clear()
    {
        _cards.Clear();
    }
}
