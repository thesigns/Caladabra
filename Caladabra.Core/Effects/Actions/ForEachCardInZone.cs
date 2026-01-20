using Caladabra.Core.Cards;
using Caladabra.Core.Zones;

namespace Caladabra.Core.Effects.Actions;

/// <summary>
/// Wykonuje efekt za każdą kartę w strefie (opcjonalnie filtrując po smaku).
/// </summary>
public sealed class ForEachCardInZone : IEffect
{
    private readonly ZoneType[] _zones;
    private readonly Flavor? _flavorFilter;
    private readonly Func<int, IEffect> _effectFactory;

    public ForEachCardInZone(ZoneType[] zones, Flavor? flavorFilter, Func<int, IEffect> effectFactory)
    {
        _zones = zones;
        _flavorFilter = flavorFilter;
        _effectFactory = effectFactory;
    }

    /// <summary>
    /// Tworzy efekt który wykonuje akcję za każdą kartę (np. +2 SW za każdą gorzką).
    /// </summary>
    public static ForEachCardInZone Create(ZoneType[] zones, Flavor? flavorFilter, IEffect effectPerCard)
    {
        return new ForEachCardInZone(zones, flavorFilter, _ => effectPerCard);
    }

    public EffectResult Execute(EffectContext context)
    {
        int count = 0;

        foreach (var zoneType in _zones)
        {
            var zone = context.State.GetZone(zoneType);
            var cards = zone.Cards;

            if (_flavorFilter.HasValue)
            {
                cards = cards.Where(c => c.Flavor == _flavorFilter.Value).ToList();
            }

            count += cards.Count;
        }

        // Wykonaj efekt count razy
        for (int i = 0; i < count; i++)
        {
            var effect = _effectFactory(i);
            var result = effect.Execute(context);

            // Jeśli efekt wymaga decyzji, to komplikacja - na razie ignorujemy
            if (result is EffectResult.NeedsChoiceResult)
            {
                throw new InvalidOperationException("ForEachCardInZone nie obsługuje efektów wymagających decyzji.");
            }
        }

        return EffectResult.Done();
    }
}
