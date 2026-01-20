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
        while (_currentIndex < _effects.Length)
        {
            var result = _effects[_currentIndex].Execute(context);

            if (result is EffectResult.NeedsChoiceResult needsChoice)
            {
                // Zapisz kontynuację - ten sam Sequence z przesuniętym indeksem
                _currentIndex++;
                return EffectResult.NeedsChoice(needsChoice.Choice, this);
            }

            _currentIndex++;
        }

        return EffectResult.Done();
    }
}
