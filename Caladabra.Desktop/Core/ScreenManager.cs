using SFML.Window;

namespace Caladabra.Desktop.Core;

/// <summary>
/// Zarządza ustawieniami ekranu i dostępnymi rozdzielczościami.
/// </summary>
public static class ScreenManager
{
    // Predefiniowane rozdzielczości
    private static readonly (uint Width, uint Height, string Label)[] PredefinedResolutions =
    [
        (1280, 720, "1280 x 720"),
        (1366, 768, "1366 x 768"),
        (1440, 810, "1440 x 810"),
        (1536, 864, "1536 x 864"),
        (1600, 900, "1600 x 900"),
        (1760, 990, "1760 x 990"),
        (1920, 1080, "1920 x 1080"),
        (2560, 1440, "2560 x 1440"),
        (3840, 2160, "3840 x 2160")
    ];

    /// <summary>
    /// Zwraca domyślną rozdzielczość okna - największą z predefiniowanych,
    /// ale mniejszą od rozdzielczości desktopu (okno musi się zmieścić z ramką).
    /// </summary>
    public static (uint Width, uint Height) GetDefaultWindowResolution()
    {
        var desktop = VideoMode.DesktopMode;

        // Znajdź największą rozdzielczość MNIEJSZĄ od desktopu
        var suitable = PredefinedResolutions
            .Where(r => r.Width < desktop.Size.X && r.Height < desktop.Size.Y)
            .OrderByDescending(r => r.Width * r.Height)
            .FirstOrDefault();

        // Fallback: najmniejsza z listy jeśli żadna nie pasuje
        return suitable != default
            ? (suitable.Width, suitable.Height)
            : (PredefinedResolutions[0].Width, PredefinedResolutions[0].Height);
    }

    /// <summary>
    /// Zwraca rozdzielczości dostępne dla bieżącego monitora
    /// (równe lub mniejsze od rozdzielczości pulpitu).
    /// </summary>
    public static (uint Width, uint Height, string Label)[] GetAvailableResolutions()
    {
        var desktop = VideoMode.DesktopMode;
        var available = PredefinedResolutions
            .Where(r => r.Width <= desktop.Size.X && r.Height <= desktop.Size.Y)
            .ToList();

        // Jeśli rozdzielczość monitora nie jest na liście, dodaj ją na końcu
        bool desktopInList = available.Any(r =>
            r.Width == desktop.Size.X && r.Height == desktop.Size.Y);

        if (!desktopInList)
        {
            available.Add((desktop.Size.X, desktop.Size.Y,
                $"{desktop.Size.X} x {desktop.Size.Y}"));
        }

        return available.ToArray();
    }
}
