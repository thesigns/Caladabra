using Caladabra.Core.Cards;

namespace Caladabra.Core.Zones;

/// <summary>
/// Interfejs strefy kart.
/// </summary>
public interface IZone
{
    /// <summary>Typ strefy.</summary>
    ZoneType Type { get; }

    /// <summary>Liczba kart w strefie.</summary>
    int Count { get; }

    /// <summary>Czy strefa jest pusta.</summary>
    bool IsEmpty => Count == 0;

    /// <summary>Wszystkie karty w strefie (tylko do odczytu).</summary>
    IReadOnlyList<Card> Cards { get; }
}
