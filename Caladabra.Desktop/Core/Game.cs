using SFML.Graphics;
using SFML.Window;
using SFML.System;
using Caladabra.Core.Cards;
using Caladabra.Core.Cards.Definitions;
using Caladabra.Desktop.Scenes;
using Caladabra.Desktop.Rendering;
using Caladabra.Desktop.UI;
using Caladabra.Desktop.Integration;
using System.Linq;

namespace Caladabra.Desktop.Core;

public sealed class Game
{
    private RenderWindow _window = null!;
    private readonly SceneManager _sceneManager = new();
    private readonly AssetManager _assets = new();
    private readonly ScaleManager _scale = new();
    private readonly GameSettings _settings;
    private readonly Clock _clock = new();

    public SceneManager SceneManager => _sceneManager;
    public AssetManager Assets => _assets;
    public ScaleManager Scale => _scale;
    public GameSettings Settings => _settings;
    public RenderWindow Window => _window;

    public Game()
    {
        _settings = GameSettings.Load();
    }

    public void Run()
    {
        InitializeWindow();
        _assets.PreloadAssets();
        _scale.UpdateScale(_settings.ScreenWidth, _settings.ScreenHeight);

        // Initialize card definitions
        CardDefinitions.RegisterAll();

        // Start with a new game scene (will be replaced with MainMenuScene later)
        var gameController = GameController.NewGame();
        _sceneManager.PushScene(new GameScene(this, gameController));

        while (_window.IsOpen)
        {
            float deltaTime = _clock.Restart().AsSeconds();

            _window.DispatchEvents();
            Update(deltaTime);
            Render();
        }

        Cleanup();
    }

    private void InitializeWindow()
    {
        var mode = new VideoMode(new Vector2u(_settings.ScreenWidth, _settings.ScreenHeight));
        var style = Styles.Default;
        var state = _settings.Fullscreen ? State.Fullscreen : State.Windowed;

        _window = new RenderWindow(mode, "Caladabra", style, state);
        _window.SetFramerateLimit(60);

        // Set up event handlers
        _window.Closed += OnWindowClosed;
        _window.Resized += OnWindowResized;
        _window.KeyPressed += OnKeyPressed;
        _window.KeyReleased += OnKeyReleased;
        _window.MouseButtonPressed += OnMouseButtonPressed;
        _window.MouseButtonReleased += OnMouseButtonReleased;
        _window.MouseMoved += OnMouseMoved;
        _window.MouseWheelScrolled += OnMouseWheelScrolled;
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        _window.Close();
    }

    private void OnWindowResized(object? sender, SizeEventArgs e)
    {
        _scale.UpdateScale(e.Size.X, e.Size.Y);
        var view = new View(new FloatRect(new Vector2f(0, 0), new Vector2f(e.Size.X, e.Size.Y)));
        _window.SetView(view);
    }

    private void OnKeyPressed(object? sender, KeyEventArgs e)
    {
        var sfmlEvent = new Event { Type = EventType.KeyPressed, Key = new KeyEvent { Code = e.Code } };
        _sceneManager.HandleEvent(sfmlEvent);
    }

    private void OnKeyReleased(object? sender, KeyEventArgs e)
    {
        var sfmlEvent = new Event { Type = EventType.KeyReleased, Key = new KeyEvent { Code = e.Code } };
        _sceneManager.HandleEvent(sfmlEvent);
    }

    private void OnMouseButtonPressed(object? sender, MouseButtonEventArgs e)
    {
        var sfmlEvent = new Event
        {
            Type = EventType.MouseButtonPressed,
            MouseButton = new MouseButtonEvent { Button = e.Button, Position = e.Position }
        };
        _sceneManager.HandleEvent(sfmlEvent);
    }

