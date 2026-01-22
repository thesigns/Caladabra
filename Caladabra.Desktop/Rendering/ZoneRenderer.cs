using SFML.Graphics;
using SFML.System;
using Caladabra.Core.Cards;
using Caladabra.Core.Zones;
using Caladabra.Desktop.Core;
using Caladabra.Desktop.UI;

namespace Caladabra.Desktop.Rendering;

/// <summary>
/// Renderuje strefy gry (Hand, Table, Stomach, Pantry, Toilet).
/// </summary>
public sealed class ZoneRenderer
{
    private readonly CardRenderer _cardRenderer;
    private readonly Font _font;
    private readonly ScaleManager _scale;

    // Domyślne skale dla stref
    public const float HandScale = 1.0f;
    public const float TableScale = 1.0f;
    public const float StomachScale = 0.5f;
    public const float PantryScale = 0.6f;
    public const float ToiletScale = 0.5f;

    // Odstępy między kartami (proporcja do szerokości karty)
    private const float CardSpacingRatio = 0.15f;

    public ZoneRenderer(CardRenderer cardRenderer, Font font, ScaleManager scale)
    {
        _cardRenderer = cardRenderer;
        _font = font;
        _scale = scale;
    }

    /// <summary>
    /// Rysuje strefę ręki (Hand) - karty w poziomym rzędzie.
    /// </summary>
    public void DrawHand(IRenderTarget target, IReadOnlyList<Card> cards, Vector2f position, float? customScale = null)
    {
        float scale = customScale ?? HandScale;
        DrawHorizontalCards(target, cards, position, CardDisplayMode.Small, scale);
    }

    /// <summary>
    /// Rysuje strefę stołu (Table) - karty w poziomym rzędzie.
    /// </summary>
    public void DrawTable(IRenderTarget target, IReadOnlyList<Card> cards, Vector2f position, float? customScale = null)
    {
        float scale = customScale ?? TableScale;
        DrawHorizontalCards(target, cards, position, CardDisplayMode.Small, scale);
    }

    /// <summary>
    /// Rysuje żołądek (Stomach) - karty w pionowej kolumnie.
    /// </summary>
    public void DrawStomach(IRenderTarget target, IReadOnlyList<Card> cards, Vector2f position,
                             string? label = null, float? customScale = null)
    {
        float scale = customScale ?? StomachScale;
        var cardSize = _cardRenderer.GetCardSize(scale);
        float spacing = cardSize.Y * CardSpacingRatio;

        // Etykieta
        if (!string.IsNullOrEmpty(label))
        {
            DrawZoneLabel(target, label, position);
            position = new Vector2f(position.X, position.Y + _scale.S(25f));
        }

        // Karty w kolumnie
        for (int i = 0; i < cards.Count; i++)
        {
            var cardPos = new Vector2f(position.X, position.Y + i * (cardSize.Y + spacing));
            _cardRenderer.Draw(target, cards[i], cardPos, CardDisplayMode.Tiny, scale);
        }
    }

    /// <summary>
    /// Rysuje spiżarnię (Pantry) - stos kart (tylko górna widoczna jako rewers).
    /// </summary>
    public void DrawPantry(IRenderTarget target, IReadOnlyList<Card> cards, Vector2f position,
                            string? label = null, float? customScale = null)
    {
        float scale = customScale ?? PantryScale;
        var cardSize = _cardRenderer.GetCardSize(scale);

        // Etykieta z liczbą kart
        string displayLabel = label ?? $"Spiżarnia [{cards.Count}]";
        DrawZoneLabel(target, displayLabel, position);
        position = new Vector2f(position.X, position.Y + _scale.S(25f));

        if (cards.Count == 0)
        {
            // Puste miejsce na kartę (outline)
            DrawEmptySlot(target, position, cardSize);
            return;
        }

        // Rysuj efekt stosu (offsetowane cienie)
        int shadowCount = Math.Min(3, cards.Count - 1);
        for (int i = shadowCount; i > 0; i--)
        {
            float offset = _scale.S(2f) * i;
            var shadowPos = new Vector2f(position.X + offset, position.Y + offset);
            DrawCardShadow(target, shadowPos, cardSize);
        }

        // Górna karta (rewers z widocznym smakiem)
        var topCard = cards[^1];  // Ostatnia karta = górna w stosie
        _cardRenderer.Draw(target, topCard, position, CardDisplayMode.Back, scale);
    }

