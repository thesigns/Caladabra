namespace Caladabra.Core.State;

/// <summary>
/// Faza gry - określa jaki input oczekuje gra od gracza.
/// </summary>
public enum GamePhase
{
    /// <summary>Czeka na akcję gracza (play/eat).</summary>
    AwaitingAction,

    /// <summary>Czeka na decyzję gracza (choose) - np. wybór karty ze stołu.</summary>
    AwaitingChoice,

    /// <summary>Gra wygrana - Tłuszcz = 0.</summary>
    Won,

    /// <summary>Gra przegrana - brak kart lub niemożliwy ruch.</summary>
    Lost
}