    private void OnMouseButtonReleased(object? sender, MouseButtonEventArgs e)
    {
        var sfmlEvent = new Event
        {
            Type = EventType.MouseButtonReleased,
            MouseButton = new MouseButtonEvent { Button = e.Button, Position = e.Position }
        };
        _sceneManager.HandleEvent(sfmlEvent);
    }

    private void OnMouseMoved(object? sender, MouseMoveEventArgs e)
    {
        var sfmlEvent = new Event
        {
            Type = EventType.MouseMoved,
            MouseMove = new MouseMoveEvent { Position = e.Position }
        };
        _sceneManager.HandleEvent(sfmlEvent);
    }

    private void OnMouseWheelScrolled(object? sender, MouseWheelScrollEventArgs e)
    {
        var sfmlEvent = new Event
        {
            Type = EventType.MouseWheelScrolled,
            MouseWheelScroll = new MouseWheelScrollEvent { Delta = e.Delta, Wheel = e.Wheel, Position = e.Position }
        };
        _sceneManager.HandleEvent(sfmlEvent);
    }

    private void Update(float deltaTime)
    {
        _sceneManager.Update(deltaTime);
    }

    private void Render()
    {
        _window.Clear(Theme.BackgroundDark);
        _sceneManager.Render(_window);
        _window.Display();
    }

    private void Cleanup()
    {
        _assets.Dispose();
        _window.Dispose();
        _clock.Dispose();
    }

    public void ApplyResolution(uint width, uint height, bool fullscreen)
    {
        _settings.ScreenWidth = width;
        _settings.ScreenHeight = height;
        _settings.Fullscreen = fullscreen;
        _settings.Save();

        // Close current window and recreate
        _window.Closed -= OnWindowClosed;
        _window.Resized -= OnWindowResized;
        _window.KeyPressed -= OnKeyPressed;
        _window.KeyReleased -= OnKeyReleased;
        _window.MouseButtonPressed -= OnMouseButtonPressed;
        _window.MouseButtonReleased -= OnMouseButtonReleased;
        _window.MouseMoved -= OnMouseMoved;
        _window.MouseWheelScrolled -= OnMouseWheelScrolled;
        _window.Close();
        _window.Dispose();

        InitializeWindow();
        _scale.UpdateScale(width, height);
    }
}

// Temporary placeholder scene for testing card rendering
internal sealed class PlaceholderScene : IScene
{
    private readonly Game _game;
    private Text _titleText = null!;
    private Text _infoText = null!;
    private CardRenderer _cardRenderer = null!;
    private List<Card> _testCards = null!;

    public PlaceholderScene(Game game)
    {
        _game = game;
    }

    public void Enter()
    {
        var font = _game.Assets.DefaultFont;

        _titleText = new Text(font, "Caladabra - Card Display Modes", _game.Scale.S(Theme.FontSizeTitle))
        {
            FillColor = Theme.TextPrimary
        };

        _infoText = new Text(font, "Full (1.5x) | Small (1.0x) | Tiny (0.5x) | Back (0.6x) | Press ESC to exit", _game.Scale.S(Theme.FontSizeNormal))
        {
            FillColor = Theme.TextSecondary
        };

        _cardRenderer = new CardRenderer(font, _game.Scale);

        // Get test cards - one of each flavor
        _testCards = CardRegistry.Instance.GetAll()
            .GroupBy(c => c.Flavor)
            .Select(g => g.First())
            .Take(6)
            .ToList();

        UpdatePositions();
    }

    public void Exit() { }

    public void HandleEvent(Event sfmlEvent)
    {
        if (sfmlEvent.Type == EventType.KeyPressed && sfmlEvent.Key.Code == Keyboard.Key.Escape)
        {
            _game.Window.Close();
        }

        if (sfmlEvent.Type == EventType.Resized)
        {
            UpdatePositions();
        }
    }

    public void Update(float deltaTime) { }

