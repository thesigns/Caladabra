using Caladabra.Core.State;
using Caladabra.Core.Zones;

namespace Caladabra.Core.Effects.Conditions;

/// <summary>
/// Sprawdza czy w podanej strefie jest co najmniej N unikalnych smaków.
/// </summary>
public sealed class CountUniqueFlavorsInZone(ZoneType zone, int minCount) : ICondition
{
    public ZoneType Zone { get; } = zone;
    public int MinCount { get; } = minCount;

    public bool Evaluate(GameState state)
    {
        var zoneCards = state.GetZone(Zone).Cards;
        var uniqueFlavors = zoneCards.Select(c => c.Flavor).Distinct().Count();

        return uniqueFlavors >= MinCount;
    }
}
