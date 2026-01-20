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
        // Użyj indeksu żeby znaleźć faktyczną kartę w ręce
        // (ChosenCard może być klonem z JSON, nie referencją do karty w Hand)
        if (context.ChosenIndices == null || context.ChosenIndices.Length == 0)
            return EffectResult.Done();

        var index = context.ChosenIndices[0];
        var card = context.State.Hand.GetAt(index);
        if (card == null)
            return EffectResult.Done();

        // Usuń z ręki
        context.State.Hand.RemoveAt(index);

        // Dodaj do kibelka
        context.State.Toilet.Add(card);
        context.Emit(new CardDiscardedEvent(card, ZoneType.Hand));

        return EffectResult.Done();
    }
}
