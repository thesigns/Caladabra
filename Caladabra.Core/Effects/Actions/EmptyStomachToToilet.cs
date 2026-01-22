using Caladabra.Core.Events;
using Caladabra.Core.Zones;

namespace Caladabra.Core.Effects.Actions;

/// <summary>
/// Opróżnia żołądek do kibelka.
/// </summary>
public sealed class EmptyStomachToToilet : IEffect
{
    public static readonly EmptyStomachToToilet Instance = new();

    private EmptyStomachToToilet() { }

    public EffectResult Execute(EffectContext context)
    {
        var cards = context.State.Stomach.Empty();

        foreach (var card in cards)
        {
            context.State.Toilet.Add(card);
            context.Emit(new CardMovedEvent(card, ZoneType.Stomach, ZoneType.Toilet));

            // Wykonaj OnDiscard
            if (card.OnDiscard != null)
            {
                var cardContext = new EffectContext
                {
                    State = context.State,
                    SourceCard = card,
                    Events = context.Events
                };
                card.OnDiscard.Execute(cardContext);
            }
        }

        return EffectResult.Done();
    }
}
