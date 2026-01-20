using Caladabra.Core.Events;

namespace Caladabra.Core.Effects.Actions;

/// <summary>
/// Zmniejsza Siłę Woli o podaną wartość.
/// </summary>
public sealed class LoseWillpower(int amount) : IEffect
{
    public int Amount { get; } = amount;

    public EffectResult Execute(EffectContext context)
    {
        int oldWP = context.State.Willpower;
        context.State.ModifyWillpower(-Amount);
        int newWP = context.State.Willpower;

        if (oldWP != newWP)
        {
            context.Emit(new WillpowerChangedEvent(oldWP, newWP));
        }

        return EffectResult.Done();
    }
}
