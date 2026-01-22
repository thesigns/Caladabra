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
    private readonly AssetManager _assets;

    // Proporcje karty (aspect ratio) - szerokość / wysokość
    public const float AspectRatio = 13f / 18f;  // ≈ 0.722

    // Wysokość referencyjna (dla skali 1.0 przy rozdzielczości bazowej)
    public const float BaseHeight = 222f;
    public const float BaseWidth = BaseHeight * AspectRatio;  // ≈ 160f

    // Proporcje elementów karty (jako ułamek wysokości)
    private const float BorderRatio = 0.014f;      // grubość ramki
    private const float PaddingRatio = 0.046f;     // wewnętrzny margines
    private const float TopBarRatio = 0.113f;      // wysokość górnego paska (SW, smak, Kal)
    private const float NameRatio = 0.158f;        // wysokość sekcji nazwy
    private const float IllustrationRatio = 0.338f; // wysokość ilustracji
    private const float InstructionRatio = 0.248f; // wysokość instrukcji
    private const float FlavorTextRatio = 0.144f;  // wysokość flavor text

    // Proporcje fontów (jako ułamek wysokości karty)
    private const float FontStatRatio = 0.050f;
    private const float FontFlavorRatio = 0.041f;
    private const float FontNameRatio = 0.050f;       // mniejsza nazwa
    private const float FontInstructionRatio = 0.036f; // mniejsza instrukcja
    private const float FontFlavorTextRatio = 0.028f;  // malutki flavor text

    public CardRenderer(Font font, ScaleManager scale, AssetManager assets)
    {
        _font = font;
        _scale = scale;
        _assets = assets;
    }

    /// <summary>
    /// Rysuje kartę w określonym trybie i skali.
    /// </summary>
    /// <param name="target">Cel renderowania</param>
    /// <param name="card">Karta do narysowania</param>
    /// <param name="position">Pozycja lewego górnego rogu</param>
    /// <param name="mode">Tryb wyświetlania (Full, Small, Tiny, Back)</param>
    /// <param name="scale">Skala karty (1.0 = rozmiar bazowy)</param>
    /// <param name="tint">Opcjonalny kolor tintu (podświetlenie)</param>
    public void Draw(IRenderTarget target, Card card, Vector2f position,
                     CardDisplayMode mode = CardDisplayMode.Full, float scale = 1.0f,
                     Color? tint = null)
    {
        // Rozmiar obliczany z proporcji i skali
        float height = _scale.S(BaseHeight * scale);
        float width = height * AspectRatio;

        // Elementy proporcjonalne do rozmiaru karty
        float borderWidth = height * BorderRatio;
        float padding = height * PaddingRatio;

        // Tryb Back - rewers karty (tekstura + ikona)
        if (mode == CardDisplayMode.Back)
        {
            DrawCardBack(target, position, width, height, card.Flavor, padding, tint);
            return;
        }

        // Tryb Tiny - tło + ikona smaku na środku
        if (mode == CardDisplayMode.Tiny)
        {
            DrawCardBackground(target, position, width, height, card.Flavor, borderWidth, tint);

            // Ikona smaku na środku (50% wysokości karty)
            var icon = _assets.GetFlavorIcon(card.Flavor);
            var iconSize = height * 0.5f;
            var iconSprite = new Sprite(icon)
            {
                Position = position + new Vector2f((width - iconSize) / 2, (height - iconSize) / 2),
                Scale = new Vector2f(iconSize / icon.Size.X, iconSize / icon.Size.Y)
            };
            target.Draw(iconSprite);
            return;
        }

        // Tryby Full i Small
        DrawCardBackground(target, position, width, height, card.Flavor, borderWidth, tint);
        DrawTopBar(target, card, position, width, height, padding);
        DrawName(target, card.Name, position, width, height);
        DrawIllustration(target, card, position, width, height, mode);

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

    private void DrawIllustration(IRenderTarget target, Card card, Vector2f pos,
                                    float width, float height, CardDisplayMode mode)
    {
        float topOffset = height * (TopBarRatio + NameRatio);
        float illustrationHeight = height * IllustrationRatio;
        float illustrationY = pos.Y + topOffset;

        // Ilustracja karty
        var art = _assets.GetCardArt(card.Id);
        if (art != null)
        {
            // Skaluj zachowując proporcje
            float scaleX = width / art.Size.X;
            float scaleY = illustrationHeight / art.Size.Y;
            float baseScale = Math.Min(scaleX, scaleY);

            // Różna skala dla Full vs Small
            float scale = mode == CardDisplayMode.Full
                ? baseScale * 0.78f   // Full: +20%
                : baseScale * 1.43f;  // Small: +120%

            float scaledW = art.Size.X * scale;
            float scaledH = art.Size.Y * scale;

            // Small - trochę niżej
            float offsetY = mode == CardDisplayMode.Small
                ? illustrationHeight * 0.3f
                : illustrationHeight * -0.1f;

            var sprite = new Sprite(art)
            {
                Scale = new Vector2f(scale, scale),
                Position = new Vector2f(
                    pos.X + (width - scaledW) / 2,
                    illustrationY + (illustrationHeight - scaledH) / 2 + offsetY
                )
            };
            target.Draw(sprite);
        }
    }

    private void DrawCardBackground(IRenderTarget target, Vector2f pos, float width, float height,
                                     Flavor flavor, float borderWidth, Color? tint = null)
    {
        var texture = _assets.GetCardFront(flavor);
        var sprite = new Sprite(texture)
        {
            Position = pos,
            Scale = new Vector2f(width / texture.Size.X, height / texture.Size.Y),
            Color = tint ?? Color.White
        };
        target.Draw(sprite);
    }

    private void DrawCardBack(IRenderTarget target, Vector2f pos, float width, float height,
                               Flavor flavor, float padding, Color? tint = null)
    {
        // Tło rewersu z tekstury
        var texture = _assets.GetCardBack(flavor);
        var sprite = new Sprite(texture)
        {
            Position = pos,
            Scale = new Vector2f(width / texture.Size.X, height / texture.Size.Y),
            Color = tint ?? Color.White
        };
        target.Draw(sprite);

        // Ikona smaku na środku (40% wysokości karty)
        var icon = _assets.GetFlavorIcon(flavor);
        var iconSize = height * 0.4f;
        var iconSprite = new Sprite(icon)
        {
            Position = pos + new Vector2f((width - iconSize) / 2, (height - iconSize) / 2),
            Scale = new Vector2f(iconSize / icon.Size.X, iconSize / icon.Size.Y)
        };
        target.Draw(iconSprite);
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
        float bottomOffset = height - height * (FlavorTextRatio - 0.06f);  // niżej
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

    /// <summary>
    /// Rysuje licznik tury na karcie (duża cyfra po prawej stronie ilustracji).
    /// </summary>
    public void DrawCounter(IRenderTarget target, Vector2f cardPosition, float cardScale, int counter)
    {
        if (counter <= 0) return;

        float height = _scale.S(BaseHeight * cardScale);
        float width = height * AspectRatio;

        // Pozycja: prawa strona ilustracji
        float topOffset = height * (TopBarRatio + NameRatio);
        float illustrationHeight = height * IllustrationRatio;

        // Duży font (35% wysokości ilustracji)
        uint fontSize = (uint)(illustrationHeight * 0.35f);

        var counterText = new Text(_font, counter.ToString(), fontSize)
        {
            FillColor = Color.White,
            OutlineColor = Color.Black,
            OutlineThickness = _scale.S(2f),
            Style = Text.Styles.Bold
        };

        var bounds = counterText.GetLocalBounds();
        float padding = _scale.S(8f);

        // Pozycja: prawy dolny róg ilustracji
        counterText.Position = new Vector2f(
            cardPosition.X + width - bounds.Size.X - padding,
            cardPosition.Y + topOffset + illustrationHeight - bounds.Size.Y - padding
        );

        target.Draw(counterText);
    }
}
