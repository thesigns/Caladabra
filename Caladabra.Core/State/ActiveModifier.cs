using Caladabra.Core.Cards;

namespace Caladabra.Core.State;

/// <summary>
/// Aktywny modyfikator gry pochodzący od karty na stole.
/// </summary>
public sealed class ActiveModifier
{
    /// <summary>Typ modyfikatora.</summary>
    public required ModifierType Type { get; init; }

    /// <summary>Wartość modyfikatora (np. +1 do dobrania, -3 kalorie).</summary>
    public required int Value { get; init; }

    /// <summary>Karta źródłowa która utworzyła ten modyfikator.</summary>
    public required Card SourceCard { get; init; }
}
