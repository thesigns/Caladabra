using SFML.Graphics;
using SFML.Window;
using SFML.System;
using Caladabra.Core.Cards;
using Caladabra.Core.Engine;
using Caladabra.Core.State;
using Caladabra.Desktop.Core;
using Caladabra.Desktop.Rendering;
using Caladabra.Desktop.UI;
using Caladabra.Desktop.Integration;

namespace Caladabra.Desktop.Scenes;

/// <summary>
/// Główna scena rozgrywki - wyświetla planszę z kartami i statystykami.
/// </summary>
public sealed class GameScene : IScene
{
    private readonly Game _game;
    private readonly GameController _controller;
    private GameState State => _controller.State;

    // Renderery
    private CardRenderer _cardRenderer = null!;
    private ZoneRenderer _zoneRenderer = null!;

    // UI Elements
    private Text _fatLabel = null!;
    private Text _willpowerLabel = null!;
    private Text _turnLabel = null!;
    private Text _infoText = null!;

    // Hover & interaction state
    private Card? _hoveredCard;
    private int _hoveredHandIndex = -1;
    private Vector2i _mousePosition;

    // Double-click detection
    private Clock _clickClock = new();
    private int _lastClickedIndex = -1;
    private const float DoubleClickTime = 0.3f;  // 300ms

    // Hold-to-eat detection
    private Clock _holdClock = new();
    private bool _isHolding;
    private int _holdingIndex = -1;
    private const float HoldTime = 0.8f;  // 800ms

    // Layout constants (bazowe dla 1920x1080)
    private const float StatusBarHeight = 50f;
    private const float PreviewPanelWidth = 280f;
    private const float SidePanelWidth = 160f;
    private const float Padding = 20f;

    // Card spacing ratio (must match ZoneRenderer)
    private const float CardSpacingRatio = 0.15f;

    public GameScene(Game game, GameController controller)
    {
        _game = game;
        _controller = controller;
    }

    public void Enter()
    {
        var font = _game.Assets.DefaultFont;

        _cardRenderer = new CardRenderer(font, _game.Scale);
        _zoneRenderer = new ZoneRenderer(_cardRenderer, font, _game.Scale);

        // Status bar labels
        _fatLabel = new Text(font, "", _game.Scale.S(Theme.FontSizeNormal))
        {
            FillColor = Theme.TextPrimary
        };

        _willpowerLabel = new Text(font, "", _game.Scale.S(Theme.FontSizeNormal))
        {
            FillColor = Theme.TextPrimary
        };

        _turnLabel = new Text(font, "", _game.Scale.S(Theme.FontSizeNormal))
        {
            FillColor = Theme.TextPrimary
        };

        _infoText = new Text(font, "ESC = wyjście | Double-click = zagraj | Przytrzymaj = zjedz", _game.Scale.S(Theme.FontSizeSmall))
        {
            FillColor = Theme.TextSecondary
        };

        UpdateLayout();
    }

    public void Exit()
    {
        _clickClock.Dispose();
        _holdClock.Dispose();
    }

    public void HandleEvent(Event sfmlEvent)
    {
        if (sfmlEvent.Type == EventType.KeyPressed && sfmlEvent.Key.Code == Keyboard.Key.Escape)
        {
            _game.Window.Close();
        }

        if (sfmlEvent.Type == EventType.Resized)
        {
            UpdateLayout();
        }

        if (sfmlEvent.Type == EventType.MouseMoved)
        {
            _mousePosition = sfmlEvent.MouseMove.Position;
            UpdateHoveredCard();
        }

        if (sfmlEvent.Type == EventType.MouseButtonPressed && sfmlEvent.MouseButton.Button == Mouse.Button.Left)
        {
            HandleMouseDown();
        }

        if (sfmlEvent.Type == EventType.MouseButtonReleased && sfmlEvent.MouseButton.Button == Mouse.Button.Left)
        {
            HandleMouseUp();
        }
    }

