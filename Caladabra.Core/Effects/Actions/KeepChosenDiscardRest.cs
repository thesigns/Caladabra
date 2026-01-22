using Caladabra.Core.Events;
using Caladabra.Core.State;
using Caladabra.Core.Zones;

namespace Caladabra.Core.Effects.Actions;

/// <summary>
/// Zachowuje wybraną kartę w ręce, odrzuca pozostałe z opcji wyboru (Jasnowidzenie).
/// </summary>
public sealed class KeepChosenDiscardRest : IEffect
{
    public static readonly KeepChosenDiscardRest Instance = new();

    private KeepChosenDiscardRest() { }

    public EffectResult Execute(EffectContext context)
    {
        if (context.ChosenIndices == null || context.ChosenIndices.Length == 0)
            return EffectResult.Done();

        var chosenIndex = context.ChosenIndices[0];
        var pendingChoice = context.PendingChoice;

        if (pendingChoice == null)
            return EffectResult.Done();

        // Zbierz karty do odrzucenia (wszystkie z opcji oprócz wybranej)
        var cardsToDiscard = pendingChoice.Options
            .Where(o => o.Index != chosenIndex)
            .Select(o => o.Card)
            .ToList();

        // Odrzuć karty (szukamy po ID bo mogą być klonami)
        foreach (var cardToDiscard in cardsToDiscard)
        {
            var handCard = context.State.Hand.Cards.FirstOrDefault(c => c.Id == cardToDiscard.Id);
            if (handCard != null)
            {
                context.State.Hand.Remove(handCard);
                context.State.Toilet.Add(handCard);
                context.Emit(new CardDiscardedEvent(handCard, ZoneType.Hand));
            }
        }

        return EffectResult.Done();
    }
}