    /// <summary>
    /// Rysuje kibelek (Toilet) - stos odrzuconych kart.
    /// </summary>
    public void DrawToilet(IRenderTarget target, IReadOnlyList<Card> cards, Vector2f position,
                            string? label = null, float? customScale = null)
    {
        float scale = customScale ?? ToiletScale;
        var cardSize = _cardRenderer.GetCardSize(scale);

        // Etykieta z liczbą kart
        string displayLabel = label ?? $"Kibelek [{cards.Count}]";
        DrawZoneLabel(target, displayLabel, position);
        position = new Vector2f(position.X, position.Y + _scale.S(25f));

        if (cards.Count == 0)
        {
            DrawEmptySlot(target, position, cardSize);
            return;
        }

        // Efekt stosu
        int shadowCount = Math.Min(3, cards.Count - 1);
        for (int i = shadowCount; i > 0; i--)
        {
            float offset = _scale.S(2f) * i;
            var shadowPos = new Vector2f(position.X + offset, position.Y + offset);
            DrawCardShadow(target, shadowPos, cardSize);
        }

        // Górna karta (rewers bez smaku - szary)
        var topCard = cards[^1];
        _cardRenderer.Draw(target, topCard, position, CardDisplayMode.Back, scale);
    }

    /// <summary>
    /// Rysuje karty w poziomym rzędzie z określonym trybem i skalą.
    /// </summary>
    private void DrawHorizontalCards(IRenderTarget target, IReadOnlyList<Card> cards,
                                      Vector2f position, CardDisplayMode mode, float scale)
    {
        if (cards.Count == 0) return;

        var cardSize = _cardRenderer.GetCardSize(scale);
        float spacing = cardSize.X * CardSpacingRatio;

        for (int i = 0; i < cards.Count; i++)
        {
            var cardPos = new Vector2f(position.X + i * (cardSize.X + spacing), position.Y);
            _cardRenderer.Draw(target, cards[i], cardPos, mode, scale);
        }
    }

    /// <summary>
    /// Rysuje etykietę strefy.
    /// </summary>
    private void DrawZoneLabel(IRenderTarget target, string text, Vector2f position)
    {
        var label = new Text(_font, text, _scale.S(Theme.FontSizeSmall))
        {
            FillColor = Theme.TextSecondary,
            Position = position
        };
        target.Draw(label);
    }

    /// <summary>
    /// Rysuje puste miejsce na kartę.
    /// </summary>
    private void DrawEmptySlot(IRenderTarget target, Vector2f position, Vector2f size)
    {
        var slot = new RectangleShape(size)
        {
            Position = position,
            FillColor = new Color(40, 40, 45),
            OutlineColor = new Color(60, 60, 65),
            OutlineThickness = _scale.S(2f)
        };
        target.Draw(slot);
    }

    /// <summary>
    /// Rysuje cień karty (dla efektu stosu).
    /// </summary>
    private void DrawCardShadow(IRenderTarget target, Vector2f position, Vector2f size)
    {
        var shadow = new RectangleShape(size)
        {
            Position = position,
            FillColor = new Color(30, 30, 35)
        };
        target.Draw(shadow);
    }

    /// <summary>
    /// Oblicza całkowitą szerokość strefy poziomej (np. Hand, Table).
    /// </summary>
    public float GetHorizontalZoneWidth(int cardCount, float scale)
    {
        if (cardCount == 0) return 0;
        var cardSize = _cardRenderer.GetCardSize(scale);
        float spacing = cardSize.X * CardSpacingRatio;
        return cardCount * cardSize.X + (cardCount - 1) * spacing;
    }

    /// <summary>
    /// Oblicza całkowitą wysokość strefy pionowej (np. Stomach).
    /// </summary>
    public float GetVerticalZoneHeight(int cardCount, float scale, bool includeLabel = true)
    {
        if (cardCount == 0) return includeLabel ? _scale.S(25f) : 0;
        var cardSize = _cardRenderer.GetCardSize(scale);
        float spacing = cardSize.Y * CardSpacingRatio;
        float cardsHeight = cardCount * cardSize.Y + (cardCount - 1) * spacing;
        return includeLabel ? cardsHeight + _scale.S(25f) : cardsHeight;
    }
}
