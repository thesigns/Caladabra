using Caladabra.Core.Events;

namespace Caladabra.Core.Effects.Actions;

/// <summary>
/// Zwiększa Siłę Woli o podaną wartość.
/// </summary>
public sealed class GainWillpower(int amount) : IEffect
{
    public int Amount { get; } = amount;

    public EffectResult Execute(EffectContext context)
    {
        int oldWP = context.State.Willpower;
        context.State.ModifyWillpower(Amount);
        int newWP = context.State.Willpower;

        if (oldWP != newWP)
        {
            context.Emit(new WillpowerChangedEvent(oldWP, newWP));
        }

        return EffectResult.Done();
    }
}
