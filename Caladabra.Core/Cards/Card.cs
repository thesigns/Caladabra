using Caladabra.Core.Effects;

namespace Caladabra.Core.Cards;

/// <summary>
/// Reprezentuje kartę w grze Caladabra.
/// Karta jest niemutowalna po utworzeniu (immutable).
/// </summary>
public sealed class Card
{
    // === Identyfikacja ===

    /// <summary>Unikalny identyfikator karty (np. "wyprawa_do_toalety").</summary>
    public required string Id { get; init; }

    /// <summary>Wyświetlana nazwa karty.</summary>
    public required string Name { get; init; }

    /// <summary>Tekst klimatyczny (flavor text) - kursywą na karcie.</summary>
    public string? FlavorText { get; init; }

    /// <summary>Instrukcja działania karty (czytelna dla gracza).</summary>
    public string Instruction { get; init; } = "";

    // === Statystyki ===

    /// <summary>Smak karty (archetyp).</summary>
    public required Flavor Flavor { get; init; }

    /// <summary>Koszt Siły Woli do zagrania karty.</summary>
    public required int WillpowerCost { get; init; }

    /// <summary>Kaloryczność - ile Tłuszczu dodaje zjedzenie karty.</summary>
    public required int Calories { get; init; }

    // === Efekty - triggery ===

    /// <summary>Efekt wykonywany po zagraniu karty.</summary>
    public IEffect? OnPlay { get; init; }

    /// <summary>Efekt wykonywany po zjedzeniu karty.</summary>
    public IEffect? OnEat { get; init; }

    /// <summary>Efekt wykonywany po dobraniu karty do ręki.</summary>
    public IEffect? OnDraw { get; init; }

    /// <summary>Efekt wykonywany po trafieniu karty do żołądka.</summary>
    public IEffect? OnEnterStomach { get; init; }

    /// <summary>Efekt wykonywany po opuszczeniu żołądka.</summary>
    public IEffect? OnLeaveStomach { get; init; }

    /// <summary>Efekt wykonywany po położeniu karty na stole.</summary>
    public IEffect? OnEnterTable { get; init; }

    /// <summary>Efekt wykonywany po usunięciu karty ze stołu.</summary>
    public IEffect? OnLeaveTable { get; init; }

    /// <summary>Efekt wykonywany po trafieniu karty do kibelka.</summary>
    public IEffect? OnDiscard { get; init; }

    /// <summary>Efekt wykonywany co turę gdy karta leży na stole.</summary>
    public IEffect? OnTurnOnTable { get; init; }

    /// <summary>Efekt wykonywany gdy licznik karty na stole osiągnie 0.</summary>
    public IEffect? OnTableCounterZero { get; init; }

    // === Stół ===

    /// <summary>
    /// Ile tur karta leży na stole (null = nie trafia na stół automatycznie).
    /// Używane przez efekt PlaceOnTable.
    /// </summary>
    public int? TableDuration { get; init; }

    // === Metody ===

    /// <summary>
    /// Tworzy kopię karty (dla Listy Kart Caladabra).
    /// Efekty są współdzielone (są niemutowalne).
    /// </summary>
    public Card Clone() => new()
    {
        Id = Id,
        Name = Name,
        FlavorText = FlavorText,
        Instruction = Instruction,
        Flavor = Flavor,
        WillpowerCost = WillpowerCost,
        Calories = Calories,
        OnPlay = OnPlay,
        OnEat = OnEat,
        OnDraw = OnDraw,
        OnEnterStomach = OnEnterStomach,
        OnLeaveStomach = OnLeaveStomach,
        OnEnterTable = OnEnterTable,
        OnLeaveTable = OnLeaveTable,
        OnDiscard = OnDiscard,
        OnTurnOnTable = OnTurnOnTable,
        OnTableCounterZero = OnTableCounterZero,
        TableDuration = TableDuration
    };

    public override string ToString() => $"{Name} ({Flavor.ToPolishName()}, SW:{WillpowerCost}, Kal:{Calories})";
}