    private void HandleMouseDown()
    {
        if (_hoveredHandIndex < 0) return;

        // Check for double-click
        float elapsed = _clickClock.ElapsedTime.AsSeconds();
        if (_lastClickedIndex == _hoveredHandIndex && elapsed < DoubleClickTime)
        {
            // Double-click detected - play card
            if (_controller.PlayCard(_hoveredHandIndex))
            {
                _lastClickedIndex = -1;
                _hoveredCard = null;
                _hoveredHandIndex = -1;
            }
            return;
        }

        // Start tracking for potential double-click or hold
        _clickClock.Restart();
        _lastClickedIndex = _hoveredHandIndex;

        // Start hold tracking
        _holdClock.Restart();
        _isHolding = true;
        _holdingIndex = _hoveredHandIndex;
    }

    private void HandleMouseUp()
    {
        _isHolding = false;
        _holdingIndex = -1;
    }

    public void Update(float deltaTime)
    {
        // Check for hold-to-eat
        if (_isHolding && _holdingIndex >= 0)
        {
            if (_holdClock.ElapsedTime.AsSeconds() >= HoldTime)
            {
                // Hold completed - eat card
                if (_controller.EatCard(_holdingIndex))
                {
                    _isHolding = false;
                    _holdingIndex = -1;
                    _hoveredCard = null;
                    _hoveredHandIndex = -1;
                    _lastClickedIndex = -1;
                }
            }
        }

        // Update status labels
        _fatLabel.DisplayedString = $"Tłuszcz: {State.Fat}";
        _willpowerLabel.DisplayedString = $"Siła Woli: {State.Willpower}/{GameRules.MaxWillpower}";
        _turnLabel.DisplayedString = $"Tura: {State.Turn}";

        // Update info text based on game state
        if (_controller.IsGameOver)
        {
            _infoText.DisplayedString = _controller.GetGameOverMessage();
            _infoText.FillColor = State.Phase == GamePhase.Won ? new Color(100, 200, 100) : new Color(200, 100, 100);
        }
        else if (_controller.IsAwaitingChoice)
        {
            _infoText.DisplayedString = "Wybierz opcję...";
        }
        else if (_isHolding && _holdingIndex >= 0)
        {
            float progress = _holdClock.ElapsedTime.AsSeconds() / HoldTime;
            int percent = (int)(progress * 100);
            _infoText.DisplayedString = $"Przytrzymaj aby zjeść... {percent}%";
        }
    }

    private void UpdateHoveredCard()
    {
        _hoveredCard = null;
        _hoveredHandIndex = -1;

        // Get hand cards and their positions
        var cards = State.Hand.Cards.ToList();
        if (cards.Count == 0) return;

        var cardSize = _cardRenderer.GetCardSize(ZoneRenderer.HandScale);
        float spacing = cardSize.X * CardSpacingRatio;
        float totalWidth = cards.Count * cardSize.X + (cards.Count - 1) * spacing;

        float centerX = _game.Scale.CurrentWidth / 2;
        float startX = centerX - totalWidth / 2;
        float y = _game.Scale.CurrentHeight - _game.Scale.S(280f);

        // Check each card
        for (int i = 0; i < cards.Count; i++)
        {
            float cardX = startX + i * (cardSize.X + spacing);
            var cardRect = new FloatRect(new Vector2f(cardX, y), cardSize);

            if (cardRect.Contains(new Vector2f(_mousePosition.X, _mousePosition.Y)))
            {
                _hoveredCard = cards[i];
                _hoveredHandIndex = i;
                break;
            }
        }
    }

    public void Render(RenderWindow window)
    {
        // Status bar
        DrawStatusBar(window);

        // Main game area
        DrawPreviewPanel(window);
        DrawPantryZone(window);
        DrawToiletZone(window);
        DrawStomachZone(window);
        DrawTableZone(window);
        DrawHandZone(window);

        // Info text at bottom
        DrawInfoBar(window);
    }

