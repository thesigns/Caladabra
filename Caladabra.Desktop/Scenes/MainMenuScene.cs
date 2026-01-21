using SFML.Graphics;
using SFML.Window;
using SFML.System;
using Caladabra.Desktop.Core;
using Caladabra.Desktop.UI;
using Caladabra.Desktop.Integration;
using Caladabra.Core.Cards.Definitions;

namespace Caladabra.Desktop.Scenes;

public sealed class MainMenuScene : IScene
{
    private readonly Game _game;

    private Text _titleText = null!;
    private Text _subtitleText = null!;
    private Text _versionText = null!;

    private Button _newGameButton = null!;
    private Button _newGameSeedButton = null!;
    private Button _continueButton = null!;
    private Button _optionsButton = null!;
    private Button _exitButton = null!;

    private List<Button> _buttons = null!;

    private const float ButtonWidth = 280f;
    private const float ButtonHeight = 50f;
    private const float ButtonSpacing = 15f;

    public MainMenuScene(Game game)
    {
        _game = game;
    }

    public void Enter()
    {
        CardDefinitions.RegisterAll();

        var font = _game.Assets.DefaultFont;
        var scale = _game.Scale;

        // Title
        _titleText = new Text(font, "Caladabra", scale.S(64u))
        {
            FillColor = Theme.AccentPrimary,
            Style = Text.Styles.Bold
        };

        // Subtitle
        _subtitleText = new Text(font, "Willpower is Magic", scale.S(Theme.FontSizeMedium))
        {
            FillColor = Theme.TextSecondary,
            Style = Text.Styles.Italic
        };

        // Version
        _versionText = new Text(font, "v0.4 - Desktop Preview", scale.S(Theme.FontSizeSmall))
        {
            FillColor = Theme.TextMuted
        };

        // Create buttons
        var buttonSize = new Vector2f(scale.S(ButtonWidth), scale.S(ButtonHeight));

        _newGameButton = new Button(font, scale, "Nowa Gra", new Vector2f(0, 0), buttonSize)
        {
            OnClick = OnNewGame
        };

        _newGameSeedButton = new Button(font, scale, "Nowa Gra (Ziarno)", new Vector2f(0, 0), buttonSize)
        {
            OnClick = OnNewGameSeed
        };

        _continueButton = new Button(font, scale, "Kontynuuj", new Vector2f(0, 0), buttonSize)
        {
            OnClick = OnContinue,
            IsEnabled = HasSavedGame()
        };

        _optionsButton = new Button(font, scale, "Opcje", new Vector2f(0, 0), buttonSize)
        {
            OnClick = OnOptions
        };

        _exitButton = new Button(font, scale, "Wyjście", new Vector2f(0, 0), buttonSize)
        {
            OnClick = OnExit
        };

        _buttons = [_newGameButton, _newGameSeedButton, _continueButton, _optionsButton, _exitButton];

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
                    _game.Window.Close();
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
        window.Draw(_titleText);
        window.Draw(_subtitleText);
        window.Draw(_versionText);

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

        // Update text sizes
        _titleText.CharacterSize = scale.S(64u);
        _subtitleText.CharacterSize = scale.S(Theme.FontSizeMedium);
        _versionText.CharacterSize = scale.S(Theme.FontSizeSmall);

        // Position title
        var titleBounds = _titleText.GetLocalBounds();
        _titleText.Position = new Vector2f(
            centerX - titleBounds.Size.X / 2 - titleBounds.Position.X,
            scale.S(100f)
        );

        // Position subtitle below title
        var subtitleBounds = _subtitleText.GetLocalBounds();
        _subtitleText.Position = new Vector2f(
            centerX - subtitleBounds.Size.X / 2 - subtitleBounds.Position.X,
            _titleText.Position.Y + titleBounds.Size.Y + scale.S(15f)
        );

        // Position version at bottom
        var versionBounds = _versionText.GetLocalBounds();
        _versionText.Position = new Vector2f(
            centerX - versionBounds.Size.X / 2 - versionBounds.Position.X,
            scale.CurrentHeight - scale.S(40f)
        );

        // Position buttons in center
        var buttonSize = new Vector2f(scale.S(ButtonWidth), scale.S(ButtonHeight));
        var totalButtonsHeight = _buttons.Count * buttonSize.Y + (_buttons.Count - 1) * scale.S(ButtonSpacing);
        var startY = centerY - totalButtonsHeight / 2f + scale.S(50f); // Offset down a bit

        for (int i = 0; i < _buttons.Count; i++)
        {
            var button = _buttons[i];
            button.Size = buttonSize;
            button.Position = new Vector2f(
                centerX - buttonSize.X / 2f,
                startY + i * (buttonSize.Y + scale.S(ButtonSpacing))
            );
        }
    }

    private bool HasSavedGame()
    {
        return File.Exists("game.json");
    }

    private void OnNewGame()
    {
        var gameController = GameController.NewGame();
        _game.SceneManager.ReplaceScene(new GameScene(_game, gameController));
    }

    private void OnNewGameSeed()
    {
        _game.SceneManager.PushScene(new EnterSeedScene(_game));
    }

    private void OnContinue()
    {
        // TODO: Load saved game from game.json
        var gameController = GameController.NewGame();
        _game.SceneManager.ReplaceScene(new GameScene(_game, gameController));
    }

    private void OnOptions()
    {
        _game.SceneManager.PushScene(new OptionsScene(_game));
    }

    private void OnExit()
    {
        _game.Window.Close();
    }
}
