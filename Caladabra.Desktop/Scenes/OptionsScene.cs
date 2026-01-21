using SFML.Graphics;
using SFML.Window;
using SFML.System;
using Caladabra.Desktop.Core;
using Caladabra.Desktop.UI;

namespace Caladabra.Desktop.Scenes;

public sealed class OptionsScene : IScene
{
    private readonly Game _game;

    private Text _titleText = null!;
    private Text _resolutionLabel = null!;
    private Text _resolutionValue = null!;
    private Text _fullscreenLabel = null!;
    private Text _fullscreenValue = null!;
    private Text _infoText = null!;

    private Button _resolutionLeftButton = null!;
    private Button _resolutionRightButton = null!;
    private Button _fullscreenToggleButton = null!;
    private Button _applyButton = null!;
    private Button _backButton = null!;

    private List<Button> _buttons = null!;

    private static readonly (uint Width, uint Height, string Label)[] Resolutions =
    [
        (1280, 720, "1280 x 720"),
        (1600, 900, "1600 x 900"),
        (1920, 1080, "1920 x 1080"),
        (2560, 1440, "2560 x 1440")
    ];

    private int _selectedResolutionIndex;
    private bool _selectedFullscreen;
    private bool _hasChanges;

    private const float RowHeight = 60f;
    private const float LabelWidth = 200f;
    private const float ValueWidth = 200f;
    private const float ArrowButtonSize = 40f;

    public OptionsScene(Game game)
    {
        _game = game;
    }

    public void Enter()
    {
        // Initialize from current settings
        _selectedFullscreen = _game.Settings.Fullscreen;
        _selectedResolutionIndex = FindCurrentResolutionIndex();

        var font = _game.Assets.DefaultFont;
        var scale = _game.Scale;

        // Title
        _titleText = new Text(font, "Opcje", scale.S(Theme.FontSizeTitle))
        {
            FillColor = Theme.TextPrimary
        };

        // Resolution row
        _resolutionLabel = new Text(font, "Rozdzielczość:", scale.S(Theme.FontSizeMedium))
        {
            FillColor = Theme.TextPrimary
        };

        _resolutionValue = new Text(font, GetCurrentResolutionLabel(), scale.S(Theme.FontSizeMedium))
        {
            FillColor = Theme.AccentPrimary
        };

        // Fullscreen row
        _fullscreenLabel = new Text(font, "Pełny ekran:", scale.S(Theme.FontSizeMedium))
        {
            FillColor = Theme.TextPrimary
        };

        _fullscreenValue = new Text(font, _selectedFullscreen ? "Tak" : "Nie", scale.S(Theme.FontSizeMedium))
        {
            FillColor = Theme.AccentPrimary
        };

        // Info text
        _infoText = new Text(font, "ESC = powrót", scale.S(Theme.FontSizeSmall))
        {
            FillColor = Theme.TextMuted
        };

        // Create buttons
        var arrowSize = new Vector2f(scale.S(ArrowButtonSize), scale.S(ArrowButtonSize));
        var wideButtonSize = new Vector2f(scale.S(120f), scale.S(40f));

        _resolutionLeftButton = new Button(font, scale, "<", new Vector2f(0, 0), arrowSize)
        {
            OnClick = OnResolutionLeft
        };

        _resolutionRightButton = new Button(font, scale, ">", new Vector2f(0, 0), arrowSize)
        {
            OnClick = OnResolutionRight
        };

        _fullscreenToggleButton = new Button(font, scale, "Zmień", new Vector2f(0, 0), wideButtonSize)
        {
            OnClick = OnFullscreenToggle
        };

        _applyButton = new Button(font, scale, "Zastosuj", new Vector2f(0, 0), new Vector2f(scale.S(140f), scale.S(45f)))
        {
            OnClick = OnApply,
            IsEnabled = false
        };

        _backButton = new Button(font, scale, "Powrót", new Vector2f(0, 0), new Vector2f(scale.S(140f), scale.S(45f)))
        {
            OnClick = OnBack
        };

        _buttons =
        [
            _resolutionLeftButton,
            _resolutionRightButton,
            _fullscreenToggleButton,
            _applyButton,
            _backButton
        ];

        UpdateLayout();
    }

