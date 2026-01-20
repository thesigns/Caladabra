namespace Caladabra.Core.Events;

/// <summary>
/// Interfejs zdarzenia gry.
/// Zdarzenia są emitowane przez silnik gry i mogą być użyte do animacji/logowania.
/// </summary>
public interface IGameEvent
{
    /// <summary>Typ zdarzenia.</summary>
    GameEventType Type { get; }
}

/// <summary>
/// Typy zdarzeń gry.
/// </summary>
public enum GameEventType
{
    // === Karty ===
    CardPlayed,
    CardEaten,
    CardDrawn,
    CardMoved,
    CardDiscarded,

    // === Zasoby ===
    FatChanged,
    WillpowerChanged,

    // === Tura ===
    TurnStarted,
    TurnEnded,
    TableCounterTicked,

    // === Decyzje ===
    ChoiceRequested,
    ChoiceMade,

    // === Gra ===
    GameStarted,
    GameWon,
    GameLost
}
