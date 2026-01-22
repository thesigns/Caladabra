namespace Caladabra.Core.Effects.Actions;

/// <summary>
/// Wykonuje sekwencję efektów po kolei.
/// Jeśli którykolwiek wymaga decyzji, przerywa i czeka.
/// </summary>
public sealed class Sequence : IEffect
{
    private readonly IEffect[] _effects;
    private int _currentIndex;

    public Sequence(params IEffect[] effects)
    {
        _effects = effects;
        _currentIndex = 0;
    }

    public EffectResult Execute(EffectContext context)
    {
        // Resetuj indeks dla świeżych wywołań
        // Kontynuacja po wyborze ma PendingChoice z tym Sequence jako Continuation
        bool isContinuation = context.PendingChoice?.Continuation == this;
        if (!isContinuation)
        {
            _currentIndex = 0;
        }

        while (_currentIndex < _effects.Length)
        {
            var result = _effects[_currentIndex].Execute(context);

            if (result is EffectResult.NeedsChoiceResult needsChoice)
            {
                // Nie przesuwaj indeksu - ten efekt musi być wykonany ponownie z ChosenIndices
                return EffectResult.NeedsChoice(needsChoice.Choice, this);
            }

            _currentIndex++;
        }

        return EffectResult.Done();
    }
}
