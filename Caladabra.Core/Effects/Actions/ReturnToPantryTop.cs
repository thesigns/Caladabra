using Caladabra.Core.Events;
using Caladabra.Core.Zones;

namespace Caladabra.Core.Effects.Actions;

/// <summary>
/// Przenosi kartę źródłową (context.SourceCard) ze stołu na wierzch spiżarni.
/// Używane przez Diabelski bumerang.
/// </summary>
public sealed class ReturnToPantryTop : IEffect
{
    public static readonly ReturnToPantryTop Instance = new();

    public EffectResult Execute(EffectContext context)
    {
        if (context.SourceCard == null)
        {
            return EffectResult.Done();
        }

        var card = context.SourceCard;

        // Usuń ze stołu
        context.State.Table.Remove(card);

        // Połóż na wierzch spiżarni
        context.State.Pantry.AddToTop(card);
        context.Emit(new CardMovedEvent(card, ZoneType.Table, ZoneType.Pantry));

        return EffectResult.Done();
    }
}
