namespace Caladabra.Core.State;

/// <summary>
/// Typy modyfikatorów gry.
/// </summary>
public enum ModifierType
{
    /// <summary>
    /// Jasnowidzenie: dobierz +1 kartę, potem wybierz 1 do zachowania (reszta odrzucona).
    /// </summary>
    ExtraDrawThenDiscard,

    /// <summary>
    /// Dieta cud: dobrane karty mają zmniejszoną kaloryczność.
    /// </summary>
    ReduceCaloriesOnDraw
}
