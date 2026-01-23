using SFML.Graphics;
using SFML.Window;
using SFML.System;
using Caladabra.Desktop.Scenes;
using Caladabra.Desktop.UI;

namespace Caladabra.Desktop.Core;

public sealed class Game
{
    private RenderWindow _window = null!;
    private readonly SceneManager _sceneManager = new();
    private readonly AssetManager _assets = new();
    private readonly ScaleManager _scale = new();
    private readonly GameSettings _settings;
    private readonly Clock _clock = new();

    // Pending resolution change (applied at end of game loop to avoid disposed window issues)
    private bool _pendingResolutionChange;
    private uint _pendingWidth, _pendingHeight;
    private bool _pendingFullscreen;

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

        // Start with the main menu
        _sceneManager.PushScene(new MainMenuScene(this));

        while (_window.IsOpen)
        {
            float deltaTime = _clock.Restart().AsSeconds();

            _window.DispatchEvents();
            Update(deltaTime);
            Render();
            ApplyPendingResolution();
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
        _window.TextEntered += OnTextEntered;
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

    private void OnTextEntered(object? sender, TextEventArgs e)
    {
        // e.Unicode is a string in SFML.Net, convert first char to uint
        if (string.IsNullOrEmpty(e.Unicode)) return;

        var sfmlEvent = new Event
        {
            Type = EventType.TextEntered,
            Text = new TextEvent { Unicode = e.Unicode[0] }
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
        // Schedule resolution change for end of game loop (avoid disposed window in DispatchEvents)
        _pendingWidth = width;
        _pendingHeight = height;
        _pendingFullscreen = fullscreen;
        _pendingResolutionChange = true;
    }

    private void ApplyPendingResolution()
    {
        if (!_pendingResolutionChange) return;
        _pendingResolutionChange = false;

        _settings.ScreenWidth = _pendingWidth;
        _settings.ScreenHeight = _pendingHeight;
        _settings.Fullscreen = _pendingFullscreen;
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
        _window.TextEntered -= OnTextEntered;
        _window.Close();
        _window.Dispose();

        InitializeWindow();
        _scale.UpdateScale(_pendingWidth, _pendingHeight);

        // Notify current scene about resolution change
        _sceneManager.CurrentScene?.OnResolutionChanged();
    }
}
