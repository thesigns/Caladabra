using Caladabra.Core.Cards;
using Caladabra.Core.Engine;

namespace Caladabra.Core.Zones;

/// <summary>
/// Żołądek - kolejka FIFO zjedzonych kart.
/// Maksymalnie 4 karty. Nowa karta trafia na górę, najstarsza wypada na dół do Kibelka.
/// </summary>
public sealed class Stomach : IZone
{
    private readonly List<Card> _cards = [];

    public ZoneType Type => ZoneType.Stomach;
    public int Count => _cards.Count;
    public IReadOnlyList<Card> Cards => _cards;

    /// <summary>
    /// Czy żołądek jest pełny.
    /// </summary>
    public bool IsFull => _cards.Count >= GameRules.MaxStomachSize;

    /// <summary>
    /// Dodaje kartę do żołądka (na górę).
    /// Jeśli żołądek pełny, zwraca najstarszą kartę (z dołu).
    /// </summary>
    /// <returns>Karta która wypadła z żołądka (jeśli był pełny) lub null.</returns>
    public Card? Add(Card card)
    {
        Card? expelled = null;

        // Jeśli żołądek pełny, wypchnij najstarszą kartę (index 0)
        if (IsFull)
        {
            expelled = _cards[0];
            _cards.RemoveAt(0);
        }

        // Nowa karta trafia na górę (koniec listy)
        _cards.Add(card);

        return expelled;
    }

    /// <summary>
    /// Opróżnia żołądek - zwraca wszystkie karty.
    /// </summary>
    public List<Card> Empty()
    {
        var cards = _cards.ToList();
        _cards.Clear();
        return cards;
    }

    /// <summary>
    /// Sprawdza czy w żołądku są karty o podanym smaku.
    /// </summary>
    public bool HasFlavor(Flavor flavor)
        => _cards.Any(c => c.Flavor == flavor);

    /// <summary>
    /// Liczy unikalne smaki w żołądku.
    /// </summary>
    public int CountUniqueFlavors()
        => _cards.Select(c => c.Flavor).Distinct().Count();

    /// <summary>
    /// Zwraca karty o podanym smaku.
    /// </summary>
    public IReadOnlyList<Card> GetByFlavor(Flavor flavor)
        => _cards.Where(c => c.Flavor == flavor).ToList();

    /// <summary>
    /// Czyści żołądek.
    /// </summary>
    public void Clear()
    {
        _cards.Clear();
    }
}
