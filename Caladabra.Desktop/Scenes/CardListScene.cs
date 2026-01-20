using SFML.Graphics;
using SFML.Window;
using SFML.System;
using Caladabra.Core.Cards;
using Caladabra.Core.State;
using Caladabra.Desktop.Core;
using Caladabra.Desktop.Rendering;
using Caladabra.Desktop.UI;

namespace Caladabra.Desktop.Scenes;

/// <summary>
/// Tryb wyświetlania listy kart.
/// </summary>
public enum CardListMode
{
    /// <summary>Tylko przeglądanie.</summary>
    Browse,
    /// <summary>Wybór karty (dla efektu).</summary>
    Select
}

/// <summary>
/// Reprezentuje kartę w liście z jej indeksem wyboru.
/// </summary>
public sealed class CardListItem
{
    public required Card Card { get; init; }
    public required int ChoiceIndex { get; init; }  // Indeks dla MakeChoice (-1 dla Browse mode)
}

/// <summary>
/// Scena z listą wszystkich kart Caladabra.
/// Wyświetla grid 6x2 z możliwością scrollowania.
/// </summary>
public sealed class CardListScene : IScene
{
    private readonly Game _game;
    private readonly List<CardListItem> _cardItems;
    private CardRenderer _cardRenderer = null!;

    // Tryb i callbacki
    public CardListMode Mode { get; set; } = CardListMode.Browse;
    public Flavor? FlavorFilter { get; set; }
    public Action<int>? OnCardSelected { get; set; }
    public Action? OnClose { get; set; }

    // Layout
    private const int CardsPerRow = 6;
    private const int RowsVisible = 2;
    private const float CardScale = 1.0f;
    private const float CardSpacing = 15f;
    private const float ScrollbarWidth = 20f;
    private const float PaddingHorizontal = 40f;
    private const float PaddingVertical = 30f;
    private const float HeaderHeight = 50f;

    // Scroll state
    private float _scrollOffset = 0f;
    private float _maxScrollOffset = 0f;
    private bool _isDraggingScrollbar = false;
    private float _scrollbarDragStartY = 0f;
    private float _scrollbarDragStartOffset = 0f;

    // Hover state
    private int _hoveredCardIndex = -1;
    private Vector2i _mousePosition;

    // Computed layout values
    private float _contentStartX;
    private float _contentStartY;
    private float _cardWidth;
    private float _cardHeight;
    private float _rowHeight;
    private float _totalContentHeight;
    private float _visibleHeight;
    private FloatRect _scrollbarTrackRect;
    private FloatRect _scrollbarThumbRect;

    /// <summary>
    /// Konstruktor dla trybu Browse - pokazuje wszystkie karty.
    /// </summary>
    public CardListScene(Game game, Flavor? flavorFilter = null)
    {
        _game = game;
        FlavorFilter = flavorFilter;
        Mode = CardListMode.Browse;

        // Pobierz karty z rejestru
        var allCards = CardRegistry.Instance.GetAll().ToList();

        // Filtruj po smaku jeśli podano
        if (FlavorFilter.HasValue)
        {
            allCards = allCards.Where(c => c.Flavor == FlavorFilter.Value).ToList();
        }

        // Sortuj po smaku, potem po nazwie
        _cardItems = allCards
            .OrderBy(c => c.Flavor)
            .ThenBy(c => c.Name)
            .Select(c => new CardListItem { Card = c, ChoiceIndex = -1 })
            .ToList();
    }

    /// <summary>
    /// Konstruktor dla trybu Select - używa opcji z PendingChoice.
    /// </summary>
    public CardListScene(Game game, List<ChoiceOption> options, Flavor? flavorFilter = null)
    {
        _game = game;
        FlavorFilter = flavorFilter;
        Mode = CardListMode.Select;

        // Używamy dokładnie opcji z PendingChoice (zachowując indeksy!)
        _cardItems = options
            .Select(o => new CardListItem { Card = o.Card, ChoiceIndex = o.Index })
            .ToList();
    }

    public void Enter()
    {
        _cardRenderer = new CardRenderer(_game.Assets.DefaultFont, _game.Scale);
        CalculateLayout();
    }

    public void Exit() { }

    private void CalculateLayout()
    {
        var cardSize = _cardRenderer.GetCardSize(CardScale);
        _cardWidth = cardSize.X;
        _cardHeight = cardSize.Y;
        _rowHeight = _cardHeight + _game.Scale.S(CardSpacing);

        // Pozycja startu contentu
        _contentStartX = _game.Scale.S(PaddingHorizontal);
        _contentStartY = _game.Scale.S(PaddingVertical + HeaderHeight);

        // Widoczna wysokość (2 rzędy)
        _visibleHeight = RowsVisible * _rowHeight;

        // Całkowita wysokość contentu
        int totalRows = (int)Math.Ceiling(_cardItems.Count / (float)CardsPerRow);
        _totalContentHeight = totalRows * _rowHeight;

        // Maksymalny scroll
        _maxScrollOffset = Math.Max(0, _totalContentHeight - _visibleHeight);

        // Scrollbar track (po prawej stronie)
        float trackX = _game.Scale.CurrentWidth - _game.Scale.S(PaddingHorizontal + ScrollbarWidth);
        float trackY = _contentStartY;
        float trackHeight = _visibleHeight;
        _scrollbarTrackRect = new FloatRect(
            new Vector2f(trackX, trackY),
            new Vector2f(_game.Scale.S(ScrollbarWidth), trackHeight)
        );

        UpdateScrollbarThumb();
    }

