using Caladabra.Core.Events;
using Caladabra.Core.Zones;

namespace Caladabra.Core.Effects.Actions;

/// <summary>
/// Kładzie kartę źródłową na stole na określoną liczbę tur.
/// </summary>
public sealed class PlaceOnTable(int turns) : IEffect
{
    public int Turns { get; } = turns;

    public EffectResult Execute(EffectContext context)
    {
        var card = context.SourceCard;
        var removedCard = context.State.Table.Add(card, Turns);

        context.Emit(new CardMovedEvent(card, ZoneType.Hand, ZoneType.Table));

        // Jeśli stół był pełny, najstarsza karta trafia do kibelka
        if (removedCard != null)
        {
            context.State.Toilet.Add(removedCard);
            context.Emit(new CardDiscardedEvent(removedCard, ZoneType.Table));
        }

        return EffectResult.Done();
    }
}