    private void DrawStatusBar(RenderWindow window)
    {
        float y = _game.Scale.S(Padding);

        // Background
        var barBg = new RectangleShape(new Vector2f(_game.Scale.CurrentWidth, _game.Scale.S(StatusBarHeight)))
        {
            Position = new Vector2f(0, 0),
            FillColor = new Color(35, 35, 40)
        };
        window.Draw(barBg);

        // Fat (left)
        _fatLabel.Position = new Vector2f(_game.Scale.S(Padding), y);
        window.Draw(_fatLabel);

        // Willpower (center)
        var wpBounds = _willpowerLabel.GetLocalBounds();
        _willpowerLabel.Position = new Vector2f(
            (_game.Scale.CurrentWidth - wpBounds.Size.X) / 2,
            y
        );
        window.Draw(_willpowerLabel);

        // Turn (right)
        var turnBounds = _turnLabel.GetLocalBounds();
        _turnLabel.Position = new Vector2f(
            _game.Scale.CurrentWidth - turnBounds.Size.X - _game.Scale.S(Padding),
            y
        );
        window.Draw(_turnLabel);
    }

    private void DrawPreviewPanel(RenderWindow window)
    {
        float x = _game.Scale.S(Padding);
        float y = _game.Scale.S(StatusBarHeight + Padding);
        float width = _game.Scale.S(PreviewPanelWidth);
        float height = _game.Scale.S(380f);

        // Panel background
        var panelBg = new RectangleShape(new Vector2f(width, height))
        {
            Position = new Vector2f(x, y),
            FillColor = new Color(40, 40, 45),
            OutlineColor = new Color(60, 60, 65),
            OutlineThickness = _game.Scale.S(2f)
        };
        window.Draw(panelBg);

        // Label
        var label = new Text(_game.Assets.DefaultFont, "Podgląd", _game.Scale.S(Theme.FontSizeSmall))
        {
            FillColor = Theme.TextSecondary,
            Position = new Vector2f(x + _game.Scale.S(10f), y + _game.Scale.S(5f))
        };
        window.Draw(label);

        // Draw hovered card preview
        if (_hoveredCard != null)
        {
            float previewScale = 1.4f;
            var cardSize = _cardRenderer.GetCardSize(previewScale);
            var cardPos = new Vector2f(
                x + (width - cardSize.X) / 2,
                y + _game.Scale.S(30f)
            );
            _cardRenderer.Draw(window, _hoveredCard, cardPos, CardDisplayMode.Full, previewScale);
        }
    }

    private void DrawPantryZone(RenderWindow window)
    {
        float x = _game.Scale.S(Padding);
        float y = _game.Scale.S(StatusBarHeight + 420f);

        var cards = State.Pantry.Cards.ToList();
        _zoneRenderer.DrawPantry(window, cards, new Vector2f(x, y), $"Spiżarnia [{cards.Count}]");
    }

    private void DrawToiletZone(RenderWindow window)
    {
        float x = _game.Scale.CurrentWidth - _game.Scale.S(SidePanelWidth + Padding);
        float y = _game.Scale.S(StatusBarHeight + 420f);

        var cards = State.Toilet.Cards.ToList();
        _zoneRenderer.DrawToilet(window, cards, new Vector2f(x, y), $"Kibelek [{cards.Count}]");
    }

    private void DrawStomachZone(RenderWindow window)
    {
        float x = _game.Scale.CurrentWidth - _game.Scale.S(SidePanelWidth + Padding);
        float y = _game.Scale.S(StatusBarHeight + Padding);

        var cards = State.Stomach.Cards.ToList();

        // Label
        var label = new Text(_game.Assets.DefaultFont, $"Żołądek [{cards.Count}]", _game.Scale.S(Theme.FontSizeSmall))
        {
            FillColor = Theme.TextSecondary,
            Position = new Vector2f(x, y)
        };
        window.Draw(label);

        // Draw stomach cards (vertical stack with Tiny mode)
        float cardY = y + _game.Scale.S(25f);
        var cardSize = _cardRenderer.GetCardSize(ZoneRenderer.StomachScale);

        for (int i = 0; i < Math.Min(cards.Count, 6); i++)  // Max 6 visible
        {
            var cardPos = new Vector2f(x, cardY + i * (cardSize.Y * 0.3f)); // Overlapping
            _cardRenderer.Draw(window, cards[i], cardPos, CardDisplayMode.Tiny, ZoneRenderer.StomachScale);
        }

        if (cards.Count > 6)
        {
            var moreLabel = new Text(_game.Assets.DefaultFont, $"+{cards.Count - 6} więcej", _game.Scale.S(12u))
            {
                FillColor = Theme.TextSecondary,
                Position = new Vector2f(x, cardY + 6 * (cardSize.Y * 0.3f) + cardSize.Y)
            };
            window.Draw(moreLabel);
        }
    }

