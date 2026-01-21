using Caladabra.Core.Zones;

namespace Caladabra.Core.Effects.Actions;

/// <summary>
/// Wykonuje efekt za każdą kartę o podanym ID w strefie.
/// Używane np. przez Maraton do liczenia kopii w Kibelku.
/// </summary>
public sealed class ForEachCardByIdInZone : IEffect
{
    private readonly ZoneType[] _zones;
    private readonly string _cardId;
    private readonly IEffect _effect;

    public ForEachCardByIdInZone(ZoneType[] zones, string cardId, IEffect effect)
    {
        _zones = zones;
        _cardId = cardId;
        _effect = effect;
    }

    public EffectResult Execute(EffectContext context)
    {
        int count = 0;

        foreach (var zoneType in _zones)
        {
            var zone = context.State.GetZone(zoneType);
            count += zone.Cards.Count(c => c.Id == _cardId);
        }

        // Wykonaj efekt count razy
        for (int i = 0; i < count; i++)
        {
            var result = _effect.Execute(context);

            if (result is EffectResult.NeedsChoiceResult)
            {
                throw new InvalidOperationException("ForEachCardByIdInZone nie obsługuje efektów wymagających decyzji.");
            }
        }

        return EffectResult.Done();
    }
}
