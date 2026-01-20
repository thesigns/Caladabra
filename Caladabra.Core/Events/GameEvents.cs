using Caladabra.Core.Cards;
using Caladabra.Core.State;
using Caladabra.Core.Zones;

namespace Caladabra.Core.Events;

// === Zdarzenia kart ===

/// <summary>Karta została zagrana z ręki.</summary>
public sealed record CardPlayedEvent(Card Card, int HandIndex) : IGameEvent
{
    public GameEventType Type => GameEventType.CardPlayed;
}

/// <summary>Karta została zjedzona.</summary>
public sealed record CardEatenEvent(Card Card, int HandIndex) : IGameEvent
{
    public GameEventType Type => GameEventType.CardEaten;
}

/// <summary>Karta została dobrana do ręki.</summary>
public sealed record CardDrawnEvent(Card Card) : IGameEvent
{
    public GameEventType Type => GameEventType.CardDrawn;
}

/// <summary>Karta została przeniesiona między strefami.</summary>
public sealed record CardMovedEvent(Card Card, ZoneType FromZone, ZoneType ToZone) : IGameEvent
{
    public GameEventType Type => GameEventType.CardMoved;
}

/// <summary>Karta trafiła do kibelka.</summary>
public sealed record CardDiscardedEvent(Card Card, ZoneType FromZone) : IGameEvent
{
    public GameEventType Type => GameEventType.CardDiscarded;
}

// === Zdarzenia zasobów ===

/// <summary>Tłuszcz się zmienił.</summary>
public sealed record FatChangedEvent(int OldValue, int NewValue) : IGameEvent
{
    public GameEventType Type => GameEventType.FatChanged;
    public int Delta => NewValue - OldValue;
}

/// <summary>Siła Woli się zmieniła.</summary>
public sealed record WillpowerChangedEvent(int OldValue, int NewValue) : IGameEvent
{
    public GameEventType Type => GameEventType.WillpowerChanged;
    public int Delta => NewValue - OldValue;
}

// === Zdarzenia tury ===

/// <summary>Tura się rozpoczęła.</summary>
public sealed record TurnStartedEvent(int TurnNumber) : IGameEvent
{
    public GameEventType Type => GameEventType.TurnStarted;
}

/// <summary>Tura się zakończyła.</summary>
public sealed record TurnEndedEvent(int TurnNumber) : IGameEvent
{
    public GameEventType Type => GameEventType.TurnEnded;
}

/// <summary>Licznik karty na stole się zmniejszył.</summary>
public sealed record TableCounterTickedEvent(Card Card, int OldCounter, int NewCounter) : IGameEvent
{
    public GameEventType Type => GameEventType.TableCounterTicked;
}

// === Zdarzenia decyzji ===

/// <summary>Gra oczekuje decyzji gracza.</summary>
public sealed record ChoiceRequestedEvent(PendingChoice Choice) : IGameEvent
{
    public GameEventType Type => GameEventType.ChoiceRequested;
}

/// <summary>Gracz podjął decyzję.</summary>
public sealed record ChoiceMadeEvent(int[] ChosenIndices) : IGameEvent
{
    public GameEventType Type => GameEventType.ChoiceMade;
}

// === Zdarzenia gry ===

/// <summary>Gra się rozpoczęła.</summary>
public sealed record GameStartedEvent() : IGameEvent
{
    public GameEventType Type => GameEventType.GameStarted;
}

/// <summary>Gracz wygrał.</summary>
public sealed record GameWonEvent(int FinalTurn) : IGameEvent
{
    public GameEventType Type => GameEventType.GameWon;
}

/// <summary>Gracz przegrał.</summary>
public sealed record GameLostEvent(int FinalTurn, string Reason) : IGameEvent
{
    public GameEventType Type => GameEventType.GameLost;
}
