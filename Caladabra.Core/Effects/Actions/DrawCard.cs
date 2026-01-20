using Caladabra.Core.Events;

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
        for (int i = 0; i < _count; i++)
        {
            if (context.State.Hand.IsFull)
                break;

            var card = context.State.Pantry.Draw();
            if (card == null)
                break;

            context.State.Hand.Add(card);
            context.Emit(new CardDrawnEvent(card));

            // TODO: Wykonać OnDraw karty jeśli ma
            // Na razie pomijamy żeby uniknąć komplikacji
        }

        return EffectResult.Done();
    }
}
