using Caladabra.Core.Events;
using Caladabra.Core.Zones;

namespace Caladabra.Core.Effects.Actions;

/// <summary>
/// Przenosi wybraną kartę (context.ChosenCard) z ręki do żołądka.
/// </summary>
public sealed class MoveChosenToStomach : IEffect
{
    public static readonly MoveChosenToStomach Instance = new();

    public EffectResult Execute(EffectContext context)
    {
        // Użyj indeksu zamiast referencji (po JSON restore referencje nie działają)
        if (context.ChosenIndices == null || context.ChosenIndices.Length == 0)
            return EffectResult.Done();

        var index = context.ChosenIndices[0];
        var card = context.State.Hand.GetAt(index);
        if (card == null)
            return EffectResult.Done();

        // Usuń z ręki
        context.State.Hand.RemoveAt(index);

        // Dodaj do żołądka (może wypchnąć najstarszą kartę)
        var expelled = context.State.Stomach.Add(card);
        context.Emit(new CardMovedEvent(card, ZoneType.Hand, ZoneType.Stomach));

        // Jeśli żołądek był pełny, karta wypadła do kibelka
        if (expelled != null)
        {
            context.State.Toilet.Add(expelled);
            context.Emit(new CardDiscardedEvent(expelled, ZoneType.Stomach));
        }

        return EffectResult.Done();
    }
}