    public void Render(RenderWindow window)
    {
        window.Draw(_titleText);
        window.Draw(_infoText);

        if (_testCards.Count == 0) return;

        var card = _testCards[0];  // Użyj pierwszej karty do demonstracji trybów
        float baseY = _game.Scale.S(120f);

        // Rząd 1: Full mode (1.5x) - podgląd z instrukcją
        float fullScale = 1.5f;
        var fullSize = _cardRenderer.GetCardSize(fullScale);
        float fullX = _game.Scale.S(50f);
        float fullY = baseY;
        _cardRenderer.Draw(window, card, new Vector2f(fullX, fullY), CardDisplayMode.Full, fullScale);
        DrawLabel(window, "Full (Preview)", fullX, fullY - _game.Scale.S(25f));

        // Rząd 1: Small mode (1.0x) - ręka/stół
        float smallScale = 1.0f;
        var smallSize = _cardRenderer.GetCardSize(smallScale);
        float smallX = fullX + fullSize.X + _game.Scale.S(40f);
        float smallY = baseY;
        _cardRenderer.Draw(window, card, new Vector2f(smallX, smallY), CardDisplayMode.Small, smallScale);
        DrawLabel(window, "Small (Hand/Table)", smallX, smallY - _game.Scale.S(25f));

        // Rząd 1: Tiny mode (0.5x) - żołądek
        float tinyScale = 0.5f;
        var tinySize = _cardRenderer.GetCardSize(tinyScale);
        float tinyX = smallX + smallSize.X + _game.Scale.S(40f);
        float tinyY = baseY;
        _cardRenderer.Draw(window, card, new Vector2f(tinyX, tinyY), CardDisplayMode.Tiny, tinyScale);
        DrawLabel(window, "Tiny (Stomach)", tinyX, tinyY - _game.Scale.S(25f));

        // Rząd 1: Back mode (0.6x) - spiżarnia
        float backScale = 0.6f;
        var backSize = _cardRenderer.GetCardSize(backScale);
        float backX = tinyX + tinySize.X + _game.Scale.S(40f);
        float backY = baseY;
        _cardRenderer.Draw(window, card, new Vector2f(backX, backY), CardDisplayMode.Back, backScale);
        DrawLabel(window, "Back (Pantry)", backX, backY - _game.Scale.S(25f));

        // Rząd 2: Wszystkie smaki w trybie Small
        float row2Y = baseY + fullSize.Y + _game.Scale.S(60f);
        float row2X = _game.Scale.S(50f);
        float cardSpacing = _game.Scale.S(20f);

        DrawLabel(window, "All Flavors (Small mode):", row2X, row2Y - _game.Scale.S(25f));

        for (int i = 0; i < _testCards.Count; i++)
        {
            var pos = new Vector2f(row2X + i * (smallSize.X + cardSpacing), row2Y);
            _cardRenderer.Draw(window, _testCards[i], pos, CardDisplayMode.Small, 1.0f);
        }
    }

    private void DrawLabel(RenderWindow window, string text, float x, float y)
    {
        var label = new Text(_game.Assets.DefaultFont, text, _game.Scale.S(14u))
        {
            FillColor = Theme.TextSecondary,
            Position = new Vector2f(x, y)
        };
        window.Draw(label);
    }

    private void UpdatePositions()
    {
        // Update text sizes
        _titleText.CharacterSize = _game.Scale.S(Theme.FontSizeTitle);
        _infoText.CharacterSize = _game.Scale.S(Theme.FontSizeNormal);

        // Title at top center
        var titleBounds = _titleText.GetLocalBounds();
        _titleText.Position = new Vector2f(
            (_game.Scale.CurrentWidth - titleBounds.Size.X) / 2,
            _game.Scale.S(20f)
        );

        // Info below title
        var infoBounds = _infoText.GetLocalBounds();
        _infoText.Position = new Vector2f(
            (_game.Scale.CurrentWidth - infoBounds.Size.X) / 2,
            _game.Scale.S(55f)
        );
    }
}
