using SFML.Graphics;
using SFML.Window;
using SFML.System;
using Caladabra.Desktop.Core;
using Caladabra.Desktop.UI;
using Caladabra.Desktop.Integration;

namespace Caladabra.Desktop.Scenes;

public sealed class EnterSeedScene : IScene
{
    private readonly Game _game;

    private Text _titleText = null!;
    private Text _infoText = null!;
    private TextInput _seedInput = null!;

    private Button _startButton = null!;
    private Button _randomButton = null!;
    private Button _cancelButton = null!;

    private List<Button> _buttons = null!;

    public EnterSeedScene(Game game)
    {
        _game = game;
    }

    public void Enter()
    {
        var font = _game.Assets.DefaultFont;
        var scale = _game.Scale;

        // Tytuł
        _titleText = new Text(font, "Wprowadź Ziarno", scale.S(Theme.FontSizeTitle))
        {
            FillColor = Theme.TextPrimary
        };

        // Info
        _infoText = new Text(font, "Wpisz liczbę lub zostaw puste dla losowego", scale.S(Theme.FontSizeSmall))
        {
            FillColor = Theme.TextMuted
        };

        // TextInput
        _seedInput = new TextInput(font, scale, new Vector2f(0, 0), new Vector2f(scale.S(300f), scale.S(45f)))
        {
            Placeholder = "np. 1990733316",
            MaxLength = 10,
            DigitsOnly = true,
            OnSubmit = _ => OnStart()
        };

        // Przyciski
        var buttonSize = new Vector2f(scale.S(120f), scale.S(40f));

        _startButton = new Button(font, scale, "Rozpocznij", new Vector2f(0, 0), buttonSize)
        {
            OnClick = OnStart
        };

        _randomButton = new Button(font, scale, "Losowe", new Vector2f(0, 0), buttonSize)
        {
            OnClick = OnRandom
        };

        _cancelButton = new Button(font, scale, "Anuluj", new Vector2f(0, 0), buttonSize)
        {
            OnClick = OnCancel
        };

        _buttons = [_startButton, _randomButton, _cancelButton];

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
                    OnCancel();
                }
                else
                {
                    _seedInput.HandleKeyPressed(sfmlEvent.Key.Code);
                }
                break;

            case EventType.TextEntered:
                _seedInput.HandleTextEntered(sfmlEvent.Text.Unicode);
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

    public void Update(float deltaTime)
    {
        _seedInput.Update(deltaTime);
    }

    public void Render(RenderWindow window)
    {
        // Półprzezroczyste tło
        var overlay = new RectangleShape(new Vector2f(_game.Scale.CurrentWidth, _game.Scale.CurrentHeight))
        {
            FillColor = new Color(0, 0, 0, 200)
        };
        window.Draw(overlay);

        // Panel
        var scale = _game.Scale;
        var panelWidth = scale.S(450f);
        var panelHeight = scale.S(250f);
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
        window.Draw(_infoText);
        _seedInput.Draw(window);

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

        var panelWidth = scale.S(450f);
        var panelHeight = scale.S(250f);
        var panelX = (scale.CurrentWidth - panelWidth) / 2f;
        var panelY = (scale.CurrentHeight - panelHeight) / 2f;

        // Tytuł
        _titleText.CharacterSize = scale.S(Theme.FontSizeTitle);
        var titleBounds = _titleText.GetLocalBounds();
        _titleText.Position = new Vector2f(
            centerX - titleBounds.Size.X / 2 - titleBounds.Position.X,
            panelY + scale.S(20f)
        );

        // Info pod tytułem
        _infoText.CharacterSize = scale.S(Theme.FontSizeSmall);
        var infoBounds = _infoText.GetLocalBounds();
        _infoText.Position = new Vector2f(
            centerX - infoBounds.Size.X / 2 - infoBounds.Position.X,
            panelY + scale.S(60f)
        );

        // TextInput
        var inputWidth = scale.S(300f);
        var inputHeight = scale.S(45f);
        _seedInput.Size = new Vector2f(inputWidth, inputHeight);
        _seedInput.Position = new Vector2f(
            centerX - inputWidth / 2f,
            panelY + scale.S(100f)
        );

        // Przyciski w rzędzie
        var buttonWidth = scale.S(120f);
        var buttonHeight = scale.S(40f);
        var buttonSpacing = scale.S(15f);
        var totalButtonsWidth = 3 * buttonWidth + 2 * buttonSpacing;
        var buttonsStartX = centerX - totalButtonsWidth / 2f;
        var buttonsY = panelY + scale.S(170f);

        var buttonSize = new Vector2f(buttonWidth, buttonHeight);

        _startButton.Size = buttonSize;
        _startButton.Position = new Vector2f(buttonsStartX, buttonsY);

        _randomButton.Size = buttonSize;
        _randomButton.Position = new Vector2f(buttonsStartX + buttonWidth + buttonSpacing, buttonsY);

        _cancelButton.Size = buttonSize;
        _cancelButton.Position = new Vector2f(buttonsStartX + 2 * (buttonWidth + buttonSpacing), buttonsY);
    }

    private void OnStart()
    {
        int? seed = null;

        if (!string.IsNullOrEmpty(_seedInput.Text))
        {
            if (int.TryParse(_seedInput.Text, out var parsedSeed))
            {
                seed = parsedSeed;
            }
        }

        StartGame(seed);
    }

    private void OnRandom()
    {
        StartGame(null);
    }

    private void OnCancel()
    {
        _game.SceneManager.PopScene();
    }

    private void StartGame(int? seed)
    {
        var gameController = GameController.NewGame(seed);
        // Pop this scene and replace MainMenu with GameScene
        _game.SceneManager.PopScene();
        _game.SceneManager.ReplaceScene(new GameScene(_game, gameController));
    }

    public void OnResolutionChanged() => UpdateLayout();
}
