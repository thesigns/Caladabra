using System.Text;
using SFML.Graphics;
using SFML.System;

namespace Caladabra.Desktop.UI;

public enum TextAlignment
{
    Left,
    Center,
    Right
}

public sealed class TextBox : IDrawable
{
    private readonly List<Text> _lines = new();
    private readonly Font _font;

    public string Content { get; set; } = "";
    public uint CharacterSize { get; set; } = 14;
    public float MaxWidth { get; set; } = 200f;
    public Color FillColor { get; set; } = Color.Black;
    public TextAlignment Alignment { get; set; } = TextAlignment.Left;
    public Text.Styles Style { get; set; } = Text.Styles.Regular;
    public float LineSpacing { get; set; } = 1.2f;

    public Vector2f Position { get; set; }
    public float Height { get; private set; }

    public TextBox(Font font)
    {
        _font = font;
    }

    public void WrapText()
    {
        _lines.Clear();
        Height = 0;

        if (string.IsNullOrEmpty(Content))
            return;

        var words = Content.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var currentLine = new StringBuilder();
        float lineHeight = _font.GetLineSpacing(CharacterSize) * LineSpacing;
        float y = 0;

        foreach (var word in words)
        {
            var testLine = currentLine.Length == 0
                ? word
                : currentLine + " " + word;

            var testText = new Text(_font, testLine, CharacterSize);
            var testWidth = testText.GetLocalBounds().Size.X;

            if (testWidth > MaxWidth && currentLine.Length > 0)
            {
                // Current line is full, add it and start new line
                AddLine(currentLine.ToString(), y);
                y += lineHeight;
                currentLine.Clear();
                currentLine.Append(word);
            }
            else
            {
                if (currentLine.Length > 0)
                    currentLine.Append(' ');
                currentLine.Append(word);
            }
        }

        // Add last line
        if (currentLine.Length > 0)
        {
            AddLine(currentLine.ToString(), y);
            y += lineHeight;
        }

        Height = y;
    }

    private void AddLine(string text, float y)
    {
        var line = new Text(_font, text, CharacterSize)
        {
            FillColor = FillColor,
            Style = Style,
            Position = new Vector2f(GetAlignedX(text), Position.Y + y)
        };
        _lines.Add(line);
    }

    private float GetAlignedX(string text)
    {
        var tempText = new Text(_font, text, CharacterSize);
        float textWidth = tempText.GetLocalBounds().Size.X;

        return Alignment switch
        {
            TextAlignment.Left => Position.X,
            TextAlignment.Center => Position.X + (MaxWidth - textWidth) / 2,
            TextAlignment.Right => Position.X + MaxWidth - textWidth,
            _ => Position.X
        };
    }

    public void UpdatePositions()
    {
        float lineHeight = _font.GetLineSpacing(CharacterSize) * LineSpacing;
        float y = 0;

        foreach (var line in _lines)
        {
            var text = line.DisplayedString;
            line.Position = new Vector2f(GetAlignedX(text), Position.Y + y);
            y += lineHeight;
        }
    }

    public void Draw(IRenderTarget target, RenderStates states)
    {
        foreach (var line in _lines)
            target.Draw(line, states);
    }
}
