using Caladabra.Core.State;

namespace Caladabra.Core.Effects;

/// <summary>
/// Interfejs efektu karty.
/// Efekty są niemutowalne i reużywalne między kartami.
/// </summary>
public interface IEffect
{
    /// <summary>
    /// Wykonuje efekt na stanie gry.
    /// </summary>
    /// <param name="context">Kontekst wykonania efektu.</param>
    /// <returns>Wynik wykonania efektu.</returns>
    EffectResult Execute(EffectContext context);
}
