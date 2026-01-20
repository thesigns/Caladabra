using Caladabra.Core.State;

namespace Caladabra.Core.Effects;

/// <summary>
/// Wynik wykonania efektu karty.
/// </summary>
public abstract record EffectResult
{
    /// <summary>Efekt zakończony pomyślnie.</summary>
    public static EffectResult Done() => DoneResult.Instance;

    /// <summary>Efekt wymaga decyzji gracza przed kontynuacją.</summary>
    public static EffectResult NeedsChoice(PendingChoice choice, IEffect continuation)
        => new NeedsChoiceResult(choice, continuation);

    /// <summary>Efekt zakończony - marker.</summary>
    public sealed record DoneResult : EffectResult
    {
        public static readonly DoneResult Instance = new();
        private DoneResult() { }
    }

    /// <summary>Efekt czeka na decyzję gracza.</summary>
    public sealed record NeedsChoiceResult(PendingChoice Choice, IEffect Continuation) : EffectResult;
}
