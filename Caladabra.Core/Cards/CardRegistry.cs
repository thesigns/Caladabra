using Caladabra.Core.Zones;

namespace Caladabra.Core.Cards;

/// <summary>
/// Rejestr wszystkich kart w grze.
/// Singleton zarządzający definicjami kart.
/// </summary>
public sealed class CardRegistry
{
    private static CardRegistry? _instance;
    private readonly Dictionary<string, Card> _cards = new();
    private bool _initialized;

    public static CardRegistry Instance => _instance ??= new CardRegistry();

    private CardRegistry() { }

    /// <summary>
    /// Rejestruje kartę w rejestrze.
    /// </summary>
    public void Register(Card card)
    {
        if (_cards.ContainsKey(card.Id))
            throw new InvalidOperationException($"Karta '{card.Id}' jest już zarejestrowana.");

        _cards[card.Id] = card;
    }

    /// <summary>
    /// Pobiera kartę po ID.
    /// </summary>
    public Card? GetById(string id)
    {
        _cards.TryGetValue(id, out var card);
        return card;
    }

    /// <summary>
    /// Pobiera wszystkie zarejestrowane karty.
    /// </summary>
    public IReadOnlyCollection<Card> GetAll() => _cards.Values;

    /// <summary>
    /// Pobiera karty o określonym smaku.
    /// </summary>
    public IEnumerable<Card> GetByFlavor(Flavor flavor)
        => _cards.Values.Where(c => c.Flavor == flavor);

    /// <summary>
    /// Tworzy kopię karty po ID.
    /// </summary>
    public Card CloneCard(string id)
    {
        var card = GetById(id)
            ?? throw new InvalidOperationException($"Karta '{id}' nie istnieje.");
        return card.Clone();
    }

    /// <summary>
    /// Ładuje wszystkie karty do CardList w GameState.
    /// </summary>
    public void PopulateCardList(CardList cardList)
    {
        foreach (var card in _cards.Values)
        {
            cardList.Register(card.Clone());
        }
    }

    /// <summary>
    /// Inicjalizuje rejestr domyślnymi kartami.
    /// Wywoływane raz przy starcie aplikacji.
    /// </summary>
    public void Initialize()
    {
        if (_initialized)
            return;

        // Karty będą rejestrowane przez CardDefinitions
        _initialized = true;
    }

    /// <summary>
    /// Czyści rejestr (do testów).
    /// </summary>
    public void Clear()
    {
        _cards.Clear();
        _initialized = false;
    }
}
