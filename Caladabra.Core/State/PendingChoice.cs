using Caladabra.Core.Cards;
using Caladabra.Core.Effects;

namespace Caladabra.Core.State;

/// <summary>
/// Typ decyzji oczekiwanej od gracza.
/// </summary>
public enum ChoiceType
{
    /// <summary>Wybór karty z ręki.</summary>
    SelectFromHand,

    /// <summary>Wybór karty ze stołu.</summary>
    SelectFromTable,

    /// <summary>Wybór karty z żołądka.</summary>
    SelectFromStomach,

    /// <summary>Wybór karty z kibelka.</summary>
    SelectFromToilet,

    /// <summary>Wybór karty z Listy Kart Caladabra.</summary>
    SelectFromCardList,

    /// <summary>Wybór karty z Listy Kart Caladabra (tylko określony smak).</summary>
    SelectFromCardListFiltered
}

/// <summary>
/// Opcja wyboru dla gracza.
/// </summary>
public sealed class ChoiceOption
{
    /// <summary>Indeks opcji (do komendy choose).</summary>
    public required int Index { get; init; }

    /// <summary>Karta której dotyczy opcja.</summary>
    public required Card Card { get; init; }

    /// <summary>Tekst wyświetlany graczowi.</summary>
    public required string DisplayText { get; init; }
}

/// <summary>
/// Decyzja oczekująca na gracza.
/// </summary>
public sealed class PendingChoice
{
    /// <summary>Typ decyzji.</summary>
    public required ChoiceType Type { get; init; }

    /// <summary>Tekst pytania dla gracza.</summary>
    public required string Prompt { get; init; }

    /// <summary>Dostępne opcje.</summary>
    public required List<ChoiceOption> Options { get; init; }

    /// <summary>Minimalna liczba wyborów.</summary>
    public int MinChoices { get; init; } = 1;

    /// <summary>Maksymalna liczba wyborów.</summary>
    public int MaxChoices { get; init; } = 1;

    /// <summary>Filtr smaku (dla SelectFromCardListFiltered).</summary>
    public Flavor? FlavorFilter { get; init; }

    /// <summary>Efekt kontynuacji po wyborze.</summary>
    public required IEffect Continuation { get; init; }

    /// <summary>Karta źródłowa która wywołała ten wybór.</summary>
    public Card? SourceCard { get; init; }

    /// <summary>Który trigger wywołał efekt (np. "OnPlay", "OnEat").</summary>
    public string? EffectTrigger { get; set; }

    /// <summary>Karta która została zagrana (do dodania do Toilet po rozwiązaniu wyboru).</summary>
    public Card? PlayedCard { get; set; }
}
