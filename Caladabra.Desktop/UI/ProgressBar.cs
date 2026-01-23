using SFML.Graphics;
using SFML.System;
using Caladabra.Desktop.Core;

namespace Caladabra.Desktop.UI;

/// <summary>
/// Pasek postępu z etykietą tekstową, np. do wyświetlania postępu jedzenia karty.
/// </summary>
public sealed class ProgressBar
{
    private readonly Font _font;
    private readonly ScaleManager _scale;

    public Vector2f Position { get; set; }
    public Vector2f Size { get; set; }
    public float Progress { get; set; } // 0.0 - 1.0
    public string Label { get; set; } = string.Empty;
    public bool IsVisible { get; set; }

    public Color BackgroundColor { get; set; } = new(20, 20, 20, 220);
    public Color FillColor { get; set; } = Theme.AccentDanger;
    public Color TextColor { get; set; } = Theme.TextPrimary;

    public ProgressBar(Font font, ScaleManager scale)
    {
        _font = font;
        _scale = scale;
        Size = new Vector2f(_scale.S(120f), _scale.S(24f));
    }

    public void Draw(RenderWindow window)
    {
        if (!IsVisible) return;

        // Background
        var background = new RectangleShape(Size)
        {
            Position = Position,
            FillColor = BackgroundColor
        };
        window.Draw(background);

        // Fill (progress)
        var clampedProgress = Math.Clamp(Progress, 0f, 1f);
        if (clampedProgress > 0)
        {
            var fillWidth = Size.X * clampedProgress;
            var fill = new RectangleShape(new Vector2f(fillWidth, Size.Y))
            {
                Position = Position,
                FillColor = FillColor
            };
            window.Draw(fill);
        }

        // Label text (centered)
        if (!string.IsNullOrEmpty(Label))
        {
            var text = new Text(_font, Label, (uint)_scale.S(Theme.FontSizeNormal))
            {
                FillColor = TextColor
            };
            var textBounds = text.GetLocalBounds();
            text.Position = new Vector2f(
                Position.X + (Size.X - textBounds.Size.X) / 2 - textBounds.Position.X,
                Position.Y + (Size.Y - textBounds.Size.Y) / 2 - textBounds.Position.Y
            );
            window.Draw(text);
        }
    }
}
