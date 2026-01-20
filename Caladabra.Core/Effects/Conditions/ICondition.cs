using Caladabra.Core.State;

namespace Caladabra.Core.Effects.Conditions;

/// <summary>
/// Warunek sprawdzany przed/w trakcie efektu.
/// </summary>
public interface ICondition
{
    /// <summary>
    /// Sprawdza czy warunek jest spełniony.
    /// </summary>
    bool Evaluate(GameState state);
}
