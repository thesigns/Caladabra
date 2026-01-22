using Caladabra.Core.Cards;
using Caladabra.Core.Events;
using Caladabra.Core.State;

namespace Caladabra.Core.Effects;

/// <summary>
/// Kontekst wykonania efektu karty.
/// Zawiera wszystko czego efekt potrzebuje do działania.
/// </summary>
public sealed class EffectContext
{
    /// <summary>Aktualny stan gry.</summary>
    public required GameState State { get; init; }

    /// <summary>Karta która wywołała efekt.</summary>
    public required Card SourceCard { get; init; }

    /// <summary>Lista zdarzeń do emisji (efekt dodaje swoje zdarzenia).</summary>
    public required List<IGameEvent> Events { get; init; }

    /// <summary>
    /// Wynik poprzedniego wyboru gracza (jeśli efekt kontynuuje po decyzji).
    /// Null jeśli to pierwsze wykonanie efektu.
    /// </summary>
    public int[]? ChosenIndices { get; init; }

    /// <summary>
    /// Karta wybrana przez gracza (ustawiana po ChooseCardFrom).
    /// </summary>
    public Card? ChosenCard { get; set; }

    /// <summary>
    /// PendingChoice z którego pochodzi wybór (dla efektów typu KeepChosenDiscardRest).
    /// </summary>
    public PendingChoice? PendingChoice { get; init; }

    /// <summary>
    /// Flaga sygnalizująca pominięcie dobierania karty na końcu tury.
    /// Ustawiana przez SkipDraw.
    /// </summary>
    public bool ShouldSkipDraw { get; set; }

    /// <summary>
    /// Emituje zdarzenie gry.
    /// </summary>
    public void Emit(IGameEvent gameEvent)
    {
        Events.Add(gameEvent);
    }
}
