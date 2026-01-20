using Caladabra.Core.Effects.Conditions;

namespace Caladabra.Core.Effects.Actions;

/// <summary>
/// Wykonuje jeden z dwóch efektów w zależności od warunku.
/// </summary>
public sealed class Conditional(ICondition condition, IEffect ifTrue, IEffect? ifFalse = null) : IEffect
{
    public ICondition Condition { get; } = condition;
    public IEffect IfTrue { get; } = ifTrue;
    public IEffect? IfFalse { get; } = ifFalse;

    public EffectResult Execute(EffectContext context)
    {
        if (Condition.Evaluate(context.State))
        {
            return IfTrue.Execute(context);
        }
        else if (IfFalse != null)
        {
            return IfFalse.Execute(context);
        }

        return EffectResult.Done();
    }
}
