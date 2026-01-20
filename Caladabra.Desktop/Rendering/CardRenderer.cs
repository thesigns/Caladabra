using SFML.Graphics;
using SFML.System;
using Caladabra.Core.Cards;
using Caladabra.Desktop.Core;
using Caladabra.Desktop.UI;

namespace Caladabra.Desktop.Rendering;

public sealed class CardRenderer
{
    private readonly Font _font;
    private readonly ScaleManager _scale;

    // Proporcje karty (aspect ratio) - szerokość / wysokość
    public const float AspectRatio = 13f / 18f;  // ≈ 0.722

    // Wysokość referencyjna (dla skali 1.0 przy rozdzielczości bazowej)
    public const float BaseHeight = 222f;
    public const float BaseWidth = BaseHeight * AspectRatio;  // ≈ 160f

    // Proporcje elementów karty (jako ułamek wysokości)
    private const float BorderRatio = 0.014f;      // grubość ramki
    private const float PaddingRatio = 0.036f;     // wewnętrzny margines
    private const float TopBarRatio = 0.113f;      // wysokość górnego paska (SW, smak, Kal)
    private const float NameRatio = 0.158f;        // wysokość sekcji nazwy
    private const float IllustrationRatio = 0.338f; // wysokość ilustracji
    private const float InstructionRatio = 0.248f; // wysokość instrukcji
    private const float FlavorTextRatio = 0.144f;  // wysokość flavor text

    // Proporcje fontów (jako ułamek wysokości karty)
    private const float FontStatRatio = 0.050f;
    private const float FontFlavorRatio = 0.041f;
    private const float FontNameRatio = 0.059f;
    private const float FontInstructionRatio = 0.045f;
    private const float FontFlavorTextRatio = 0.041f;

    public CardRenderer(Font font, ScaleManager scale)
    {
        _font = font;
        _scale = scale;
    }

    /// <summary>
    /// Rysuje kartę w określonym trybie i skali.
    /// </summary>
    /// <param name="target">Cel renderowania</param>
    /// <param name="card">Karta do narysowania</param>
    /// <param name="position">Pozycja lewego górnego rogu</param>
    /// <param name="mode">Tryb wyświetlania (Full, Small, Tiny, Back)</param>
    /// <param name="scale">Skala karty (1.0 = rozmiar bazowy)</param>
    public void Draw(IRenderTarget target, Card card, Vector2f position,
                     CardDisplayMode mode = CardDisplayMode.Full, float scale = 1.0f)
    {
        // Rozmiar obliczany z proporcji i skali
        float height = _scale.S(BaseHeight * scale);
        float width = height * AspectRatio;

        // Elementy proporcjonalne do rozmiaru karty
        float borderWidth = height * BorderRatio;
        float padding = height * PaddingRatio;

        // Tryb Back - rewers karty
        if (mode == CardDisplayMode.Back)
        {
            DrawCardBackground(target, position, width, height, card.Flavor, borderWidth);
            DrawCardBack(target, position, width, height, card.Flavor, padding);
            return;
        }

        // Tryb Tiny - tylko kolor smaku
        if (mode == CardDisplayMode.Tiny)
        {
            DrawCardBackground(target, position, width, height, card.Flavor, borderWidth);
            // W przyszłości można dodać ikonkę smaku
            return;
        }

        // Tryby Full i Small
        DrawCardBackground(target, position, width, height, card.Flavor, borderWidth);
        DrawTopBar(target, card, position, width, height, padding);
        DrawName(target, card.Name, position, width, height);

        // Tryb Full - dodaj instrukcję i flavor text
        if (mode == CardDisplayMode.Full)
        {
            DrawInstruction(target, card.Instruction, position, width, height, padding, card.Flavor);

            if (!string.IsNullOrEmpty(card.FlavorText))
            {
                DrawFlavorText(target, card.FlavorText, position, width, height, padding);
            }
        }
    }

    private void DrawCardBackground(IRenderTarget target, Vector2f pos, float width, float height,
                                     Flavor flavor, float borderWidth)
    {
        var bgColor = FlavorColors.GetBackground(flavor);
        var borderColor = FlavorColors.GetBorder(flavor);

        // Ramka (większy prostokąt)
        var borderRect = new RectangleShape(new Vector2f(width, height))
        {
            Position = pos,
            FillColor = borderColor
        };
        target.Draw(borderRect);

        // Tło wewnętrzne
        var bgRect = new RectangleShape(new Vector2f(width - borderWidth * 2, height - borderWidth * 2))
        {
            Position = pos + new Vector2f(borderWidth, borderWidth),
            FillColor = bgColor
        };
        target.Draw(bgRect);
    }

