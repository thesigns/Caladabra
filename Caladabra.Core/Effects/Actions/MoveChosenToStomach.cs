using Caladabra.Core.Events;

namespace Caladabra.Core.Effects.Actions;

/// <summary>
/// Przenosi wybraną kartę (context.ChosenCard) z ręki do żołądka.
/// </summary>
public sealed class MoveChosenToStomach : IEffect
{
    public static readonly MoveChosenToStomach Instance = new();

    public EffectResult Execute(EffectContext context)
    {
        if (context.ChosenCard == null)
        {
            return EffectResult.Done();
        }

        var card = context.ChosenCard;

        // Usuń z ręki
        var handIndex = context.State.Hand.Cards.ToList().IndexOf(card);
        if (handIndex >= 0)
        {
            context.State.Hand.RemoveAt(handIndex);
        }

        // Dodaj do żołądka
        context.State.Stomach.Add(card);
        context.Emit(new CardEatenEvent(card, card.Calories));

        return EffectResult.Done();
    }
}