    public void Exit() { }

    public void HandleEvent(Event sfmlEvent)
    {
        switch (sfmlEvent.Type)
        {
            case EventType.KeyPressed:
                if (sfmlEvent.Key.Code == Keyboard.Key.Escape)
                {
                    OnBack();
                }
                break;

            case EventType.MouseMoved:
                var movePos = new Vector2f(sfmlEvent.MouseMove.Position.X, sfmlEvent.MouseMove.Position.Y);
                foreach (var button in _buttons)
                {
                    button.UpdateHover(movePos);
                }
                break;

            case EventType.MouseButtonPressed:
                if (sfmlEvent.MouseButton.Button == Mouse.Button.Left)
                {
                    var pressPos = new Vector2f(sfmlEvent.MouseButton.Position.X, sfmlEvent.MouseButton.Position.Y);
                    foreach (var button in _buttons)
                    {
                        button.HandlePress(pressPos);
                    }
                }
                break;

            case EventType.MouseButtonReleased:
                if (sfmlEvent.MouseButton.Button == Mouse.Button.Left)
                {
                    var releasePos = new Vector2f(sfmlEvent.MouseButton.Position.X, sfmlEvent.MouseButton.Position.Y);
                    foreach (var button in _buttons)
                    {
                        button.HandleRelease(releasePos);
                    }
                }
                break;

            case EventType.Resized:
                UpdateLayout();
                break;
        }
    }

    public void Update(float deltaTime) { }

    public void Render(RenderWindow window)
    {
        // Semi-transparent overlay
        var overlay = new RectangleShape(new Vector2f(_game.Scale.CurrentWidth, _game.Scale.CurrentHeight))
        {
            FillColor = new Color(0, 0, 0, 200)
        };
        window.Draw(overlay);

        // Panel background
        var scale = _game.Scale;
        var panelWidth = scale.S(500f);
        var panelHeight = scale.S(350f);
        var panelX = (scale.CurrentWidth - panelWidth) / 2f;
        var panelY = (scale.CurrentHeight - panelHeight) / 2f;

        var panel = new RectangleShape(new Vector2f(panelWidth, panelHeight))
        {
            Position = new Vector2f(panelX, panelY),
            FillColor = Theme.BackgroundMedium,
            OutlineColor = Theme.AccentPrimary,
            OutlineThickness = scale.S(2f)
        };
        window.Draw(panel);

        window.Draw(_titleText);
        window.Draw(_resolutionLabel);
        window.Draw(_resolutionValue);
        window.Draw(_fullscreenLabel);
        window.Draw(_fullscreenValue);
        window.Draw(_infoText);

        foreach (var button in _buttons)
        {
            button.Draw(window);
        }
    }

