namespace Caladabra.Core.Engine;

/// <summary>
/// Stałe i reguły gry Caladabra.
/// Wszystkie magiczne liczby w jednym miejscu.
/// </summary>
public static class GameRules
{
    // === Zasoby gracza ===

    /// <summary>Początkowa ilość Tłuszczu.</summary>
    public const int StartingFat = 100;

    /// <summary>Minimalna ilość Tłuszczu (cel gry).</summary>
    public const int MinFat = 0;

    /// <summary>Maksymalna liczba tur (dni diety). Po przekroczeniu gracz przegrywa.</summary>
    public const int MaxTurns = 30;

    /// <summary>Początkowa ilość Siły Woli.</summary>
    public const int StartingWillpower = 12;

    /// <summary>Maksymalna ilość Siły Woli.</summary>
    public const int MaxWillpower = 30;

    /// <summary>Minimalna ilość Siły Woli.</summary>
    public const int MinWillpower = 0;

    // === Strefy ===

    /// <summary>Maksymalna liczba kart w Ręce.</summary>
    public const int MaxHandSize = 5;

    /// <summary>Początkowa liczba kart w Ręce.</summary>
    public const int StartingHandSize = 5;

    /// <summary>Maksymalna liczba kart na Stole.</summary>
    public const int MaxTableSize = 3;

    /// <summary>Maksymalna liczba kart w Żołądku.</summary>
    public const int MaxStomachSize = 4;

    /// <summary>Liczba kart w Spiżarni na początku gry.</summary>
    public const int DeckSize = 60;

    /// <summary>Maksymalna liczba kopii tej samej karty w talii.</summary>
    public const int MaxCopiesPerCard = 4;

    // === Dobieranie kart ===

    /// <summary>Ile kart dobiera się po zagraniu karty.</summary>
    public const int DrawAfterPlay = 1;

    /// <summary>Ile kart dobiera się po zjedzeniu karty gdy w ręce jest 4 karty.</summary>
    public const int DrawAfterEatWith4Cards = 1;

    /// <summary>Ile kart dobiera się po zjedzeniu karty gdy w ręce jest mniej niż 4 karty.</summary>
    public const int DrawAfterEatWithLessThan4Cards = 2;

    /// <summary>Próg kart w ręce dla zwiększonego dobierania po jedzeniu.</summary>
    public const int EatDrawThreshold = 4;
}