    private void UpdateScrollbarThumb()
    {
        if (_maxScrollOffset <= 0)
        {
            // Nie potrzeba scrollbara
            _scrollbarThumbRect = _scrollbarTrackRect;
            return;
        }

        float thumbHeight = Math.Max(
            _game.Scale.S(30f),
            _scrollbarTrackRect.Size.Y * (_visibleHeight / _totalContentHeight)
        );

        float scrollRatio = _scrollOffset / _maxScrollOffset;
        float thumbY = _scrollbarTrackRect.Position.Y +
                       scrollRatio * (_scrollbarTrackRect.Size.Y - thumbHeight);

        _scrollbarThumbRect = new FloatRect(
            new Vector2f(_scrollbarTrackRect.Position.X, thumbY),
            new Vector2f(_scrollbarTrackRect.Size.X, thumbHeight)
        );
    }

    public void HandleEvent(Event sfmlEvent)
    {
        switch (sfmlEvent.Type)
        {
            case EventType.KeyPressed:
                if (sfmlEvent.Key.Code == Keyboard.Key.Escape)
                {
                    OnClose?.Invoke();
                    _game.SceneManager.PopScene();
                }
                break;

            case EventType.MouseMoved:
                _mousePosition = sfmlEvent.MouseMove.Position;
                UpdateHoveredCard();

                if (_isDraggingScrollbar)
                {
                    float deltaY = _mousePosition.Y - _scrollbarDragStartY;
                    float scrollRange = _scrollbarTrackRect.Size.Y - _scrollbarThumbRect.Size.Y;
                    if (scrollRange > 0)
                    {
                        float scrollDelta = deltaY / scrollRange * _maxScrollOffset;
                        _scrollOffset = Math.Clamp(_scrollbarDragStartOffset + scrollDelta, 0, _maxScrollOffset);
                        UpdateScrollbarThumb();
                    }
                }
                break;

            case EventType.MouseButtonPressed:
                if (sfmlEvent.MouseButton.Button == Mouse.Button.Left)
                {
                    var clickPos = new Vector2f(sfmlEvent.MouseButton.Position.X, sfmlEvent.MouseButton.Position.Y);

                    // Sprawdź kliknięcie scrollbara
                    if (_scrollbarThumbRect.Contains(clickPos))
                    {
                        _isDraggingScrollbar = true;
                        _scrollbarDragStartY = clickPos.Y;
                        _scrollbarDragStartOffset = _scrollOffset;
                    }
                    // Sprawdź kliknięcie w track (poza thumbem)
                    else if (_scrollbarTrackRect.Contains(clickPos))
                    {
                        // Skocz do pozycji
                        float clickRatio = (clickPos.Y - _scrollbarTrackRect.Position.Y) / _scrollbarTrackRect.Size.Y;
                        _scrollOffset = Math.Clamp(clickRatio * _maxScrollOffset, 0, _maxScrollOffset);
                        UpdateScrollbarThumb();
                    }
                    // Sprawdź kliknięcie karty
                    else if (_hoveredCardIndex >= 0 && Mode == CardListMode.Select)
                    {
                        var choiceIndex = _cardItems[_hoveredCardIndex].ChoiceIndex;
                        OnCardSelected?.Invoke(choiceIndex);
                    }
                }
                break;

            case EventType.MouseButtonReleased:
                if (sfmlEvent.MouseButton.Button == Mouse.Button.Left)
                {
                    _isDraggingScrollbar = false;
                }
                break;

            case EventType.MouseWheelScrolled:
                if (sfmlEvent.MouseWheelScroll.Wheel == Mouse.Wheel.Vertical)
                {
                    float scrollAmount = _game.Scale.S(40f);
                    _scrollOffset = Math.Clamp(
                        _scrollOffset - sfmlEvent.MouseWheelScroll.Delta * scrollAmount,
                        0,
                        _maxScrollOffset
                    );
                    UpdateScrollbarThumb();
                    UpdateHoveredCard();
                }
                break;
        }
    }

