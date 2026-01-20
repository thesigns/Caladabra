using Caladabra.Core.Events;
using Caladabra.Core.State;

namespace Caladabra.Core.Effects.Actions;

/// <summary>
/// Dobiera kartę ze spiżarni do ręki.
/// </summary>
public sealed class DrawCard : IEffect
{
    private readonly int _count;

    public DrawCard(int count = 1)
    {
        _count = count;
    }

    public static readonly DrawCard Instance = new();

    public EffectResult Execute(EffectContext context)
    {
        // Pobierz modyfikatory (spójność z GameEngine.DrawCards)
        int caloriesReduction = context.State.ActiveModifiers
            .Where(m => m.Type == ModifierType.ReduceCaloriesOnDraw)
            .Sum(m => m.Value);

        for (int i = 0; i < _count; i++)
        {
            if (context.State.Hand.IsFull)
                break;

            var card = context.State.Pantry.Draw();
            if (card == null)
                break;

            // Zastosuj redukcję kalorii (Dieta cud)
            if (caloriesReduction > 0)
            {
                card.Calories = Math.Max(0, card.Calories - caloriesReduction);
            }

            context.State.Hand.Add(card);
            context.Emit(new CardDrawnEvent(card));

            // Wykonaj OnDraw karty (np. Kwantowa próżnia)
            if (card.OnDraw != null)
            {
                var drawContext = new EffectContext
                {
                    State = context.State,
                    SourceCard = card,
                    Events = context.Events
                };

                var result = card.OnDraw.Execute(drawContext);

                if (result is EffectResult.NeedsChoiceResult needsChoice)
                {
                    needsChoice.Choice.EffectTrigger = "OnDraw";
                    // Uwaga: jeśli dobieramy wiele kart i jedna wymaga wyboru,
                    // pozostałe karty nie zostaną dobrane (spójne z GameEngine)
                    return result;
                }
            }
        }

        return EffectResult.Done();
    }
}
