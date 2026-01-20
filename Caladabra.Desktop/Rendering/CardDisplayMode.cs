namespace Caladabra.Desktop.Rendering;

/// <summary>
/// Tryby wyświetlania kart w zależności od kontekstu.
/// </summary>
public enum CardDisplayMode
{
    /// <summary>
    /// Pełna karta z instrukcją i flavor textem.
    /// Używane w: Preview (lewy panel), CardList.
    /// </summary>
    Full,

    /// <summary>
    /// Mała karta - tylko statystyki, nazwa i ilustracja.
    /// BEZ instrukcji i flavor text.
    /// Używane w: Hand, Table.
    /// </summary>
    Small,

    /// <summary>
    /// Miniaturka - tylko kolor smaku.
    /// Używane w: Stomach.
    /// </summary>
    Tiny,

    /// <summary>
    /// Rewers karty z widocznym kolorem smaku.
    /// Używane w: Pantry.
    /// </summary>
    Back
}