    private void UpdateLayout()
    {
        var scale = _game.Scale;
        var centerX = scale.CurrentWidth / 2f;
        var centerY = scale.CurrentHeight / 2f;

        var panelWidth = scale.S(500f);
        var panelHeight = scale.S(350f);
        var panelX = (scale.CurrentWidth - panelWidth) / 2f;
        var panelY = (scale.CurrentHeight - panelHeight) / 2f;
        var contentX = panelX + scale.S(30f);

        // Update text sizes
        _titleText.CharacterSize = scale.S(Theme.FontSizeTitle);
        _resolutionLabel.CharacterSize = scale.S(Theme.FontSizeMedium);
        _resolutionValue.CharacterSize = scale.S(Theme.FontSizeMedium);
        _fullscreenLabel.CharacterSize = scale.S(Theme.FontSizeMedium);
        _fullscreenValue.CharacterSize = scale.S(Theme.FontSizeMedium);
        _infoText.CharacterSize = scale.S(Theme.FontSizeSmall);

        // Title centered at top of panel
        var titleBounds = _titleText.GetLocalBounds();
        _titleText.Position = new Vector2f(
            centerX - titleBounds.Size.X / 2 - titleBounds.Position.X,
            panelY + scale.S(20f)
        );

        // Resolution row
        float row1Y = panelY + scale.S(90f);
        _resolutionLabel.Position = new Vector2f(contentX, row1Y);

        var arrowSize = new Vector2f(scale.S(ArrowButtonSize), scale.S(ArrowButtonSize));
        float valueAreaX = panelX + panelWidth - scale.S(250f);

        _resolutionLeftButton.Size = arrowSize;
        _resolutionLeftButton.Position = new Vector2f(valueAreaX, row1Y - scale.S(5f));

        _resolutionValue.Position = new Vector2f(
            valueAreaX + arrowSize.X + scale.S(15f),
            row1Y
        );

        var valueBounds = _resolutionValue.GetLocalBounds();
        _resolutionRightButton.Size = arrowSize;
        _resolutionRightButton.Position = new Vector2f(
            valueAreaX + arrowSize.X + scale.S(30f) + valueBounds.Size.X,
            row1Y - scale.S(5f)
        );

        // Fullscreen row
        float row2Y = row1Y + scale.S(RowHeight);
        _fullscreenLabel.Position = new Vector2f(contentX, row2Y);
        _fullscreenValue.Position = new Vector2f(valueAreaX + arrowSize.X + scale.S(15f), row2Y);

        var toggleSize = new Vector2f(scale.S(100f), scale.S(35f));
        _fullscreenToggleButton.Size = toggleSize;
        _fullscreenToggleButton.Position = new Vector2f(
            valueAreaX + arrowSize.X + scale.S(80f),
            row2Y - scale.S(5f)
        );

        // Bottom buttons
        float buttonsY = panelY + panelHeight - scale.S(70f);
        var buttonSize = new Vector2f(scale.S(140f), scale.S(45f));

        _applyButton.Size = buttonSize;
        _applyButton.Position = new Vector2f(
            centerX - buttonSize.X - scale.S(10f),
            buttonsY
        );

        _backButton.Size = buttonSize;
        _backButton.Position = new Vector2f(
            centerX + scale.S(10f),
            buttonsY
        );

        // Info text at bottom
        var infoBounds = _infoText.GetLocalBounds();
        _infoText.Position = new Vector2f(
            centerX - infoBounds.Size.X / 2 - infoBounds.Position.X,
            panelY + panelHeight - scale.S(25f)
        );
    }

    private int FindCurrentResolutionIndex()
    {
        for (int i = 0; i < Resolutions.Length; i++)
        {
            if (Resolutions[i].Width == _game.Settings.ScreenWidth &&
                Resolutions[i].Height == _game.Settings.ScreenHeight)
            {
                return i;
            }
        }
        return 2; // Default to 1920x1080
    }

    private string GetCurrentResolutionLabel()
    {
        return Resolutions[_selectedResolutionIndex].Label;
    }

    private void UpdateChangesState()
    {
        var current = Resolutions[_selectedResolutionIndex];
        _hasChanges = current.Width != _game.Settings.ScreenWidth ||
                      current.Height != _game.Settings.ScreenHeight ||
                      _selectedFullscreen != _game.Settings.Fullscreen;

        _applyButton.IsEnabled = _hasChanges;
    }

    private void OnResolutionLeft()
    {
        _selectedResolutionIndex = Math.Max(0, _selectedResolutionIndex - 1);
        _resolutionValue.DisplayedString = GetCurrentResolutionLabel();
        UpdateChangesState();
        UpdateLayout();
    }

    private void OnResolutionRight()
    {
        _selectedResolutionIndex = Math.Min(Resolutions.Length - 1, _selectedResolutionIndex + 1);
        _resolutionValue.DisplayedString = GetCurrentResolutionLabel();
        UpdateChangesState();
        UpdateLayout();
    }

    private void OnFullscreenToggle()
    {
        _selectedFullscreen = !_selectedFullscreen;
        _fullscreenValue.DisplayedString = _selectedFullscreen ? "Tak" : "Nie";
        UpdateChangesState();
    }

    private void OnApply()
    {
        if (!_hasChanges) return;

        var resolution = Resolutions[_selectedResolutionIndex];
        _game.ApplyResolution(resolution.Width, resolution.Height, _selectedFullscreen);

        // Scene needs to be re-entered after resolution change
        _hasChanges = false;
        _applyButton.IsEnabled = false;
        UpdateLayout();
    }

    private void OnBack()
    {
        _game.SceneManager.PopScene();
    }
}
