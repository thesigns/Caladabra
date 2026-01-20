using Caladabra.Core.Events;
using Caladabra.Core.Zones;

namespace Caladabra.Core.Effects.Actions;

/// <summary>
/// Odrzuca wybraną kartę (context.ChosenCard) do kibelka.
/// </summary>
public sealed class DiscardChosenCard(ZoneType fromZone) : IEffect
{
    public ZoneType FromZone { get; } = fromZone;

    public EffectResult Execute(EffectContext context)
    {
        if (context.ChosenCard == null)
        {
            return EffectResult.Done();
        }

        var card = context.ChosenCard;

        // Usuń kartę ze źródłowej strefy
        switch (FromZone)
        {
            case ZoneType.Table:
                context.State.Table.Remove(card);
                break;
            case ZoneType.Hand:
                // Znajdź indeks karty w ręce
                var handIndex = context.State.Hand.Cards.ToList().IndexOf(card);
                if (handIndex >= 0)
                    context.State.Hand.RemoveAt(handIndex);
                break;
            case ZoneType.Stomach:
                // Żołądek nie ma Remove - to jest kolejka
                break;
        }

        // Dodaj do kibelka
        context.State.Toilet.Add(card);
        context.Emit(new CardDiscardedEvent(card, FromZone));

        return EffectResult.Done();
    }
}
