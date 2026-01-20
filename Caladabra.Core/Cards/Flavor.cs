namespace Caladabra.Core.Cards;

/// <summary>
/// Smaki kart - główne archetypy w grze Caladabra.
/// Każda karta ma dokładnie jeden smak.
/// </summary>
public enum Flavor
{
    /// <summary>Słone - spalacze tłuszczu. Wysoki koszt SW, niska kaloryczność.</summary>
    Salty,

    /// <summary>Słodkie - motywatory. Dają SW, ale z haczykiem.</summary>
    Sweet,

    /// <summary>Gorzkie - blokery. Niski koszt SW, wysoka kaloryczność, negatywne efekty.</summary>
    Bitter,

    /// <summary>Ostre - bomby. Wywracają zasady gry.</summary>
    Spicy,

    /// <summary>Kwaśne - receptury. Działają z opóźnieniem, wymagają kombinacji smaków.</summary>
    Sour,

    /// <summary>Umami - mutacje. Zmieniają inne karty.</summary>
    Umami
}

/// <summary>
/// Rozszerzenia dla enum Flavor.
/// </summary>
public static class FlavorExtensions
{
    /// <summary>Polska nazwa smaku.</summary>
    public static string ToPolishName(this Flavor flavor) => flavor switch
    {
        Flavor.Salty => "Słony",
        Flavor.Sweet => "Słodki",
        Flavor.Bitter => "Gorzki",
        Flavor.Spicy => "Ostry",
        Flavor.Sour => "Kwaśny",
        Flavor.Umami => "Umami",
        _ => flavor.ToString()
    };

    /// <summary>Kolor konsoli dla smaku.</summary>
    public static ConsoleColor ToConsoleColor(this Flavor flavor) => flavor switch
    {
        Flavor.Salty => ConsoleColor.Gray,
        Flavor.Sweet => ConsoleColor.Magenta,
        Flavor.Bitter => ConsoleColor.DarkYellow,
        Flavor.Spicy => ConsoleColor.Red,
        Flavor.Sour => ConsoleColor.Green,
        Flavor.Umami => ConsoleColor.Cyan,
        _ => ConsoleColor.White
    };
}
