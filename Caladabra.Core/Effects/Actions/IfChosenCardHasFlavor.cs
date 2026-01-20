using Caladabra.Core.Cards;

namespace Caladabra.Core.Effects.Actions;

/// <summary>
/// Wykonuje efekt jeśli wybrana karta (context.ChosenCard) ma określony smak.
/// </summary>
public sealed class IfChosenCardHasFlavor : IEffect
{
    private readonly Flavor _flavor;
    private readonly IEffect _ifTrue;
    private readonly IEffect? _ifFalse;

    public IfChosenCardHasFlavor(Flavor flavor, IEffect ifTrue, IEffect? ifFalse = null)
    {
        _flavor = flavor;
        _ifTrue = ifTrue;
        _ifFalse = ifFalse;
    }

    public EffectResult Execute(EffectContext context)
    {
        if (context.ChosenCard == null)
        {
            return EffectResult.Done();
        }

        if (context.ChosenCard.Flavor == _flavor)
        {
            return _ifTrue.Execute(context);
        }
        else if (_ifFalse != null)
        {
            return _ifFalse.Execute(context);
        }

        return EffectResult.Done();
    }
}
