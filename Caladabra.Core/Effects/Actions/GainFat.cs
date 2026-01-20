using Caladabra.Core.Events;

namespace Caladabra.Core.Effects.Actions;

/// <summary>
/// Zwiększa Tłuszcz o podaną wartość.
/// </summary>
public sealed class GainFat(int amount) : IEffect
{
    public int Amount { get; } = amount;

    public EffectResult Execute(EffectContext context)
    {
        int oldFat = context.State.Fat;
        context.State.ModifyFat(Amount);
        int newFat = context.State.Fat;

        if (oldFat != newFat)
        {
            context.Emit(new FatChangedEvent(oldFat, newFat));
        }

        return EffectResult.Done();
    }
}
