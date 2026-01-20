using Caladabra.Core.Events;
using Caladabra.Core.Zones;

namespace Caladabra.Core.Effects.Actions;

/// <summary>
/// Odrzuca wybraną kartę z ręki do kibelka (Jasnowidzenie).
/// </summary>
public sealed class DiscardChosenFromHand : IEffect
{
    public static readonly DiscardChosenFromHand Instance = new();

    private DiscardChosenFromHand() { }

    public EffectResult Execute(EffectContext context)
    {
        if (context.ChosenCard == null)
            return EffectResult.Done();

        // Usuń z ręki
        context.State.Hand.Remove(context.ChosenCard);

        // Dodaj do kibelka
        context.State.Toilet.Add(context.ChosenCard);
        context.Emit(new CardDiscardedEvent(context.ChosenCard, ZoneType.Hand));

        return EffectResult.Done();
    }
}
