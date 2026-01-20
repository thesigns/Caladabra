using SFML.Graphics;

namespace Caladabra.Desktop.UI;

public static class Theme
{
    // Background colors
    public static readonly Color BackgroundDark = new(30, 30, 35);
    public static readonly Color BackgroundMedium = new(45, 45, 50);
    public static readonly Color BackgroundLight = new(60, 60, 65);

    // Text colors
    public static readonly Color TextPrimary = new(240, 240, 240);
    public static readonly Color TextSecondary = new(180, 180, 180);
    public static readonly Color TextMuted = new(120, 120, 120);

    // Accent colors
    public static readonly Color AccentPrimary = new(100, 150, 220);
    public static readonly Color AccentSuccess = new(100, 200, 100);
    public static readonly Color AccentWarning = new(220, 180, 80);
    public static readonly Color AccentDanger = new(220, 80, 80);

    // Card dimensions (base, before scaling)
    public const float CardWidth = 160f;
    public const float CardHeight = 222f;
    public const float CardCornerRadius = 10f;
    public const float CardBorderWidth = 3f;

    // Font sizes (base, before scaling)
    public const uint FontSizeSmall = 12;
    public const uint FontSizeNormal = 14;
    public const uint FontSizeMedium = 18;
    public const uint FontSizeLarge = 24;
    public const uint FontSizeTitle = 32;

    // Card-specific font sizes
    public const uint CardFontSizeStat = 14;
    public const uint CardFontSizeFlavor = 10;
    public const uint CardFontSizeName = 13;
    public const uint CardFontSizeInstruction = 10;
    public const uint CardFontSizeFlavorText = 9;

    // UI spacing
    public const float PaddingSmall = 8f;
    public const float PaddingNormal = 16f;
    public const float PaddingLarge = 24f;

    // Status bar
    public const float StatusBarHeight = 50f;
}
