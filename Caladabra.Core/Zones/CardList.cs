using Caladabra.Core.Cards;

namespace Caladabra.Core.Zones;

/// <summary>
/// Lista Kart Caladabra - metastrefa zawierająca po jednym egzemplarzu każdej karty.
/// Źródło dla efektów typu "wybierz dowolną kartę z Listy Kart Caladabra".
/// </summary>
public sealed class CardList : IZone
{
    private readonly List<Card> _cards = [];

    public ZoneType Type => ZoneType.CardList;
    public int Count => _cards.Count;
    public IReadOnlyList<Card> Cards => _cards;

    /// <summary>
    /// Rejestruje kartę w liście (wywoływane przez CardRegistry).
    /// </summary>
    internal void Register(Card card)
    {
        if (_cards.Any(c => c.Id == card.Id))
            throw new InvalidOperationException($"Karta '{card.Id}' jest już zarejestrowana.");

        _cards.Add(card);
    }

    /// <summary>
    /// Zwraca karty określonego smaku.
    /// </summary>
    public IReadOnlyList<Card> GetByFlavor(Flavor flavor)
        => _cards.Where(c => c.Flavor == flavor).ToList();

    /// <summary>
    /// Znajduje kartę po ID.
    /// </summary>
    public Card? FindById(string id)
        => _cards.FirstOrDefault(c => c.Id == id);

    /// <summary>
    /// Tworzy kopię karty o podanym ID.
    /// </summary>
    public Card CloneCard(string id)
    {
        var card = FindById(id)
            ?? throw new InvalidOperationException($"Karta '{id}' nie istnieje w Liście Kart.");
        return card.Clone();
    }
}