    private void DrawCardBack(IRenderTarget target, Vector2f pos, float width, float height,
                               Flavor flavor, float padding)
    {
        var borderColor = FlavorColors.GetBorder(flavor);
        var bgColor = FlavorColors.GetBackground(flavor);
        float innerPadding = padding * 2;

        // Wewnętrzny prostokąt (wzór rewersu)
        var innerRect = new RectangleShape(new Vector2f(width - innerPadding * 2, height - innerPadding * 2))
        {
            Position = pos + new Vector2f(innerPadding, innerPadding),
            FillColor = borderColor,
            OutlineColor = bgColor,
            OutlineThickness = height * 0.009f
        };
        target.Draw(innerRect);

        // Znak "C" na środku
        uint centerFontSize = (uint)(height * 0.18f);
        var centerText = new Text(_font, "C", centerFontSize)
        {
            FillColor = bgColor
        };
        var bounds = centerText.GetLocalBounds();
        centerText.Position = new Vector2f(
            pos.X + (width - bounds.Size.X) / 2,
            pos.Y + (height - bounds.Size.Y) / 2 - height * 0.045f
        );
        target.Draw(centerText);
    }

    private void DrawTopBar(IRenderTarget target, Card card, Vector2f pos, float width, float height, float padding)
    {
        uint fontSize = (uint)(height * FontStatRatio);
        uint flavorFontSize = (uint)(height * FontFlavorRatio);
        var textColor = FlavorColors.GetText(card.Flavor);

        // Koszt SW (lewo)
        var swText = new Text(_font, $"SW:{card.WillpowerCost}", fontSize)
        {
            FillColor = textColor,
            Position = pos + new Vector2f(padding, padding)
        };
        target.Draw(swText);

        // Nazwa smaku (środek)
        var flavorName = card.Flavor.ToPolishName();
        var flavorText = new Text(_font, flavorName, flavorFontSize)
        {
            FillColor = textColor
        };
        var flavorBounds = flavorText.GetLocalBounds();
        flavorText.Position = new Vector2f(
            pos.X + (width - flavorBounds.Size.X) / 2,
            pos.Y + padding
        );
        target.Draw(flavorText);

        // Kalorie (prawo)
        var calText = new Text(_font, $"Kal:{card.Calories}", fontSize)
        {
            FillColor = textColor
        };
        var calBounds = calText.GetLocalBounds();
        calText.Position = new Vector2f(
            pos.X + width - calBounds.Size.X - padding,
            pos.Y + padding
        );
        target.Draw(calText);
    }

    private void DrawName(IRenderTarget target, string name, Vector2f pos, float width, float height)
    {
        float topOffset = height * (TopBarRatio + 0.02f);
        uint fontSize = (uint)(height * FontNameRatio);

        var nameText = new Text(_font, name, fontSize)
        {
            FillColor = Color.Black
        };
        var bounds = nameText.GetLocalBounds();
        nameText.Position = new Vector2f(
            pos.X + (width - bounds.Size.X) / 2,
            pos.Y + topOffset
        );
        target.Draw(nameText);
    }

    private void DrawInstruction(IRenderTarget target, string instruction, Vector2f pos,
                                  float width, float height, float padding, Flavor flavor)
    {
        float topOffset = height * (TopBarRatio + NameRatio + IllustrationRatio);
        float maxWidth = width - padding * 2;
        uint fontSize = (uint)(height * FontInstructionRatio);

        var textBox = new TextBox(_font)
        {
            Content = instruction,
            CharacterSize = fontSize,
            MaxWidth = maxWidth,
            FillColor = FlavorColors.GetText(flavor),
            Alignment = TextAlignment.Left,
            Position = new Vector2f(pos.X + padding, pos.Y + topOffset),
            LineSpacing = 1.1f
        };
        textBox.WrapText();
        target.Draw(textBox);
    }

    private void DrawFlavorText(IRenderTarget target, string flavorText, Vector2f pos,
                                 float width, float height, float padding)
    {
        float bottomOffset = height - height * (FlavorTextRatio - 0.02f);
        float maxWidth = width - padding * 2;
        uint fontSize = (uint)(height * FontFlavorTextRatio);

        var textBox = new TextBox(_font)
        {
            Content = $"\"{flavorText}\"",
            CharacterSize = fontSize,
            MaxWidth = maxWidth,
            FillColor = new Color(60, 60, 60),
            Alignment = TextAlignment.Center,
            Style = Text.Styles.Italic,
            Position = new Vector2f(pos.X + padding, pos.Y + bottomOffset),
            LineSpacing = 1.0f
        };
        textBox.WrapText();
        target.Draw(textBox);
    }

    /// <summary>
    /// Oblicza rozmiar karty dla danej skali.
    /// </summary>
    public Vector2f GetCardSize(float scale = 1.0f)
    {
        float height = _scale.S(BaseHeight * scale);
        float width = height * AspectRatio;
        return new Vector2f(width, height);
    }
}
