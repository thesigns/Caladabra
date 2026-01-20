using Caladabra.Core.Events;
using Caladabra.Core.Zones;

namespace Caladabra.Core.Effects.Actions;

/// <summary>
/// Przenosi kartę źródłową (context.SourceCard) na wierzch spiżarni.
/// Używane przez Diabelski bumerang w OnTableCounterZero.
/// Uwaga: Karta jest już usunięta ze Stołu przez GameEngine.ProcessStartOfTurn()
/// przed wywołaniem OnTableCounterZero.
/// </summary>
public sealed class ReturnToPantryTop : IEffect
{
    public static readonly ReturnToPantryTop Instance = new();

    public EffectResult Execute(EffectContext context)
    {
        if (context.SourceCard == null)
            return EffectResult.Done();

        var card = context.SourceCard;

        // Połóż na wierzch spiżarni
        // (karta została już usunięta ze stołu przez GameEngine przed wywołaniem OnTableCounterZero)
        context.State.Pantry.AddToTop(card);
        context.Emit(new CardMovedEvent(card, ZoneType.Table, ZoneType.Pantry));

        return EffectResult.Done();
    }
}