    private void DrawTableZone(RenderWindow window)
    {
        // Center of screen
        float centerX = _game.Scale.CurrentWidth / 2;
        float y = _game.Scale.S(StatusBarHeight + 100f);

        var cards = State.Table.Cards.ToList();

        // Label
        var label = new Text(_game.Assets.DefaultFont, "Stół", _game.Scale.S(Theme.FontSizeSmall))
        {
            FillColor = Theme.TextSecondary
        };
        var labelBounds = label.GetLocalBounds();
        label.Position = new Vector2f(centerX - labelBounds.Size.X / 2, y - _game.Scale.S(25f));
        window.Draw(label);

        if (cards.Count == 0) return;

        // Calculate total width to center cards
        float totalWidth = _zoneRenderer.GetHorizontalZoneWidth(cards.Count, ZoneRenderer.TableScale);
        float startX = centerX - totalWidth / 2;

        _zoneRenderer.DrawTable(window, cards, new Vector2f(startX, y));
    }

    private void DrawHandZone(RenderWindow window)
    {
        // Bottom center
        float centerX = _game.Scale.CurrentWidth / 2;
        float y = _game.Scale.CurrentHeight - _game.Scale.S(280f);

        var cards = State.Hand.Cards.ToList();

        // Label
        var label = new Text(_game.Assets.DefaultFont, "Ręka", _game.Scale.S(Theme.FontSizeSmall))
        {
            FillColor = Theme.TextSecondary
        };
        var labelBounds = label.GetLocalBounds();
        label.Position = new Vector2f(centerX - labelBounds.Size.X / 2, y - _game.Scale.S(25f));
        window.Draw(label);

        if (cards.Count == 0) return;

        // Calculate card positions
        var cardSize = _cardRenderer.GetCardSize(ZoneRenderer.HandScale);
        float spacing = cardSize.X * CardSpacingRatio;
        float totalWidth = cards.Count * cardSize.X + (cards.Count - 1) * spacing;
        float startX = centerX - totalWidth / 2;

        // Draw cards with highlight for hovered one
        for (int i = 0; i < cards.Count; i++)
        {
            float cardX = startX + i * (cardSize.X + spacing);
            float cardY = y;
            float scale = ZoneRenderer.HandScale;

            // Lift and slightly enlarge hovered card
            if (i == _hoveredHandIndex)
            {
                cardY -= _game.Scale.S(15f);
                scale = 1.05f;

                // Draw highlight glow
                var glowRect = new RectangleShape(_cardRenderer.GetCardSize(scale) + new Vector2f(_game.Scale.S(6f), _game.Scale.S(6f)))
                {
                    Position = new Vector2f(cardX - _game.Scale.S(3f), cardY - _game.Scale.S(3f)),
                    FillColor = new Color(255, 255, 255, 60)
                };
                window.Draw(glowRect);
            }

            _cardRenderer.Draw(window, cards[i], new Vector2f(cardX, cardY), CardDisplayMode.Small, scale);
        }
    }

    private void DrawInfoBar(RenderWindow window)
    {
        var bounds = _infoText.GetLocalBounds();
        _infoText.Position = new Vector2f(
            (_game.Scale.CurrentWidth - bounds.Size.X) / 2,
            _game.Scale.CurrentHeight - _game.Scale.S(30f)
        );
        window.Draw(_infoText);
    }

    private void UpdateLayout()
    {
        // Update font sizes
        _fatLabel.CharacterSize = _game.Scale.S(Theme.FontSizeNormal);
        _willpowerLabel.CharacterSize = _game.Scale.S(Theme.FontSizeNormal);
        _turnLabel.CharacterSize = _game.Scale.S(Theme.FontSizeNormal);
        _infoText.CharacterSize = _game.Scale.S(Theme.FontSizeSmall);
    }
}
