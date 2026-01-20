using Caladabra.Core.State;

namespace Caladabra.Core.Effects.Actions;

/// <summary>
/// Dodaje modyfikator do gry (np. przy wejściu karty na stół).
/// </summary>
public sealed class AddModifier : IEffect
{
    private readonly ModifierType _type;
    private readonly int _value;

    public AddModifier(ModifierType type, int value = 1)
    {
        _type = type;
        _value = value;
    }

    public EffectResult Execute(EffectContext context)
    {
        context.State.ActiveModifiers.Add(new ActiveModifier
        {
            Type = _type,
            Value = _value,
            SourceCard = context.SourceCard
        });
        return EffectResult.Done();
    }
}
