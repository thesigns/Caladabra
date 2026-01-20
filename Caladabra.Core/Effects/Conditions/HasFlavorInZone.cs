using Caladabra.Core.Cards;
using Caladabra.Core.State;
using Caladabra.Core.Zones;

namespace Caladabra.Core.Effects.Conditions;

/// <summary>
/// Sprawdza czy w podanej strefie jest karta o określonym smaku.
/// </summary>
public sealed class HasFlavorInZone(ZoneType zone, Flavor flavor) : ICondition
{
    public ZoneType Zone { get; } = zone;
    public Flavor Flavor { get; } = flavor;

    public bool Evaluate(GameState state)
    {
        var zoneCards = state.GetZone(Zone).Cards;
        return zoneCards.Any(c => c.Flavor == Flavor);
    }
}
