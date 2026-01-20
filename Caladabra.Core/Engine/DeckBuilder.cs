using Caladabra.Core.Cards;

namespace Caladabra.Core.Engine;

/// <summary>
/// Buduje talie kart dla różnych trybów gry.
/// </summary>
public static class DeckBuilder
{
    /// <summary>
    /// Buduje prototypową talię 60 kart (z GDD).
    /// </summary>
    public static List<Card> BuildPrototypeDeck()
    {
        var registry = CardRegistry.Instance;
        var deck = new List<Card>();

        // Skład z GDD:
        AddCopies(deck, registry, "wyprawa_do_toalety", 3);
        AddCopies(deck, registry, "wspinaczka_na_i_pietro", 3);
        AddCopies(deck, registry, "lowca_dwoch_smakow", 4);

        AddCopies(deck, registry, "lizak_na_oslode", 3);
        AddCopies(deck, registry, "baton_energetyczny", 3);
        AddCopies(deck, registry, "hat_trick", 4);

        AddCopies(deck, registry, "wilczy_glod", 3);
        AddCopies(deck, registry, "diabelski_bumerang", 3);
        AddCopies(deck, registry, "dostawa_jedzenia", 4);

        AddCopies(deck, registry, "lapczywe_jedzenie", 4);
        AddCopies(deck, registry, "grzebanie_w_kibelku", 3);
        AddCopies(deck, registry, "jasnowidzenie", 3);

        AddCopies(deck, registry, "sos_slodko_kwasny", 4);
        AddCopies(deck, registry, "skutki_dietetyczne", 3);
        AddCopies(deck, registry, "swiateczne_sniadanie", 3);

        AddCopies(deck, registry, "kwantowa_proznia", 3);
        AddCopies(deck, registry, "bylo_i_nie_ma", 3);
        AddCopies(deck, registry, "dieta_cud", 4);

        return deck; // 60 kart
    }

    /// <summary>
    /// Buduje prostą talię testową.
    /// </summary>
    public static List<Card> BuildTestDeck()
    {
        var registry = CardRegistry.Instance;
        var deck = new List<Card>();

        // Prosta talia do testów - głównie spalacze i motywatory
        AddCopies(deck, registry, "wyprawa_do_toalety", 4);
        AddCopies(deck, registry, "wspinaczka_na_i_pietro", 4);
        AddCopies(deck, registry, "lowca_dwoch_smakow", 4);

        AddCopies(deck, registry, "lizak_na_oslode", 4);
        AddCopies(deck, registry, "baton_energetyczny", 4);
        AddCopies(deck, registry, "hat_trick", 4);

        AddCopies(deck, registry, "wilczy_glod", 2);
        AddCopies(deck, registry, "diabelski_bumerang", 2);

        return deck; // 28 kart - łatwiejsza do testowania
    }

    private static void AddCopies(List<Card> deck, CardRegistry registry, string cardId, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var card = registry.CloneCard(cardId);
            deck.Add(card);
        }
    }
}
