using Caladabra.Core.Cards;
using Caladabra.Core.State;
using Caladabra.Core.Zones;

namespace Caladabra.Core.Effects.Conditions;

/// <summary>
/// Sprawdza czy w podanej strefie są karty o wszystkich określonych smakach.
/// </summary>
public sealed class HasFlavorsInZone(ZoneType zone, params Flavor[] flavors) : ICondition
{
    public ZoneType Zone { get; } = zone;
    public Flavor[] Flavors { get; } = flavors;

    public bool Evaluate(GameState state)
    {
        var zoneCards = state.GetZone(Zone).Cards;
        var flavorsInZone = zoneCards.Select(c => c.Flavor).ToHashSet();

        return Flavors.All(f => flavorsInZone.Contains(f));
    }
}