    private void UpdateHoveredCard()
    {
        _hoveredCardIndex = -1;

        float mouseX = _mousePosition.X;
        float mouseY = _mousePosition.Y;

        // Sprawdź czy w obszarze kart
        if (mouseY < _contentStartY || mouseY > _contentStartY + _visibleHeight)
            return;

        // Oblicz który rząd i kolumnę
        float adjustedY = mouseY - _contentStartY + _scrollOffset;
        int row = (int)(adjustedY / _rowHeight);
        int col = (int)((mouseX - _contentStartX) / (_cardWidth + _game.Scale.S(CardSpacing)));

        if (col < 0 || col >= CardsPerRow)
            return;

        int cardIndex = row * CardsPerRow + col;
        if (cardIndex < 0 || cardIndex >= _cardItems.Count)
            return;

        // Sprawdź dokładnie czy w obrębie karty
        float cardX = _contentStartX + col * (_cardWidth + _game.Scale.S(CardSpacing));
        float cardY = _contentStartY + row * _rowHeight - _scrollOffset;

        if (mouseX >= cardX && mouseX <= cardX + _cardWidth &&
            mouseY >= cardY && mouseY <= cardY + _cardHeight)
        {
            _hoveredCardIndex = cardIndex;
        }
    }

    public void Update(float deltaTime) { }

    public void Render(RenderWindow window)
    {
        // Półprzezroczyste tło (overlay)
        var overlay = new RectangleShape(new Vector2f(_game.Scale.CurrentWidth, _game.Scale.CurrentHeight))
        {
            FillColor = new Color(0, 0, 0, 200)
        };
        window.Draw(overlay);

        // Nagłówek
        DrawHeader(window);

        // Karty (z clipping przez renderowanie tylko widocznych)
        DrawCards(window);

        // Scrollbar
        DrawScrollbar(window);

        // Podpowiedź na dole
        DrawFooter(window);
    }

    private void DrawHeader(RenderWindow window)
    {
        string title = Mode == CardListMode.Select
            ? "Wybierz kartę"
            : "Lista Kart Caladabra";

        if (FlavorFilter.HasValue)
        {
            title += $" ({FlavorFilter.Value.ToPolishName()})";
        }

        var titleText = new Text(_game.Assets.DefaultFont, title, _game.Scale.S(Theme.FontSizeTitle))
        {
            FillColor = Theme.TextPrimary
        };
        var titleBounds = titleText.GetLocalBounds();
        titleText.Position = new Vector2f(
            (_game.Scale.CurrentWidth - titleBounds.Size.X) / 2,
            _game.Scale.S(PaddingVertical)
        );
        window.Draw(titleText);
    }

    private void DrawCards(RenderWindow window)
    {
        int startRow = (int)(_scrollOffset / _rowHeight);
        int endRow = startRow + RowsVisible + 1;  // +1 dla częściowo widocznych

        for (int row = startRow; row <= endRow; row++)
        {
            for (int col = 0; col < CardsPerRow; col++)
            {
                int cardIndex = row * CardsPerRow + col;
                if (cardIndex >= _cardItems.Count)
                    break;

                var cardItem = _cardItems[cardIndex];

                float cardX = _contentStartX + col * (_cardWidth + _game.Scale.S(CardSpacing));
                float cardY = _contentStartY + row * _rowHeight - _scrollOffset;

                // Sprawdź czy karta jest widoczna
                if (cardY + _cardHeight < _contentStartY || cardY > _contentStartY + _visibleHeight)
                    continue;

                var cardPos = new Vector2f(cardX, cardY);

                // Highlight dla hovered
                if (cardIndex == _hoveredCardIndex)
                {
                    var highlight = new RectangleShape(new Vector2f(_cardWidth + 6, _cardHeight + 6))
                    {
                        Position = cardPos - new Vector2f(3, 3),
                        FillColor = Color.Transparent,
                        OutlineColor = Mode == CardListMode.Select
                            ? new Color(255, 200, 50)
                            : new Color(150, 150, 150),
                        OutlineThickness = 3
                    };
                    window.Draw(highlight);
                }

                _cardRenderer.Draw(window, cardItem.Card, cardPos, CardDisplayMode.Small, CardScale);
            }
        }
    }

    private void DrawScrollbar(RenderWindow window)
    {
        if (_maxScrollOffset <= 0)
            return;

        // Track
        var track = new RectangleShape(_scrollbarTrackRect.Size)
        {
            Position = _scrollbarTrackRect.Position,
            FillColor = new Color(40, 40, 45)
        };
        window.Draw(track);

        // Thumb
        var thumb = new RectangleShape(_scrollbarThumbRect.Size)
        {
            Position = _scrollbarThumbRect.Position,
            FillColor = _isDraggingScrollbar
                ? new Color(120, 120, 130)
                : new Color(80, 80, 90)
        };
        window.Draw(thumb);
    }

    private void DrawFooter(RenderWindow window)
    {
        string hint = Mode == CardListMode.Select
            ? "Kliknij kartę aby wybrać | ESC - anuluj"
            : "ESC - zamknij | Scroll - przewiń";

        var hintText = new Text(_game.Assets.DefaultFont, hint, _game.Scale.S(Theme.FontSizeSmall))
        {
            FillColor = Theme.TextSecondary
        };
        var hintBounds = hintText.GetLocalBounds();
        hintText.Position = new Vector2f(
            (_game.Scale.CurrentWidth - hintBounds.Size.X) / 2,
            _game.Scale.CurrentHeight - _game.Scale.S(PaddingVertical + 20f)
        );
        window.Draw(hintText);
    }
}
