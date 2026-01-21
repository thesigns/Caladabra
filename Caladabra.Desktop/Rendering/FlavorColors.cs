using SFML.Graphics;
using Caladabra.Core.Cards;

namespace Caladabra.Desktop.Rendering;

public static class FlavorColors
{
    public static Color GetBackground(Flavor flavor) => flavor switch
    {
        Flavor.Salty => new Color(180, 180, 180),   // Light gray
        Flavor.Sweet => new Color(200, 100, 200),   // Magenta/Purple
        Flavor.Bitter => new Color(139, 90, 43),    // Brown
        Flavor.Spicy => new Color(220, 80, 60),     // Red-Orange
        Flavor.Sour => new Color(120, 200, 80),     // Lime green
        Flavor.Umami => new Color(80, 180, 220),    // Cyan/Blue
        _ => new Color(200, 200, 200)
    };

    public static Color GetBorder(Flavor flavor)
    {
        var bg = GetBackground(flavor);
        return new Color(
            (byte)(bg.R * 0.6),
            (byte)(bg.G * 0.6),
            (byte)(bg.B * 0.6)
        );
    }

    public static Color GetText(Flavor flavor)
    {
        // Wszystkie teksty na kartach są czarne (tła z bitmap są jasne)
        return Color.Black;
    }

    public static Color GetHighlight(Flavor flavor)
    {
        var bg = GetBackground(flavor);
        return new Color(
            (byte)Math.Min(255, bg.R + 40),
            (byte)Math.Min(255, bg.G + 40),
            (byte)Math.Min(255, bg.B + 40)
        );
    }
}
