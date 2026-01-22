using SFML.Graphics;
using SFML.Window;
using SFML.System;
using Caladabra.Core.Cards;
using Caladabra.Core.Engine;
using Caladabra.Core.Events;
using Caladabra.Core.State;
using Caladabra.Core.Zones;
using Caladabra.Desktop.Animation;
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
    private int _hoveredTableIndex = -1;
    private int _hoveredStomachIndex = -1;
    private int _hoveredChoiceIndex = -1;
    private Vector2i _mousePosition;

    // Error message display
    private string? _errorMessage;
    private float _errorMessageTimer = 0f;
    private const float ErrorMessageDuration = 2.0f;  // 2 sekundy

    // Lista Kart button
    private FloatRect _cardListButtonRect;
    private bool _cardListSceneOpen = false;

    // Animation system
    private AnimationManager _animationManager = null!;

    // Elevation system - animowane wysuwanie grywalnych kart
    private readonly Dictionary<string, float> _cardElevation = new();
    private const float ElevationRatio = 0.20f;    // 20% wysokości karty
    private const float ElevationSpeed = 8f;       // Szybkość animacji (jednostek/s)

    // Layout constants (bazowe dla 1920x1080)
    private const float PaddingVertical = 20f;
    private const float PaddingHorizontal = 35f;  // większy margines z boków
    private const float StatusTextHeight = 40f;   // wysokość obszaru ze statystykami na górze
    private const float PreviewPanelWidth = 300f;
    private const float PreviewScale = 2.3f;  // duży podgląd
    private const float SidePanelWidth = 180f;
    private const float HandYOffset = 280f;  // odległość ręki od dołu ekranu

    // Card spacing ratio (must match ZoneRenderer)
    private const float CardSpacingRatio = 0.15f;
    private const float StomachCardSpacing = 8f;  // odstęp między kartami żołądka (piksele)

    public GameScene(Game game, GameController controller)
    {
        _game = game;
        _controller = controller;
    }

    public void Enter()
    {
        var font = _game.Assets.DefaultFont;

        _cardRenderer = new CardRenderer(font, _game.Scale, _game.Assets);
        _zoneRenderer = new ZoneRenderer(_cardRenderer, font, _game.Scale);
        _animationManager = new AnimationManager();

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

        _infoText = new Text(font, "ESC = wyjście | LPM = zagraj | PPM = zjedz", _game.Scale.S(Theme.FontSizeSmall))
        {
            FillColor = Theme.TextSecondary
        };

        UpdateLayout();
    }

    public void Exit()
    {
        // Nie dispose clocks - scena może być wznowiona (overlay)
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

        // LPM = zagraj kartę
        if (sfmlEvent.Type == EventType.MouseButtonPressed && sfmlEvent.MouseButton.Button == Mouse.Button.Left)
        {
            HandleLeftClick();
        }

        // PPM = zjedz kartę
        if (sfmlEvent.Type == EventType.MouseButtonPressed && sfmlEvent.MouseButton.Button == Mouse.Button.Right)
        {
            HandleRightClick();
        }
    }

    private void HandleLeftClick()
    {
        // Blokada interakcji podczas animacji
        if (_animationManager.IsAnimating) return;

        // Gra skończona - nie reaguj na kliknięcia (z wyjątkiem przeglądania)
        if (_controller.IsGameOver)
        {
            // Pozwól tylko na przycisk "Lista Kart"
            if (_cardListButtonRect.Contains(new Vector2f(_mousePosition.X, _mousePosition.Y)))
            {
                OpenCardListScene(CardListMode.Browse);
            }
            return;
        }

        // Check "Lista Kart" button click
        if (_cardListButtonRect.Contains(new Vector2f(_mousePosition.X, _mousePosition.Y)))
        {
            OpenCardListScene(CardListMode.Browse);
            return;
        }

        // If awaiting CardList/Toilet/Pantry choice, open CardListScene (handled in Update, but also allow manual click)
        if (_controller.IsAwaitingChoice && State.PendingChoice != null)
        {
            var choiceType = State.PendingChoice.Type;
            if (choiceType == ChoiceType.SelectFromCardList ||
                choiceType == ChoiceType.SelectFromCardListFiltered ||
                choiceType == ChoiceType.SelectFromToilet ||
                choiceType == ChoiceType.SelectFromPantry)
            {
                OpenCardListScene(CardListMode.Select, State.PendingChoice.FlavorFilter);
                return;
            }
        }

        // If awaiting choice, handle choice selection
        if (_controller.IsAwaitingChoice && _hoveredChoiceIndex >= 0)
        {
            _controller.MakeChoice(_hoveredChoiceIndex);
            _hoveredChoiceIndex = -1;
            return;
        }

        // Zagraj kartę z ręki (single-click LPM)
        if (_hoveredHandIndex >= 0)
        {
            // Sprawdź czy mamy dość SW
            if (!_controller.CanPlayCard(_hoveredHandIndex))
            {
                ShowErrorMessage("Masz za mało Siły Woli żeby zagrać tę kartę");
                return;
            }

            if (_controller.PlayCard(_hoveredHandIndex))
            {
                _hoveredCard = null;
                _hoveredHandIndex = -1;
            }
        }
    }

    private void HandleRightClick()
    {
        // Blokada interakcji podczas animacji
        if (_animationManager.IsAnimating) return;

        // Gra skończona - nie reaguj na kliknięcia
        if (_controller.IsGameOver) return;

        // Zjedz kartę z ręki (single-click PPM)
        if (_hoveredHandIndex >= 0 && !_controller.IsAwaitingChoice)
        {
            if (_controller.EatCard(_hoveredHandIndex))
            {
                _hoveredCard = null;
                _hoveredHandIndex = -1;
            }
        }
    }

    private void ShowErrorMessage(string message)
    {
        _errorMessage = message;
        _errorMessageTimer = ErrorMessageDuration;
    }

    public void Update(float deltaTime)
    {
        // Process game events and trigger animations
        ProcessGameEvents();

        // Update animations
        _animationManager.Update(deltaTime);

        // Update card elevation (playable cards raised)
        UpdateCardElevations(deltaTime);

        // Update error message timer
        if (_errorMessageTimer > 0)
        {
            _errorMessageTimer -= deltaTime;
        }

        // Auto-open CardListScene for CardList/Toilet/Pantry choices
        if (_controller.IsAwaitingChoice && State.PendingChoice != null && !_cardListSceneOpen)
        {
            var choiceType = State.PendingChoice.Type;
            if (choiceType == ChoiceType.SelectFromCardList ||
                choiceType == ChoiceType.SelectFromCardListFiltered ||
                choiceType == ChoiceType.SelectFromToilet ||
                choiceType == ChoiceType.SelectFromPantry)
            {
                OpenCardListScene(CardListMode.Select, State.PendingChoice.FlavorFilter);
            }
        }

        // Update status labels
        _fatLabel.DisplayedString = $"Tłuszcz: {State.Fat}";
        _willpowerLabel.DisplayedString = $"Siła Woli: {State.Willpower}/{GameRules.MaxWillpower}";
        _turnLabel.DisplayedString = $"Tura: {State.Turn}/{GameRules.MaxTurns}";

        // Update info text based on game state
        if (_controller.IsGameOver)
        {
            _infoText.DisplayedString = _controller.GetGameOverMessage();
            _infoText.FillColor = State.Phase == GamePhase.Won ? new Color(100, 200, 100) : new Color(200, 100, 100);
        }
        else if (_controller.IsAwaitingChoice && State.PendingChoice != null)
        {
            _infoText.DisplayedString = State.PendingChoice.Prompt;
            _infoText.FillColor = new Color(255, 200, 100);
        }
        else
        {
            // Sprawdź czy gracz może zagrać jakąkolwiek kartę
            bool canPlayAny = State.Hand.Cards.Any(c => State.Willpower >= c.WillpowerCost);

            if (canPlayAny)
            {
                _infoText.DisplayedString = "Zagraj (LPM) lub zjedz (PPM) kartę z ręki.";
                _infoText.FillColor = new Color(255, 200, 100);  // Żółty jak reszta
            }
            else
            {
                _infoText.DisplayedString = "Nie masz dość siły woli! Zjedz (PPM) kartę z ręki.";
                _infoText.FillColor = new Color(255, 150, 100);
            }
        }
    }

    private void UpdateCardElevations(float deltaTime)
    {
        if (_controller.IsGameOver) return;

        var handCards = State.Hand.Cards.ToList();
        var handCardIds = new HashSet<string>(handCards.Select(c => c.Id));

        foreach (var card in handCards)
        {
            bool isPlayable = State.Willpower >= card.WillpowerCost;
            float target = isPlayable ? 1f : 0f;

            if (!_cardElevation.TryGetValue(card.Id, out var current))
            {
                // Nowa karta - ustaw natychmiast jeśli nie ma animacji,
                // lub startuj od 0 dla kart dobranych w trakcie gry
                current = _animationManager.IsAnimating ? 0f : target;
            }

            // Płynna interpolacja w kierunku celu
            if (current < target)
                current = Math.Min(current + deltaTime * ElevationSpeed, target);
            else if (current > target)
                current = Math.Max(current - deltaTime * ElevationSpeed, target);

            _cardElevation[card.Id] = current;
        }

        // Usuń karty które nie są już w ręce
        var toRemove = _cardElevation.Keys.Where(id => !handCardIds.Contains(id)).ToList();
        foreach (var id in toRemove)
            _cardElevation.Remove(id);
    }

    private void UpdateHoveredCard()
    {
        _hoveredCard = null;
        _hoveredHandIndex = -1;
        _hoveredTableIndex = -1;
        _hoveredStomachIndex = -1;
        _hoveredChoiceIndex = -1;

        // If awaiting choice, check choice options first
        if (_controller.IsAwaitingChoice && State.PendingChoice != null)
        {
            UpdateHoveredChoice();
            if (_hoveredChoiceIndex >= 0) return;
        }

        // Check Hand cards
        if (CheckHandHover()) return;

        // Check Table cards
        if (CheckTableHover()) return;

        // Check Stomach cards
        if (CheckStomachHover()) return;

        // Check Toilet cards
        CheckToiletHover();
    }

    private bool CheckHandHover()
    {
        var cards = State.Hand.Cards.ToList();
        if (cards.Count == 0) return false;

        var cardSize = _cardRenderer.GetCardSize(ZoneRenderer.HandScale);
        float spacing = cardSize.X * CardSpacingRatio;
        float totalWidth = cards.Count * cardSize.X + (cards.Count - 1) * spacing;

        float centerX = _game.Scale.CurrentWidth / 2;
        float startX = centerX - totalWidth / 2;
        float y = _game.Scale.CurrentHeight - _game.Scale.S(280f);

        for (int i = 0; i < cards.Count; i++)
        {
            float cardX = startX + i * (cardSize.X + spacing);
            var cardRect = new FloatRect(new Vector2f(cardX, y), cardSize);

            if (cardRect.Contains(new Vector2f(_mousePosition.X, _mousePosition.Y)))
            {
                _hoveredCard = cards[i];
                _hoveredHandIndex = i;
                return true;
            }
        }
        return false;
    }

    private bool CheckTableHover()
    {
        var cards = State.Table.Cards.ToList();
        if (cards.Count == 0) return false;

        var cardSize = _cardRenderer.GetCardSize(ZoneRenderer.TableScale);
        float spacing = cardSize.X * CardSpacingRatio;
        float totalWidth = cards.Count * cardSize.X + (cards.Count - 1) * spacing;

        float centerX = _game.Scale.CurrentWidth / 2;
        float startX = centerX - totalWidth / 2;
        float y = _game.Scale.S(StatusTextHeight + 100f) + cardSize.Y * 0.5f;

        for (int i = 0; i < cards.Count; i++)
        {
            float cardX = startX + i * (cardSize.X + spacing);
            var cardRect = new FloatRect(new Vector2f(cardX, y), cardSize);

            if (cardRect.Contains(new Vector2f(_mousePosition.X, _mousePosition.Y)))
            {
                _hoveredCard = cards[i];
                _hoveredTableIndex = i;
                return true;
            }
        }
        return false;
    }

    private bool CheckStomachHover()
    {
        var cards = State.Stomach.Cards.ToList();
        if (cards.Count == 0) return false;

        float x = _game.Scale.CurrentWidth - _game.Scale.S(SidePanelWidth + PaddingHorizontal);
        float y = _game.Scale.S(StatusTextHeight + PaddingVertical + 25f);
        var cardSize = _cardRenderer.GetCardSize(ZoneRenderer.StomachScale);
        float spacing = _game.Scale.S(StomachCardSpacing);

        // Karty bez nakładania, odwrócona kolejność (najnowsza na górze)
        int maxVisible = Math.Min(cards.Count, 6);
        for (int i = 0; i < maxVisible; i++)
        {
            // i=0 → najnowsza (cards[last]) na górze ekranu
            int cardIndex = cards.Count - 1 - i;
            float cardY = y + i * (cardSize.Y + spacing);
            var cardRect = new FloatRect(new Vector2f(x, cardY), cardSize);

            if (cardRect.Contains(new Vector2f(_mousePosition.X, _mousePosition.Y)))
            {
                _hoveredCard = cards[cardIndex];
                _hoveredStomachIndex = cardIndex;
                return true;
            }
        }
        return false;
    }

    private bool CheckToiletHover()
    {
        var cards = State.Toilet.Cards.ToList();
        if (cards.Count == 0) return false;

        var cardSize = _cardRenderer.GetCardSize(ZoneRenderer.HandScale);
        float x = _game.Scale.CurrentWidth - _game.Scale.S(PaddingHorizontal) - cardSize.X;
        float y = _game.Scale.CurrentHeight - _game.Scale.S(HandYOffset);

        var cardRect = new FloatRect(new Vector2f(x, y), cardSize);
        if (cardRect.Contains(new Vector2f(_mousePosition.X, _mousePosition.Y)))
        {
            _hoveredCard = cards[^1];  // Wierzchnia karta
            return true;
        }
        return false;
    }

    private void UpdateHoveredChoice()
    {
        var choice = State.PendingChoice;
        if (choice == null) return;

        // Determine which zone to check based on choice type
        switch (choice.Type)
        {
            case ChoiceType.SelectFromTable:
                {
                    var cards = State.Table.Cards.ToList();
                    if (cards.Count == 0) return;

                    var cardSize = _cardRenderer.GetCardSize(ZoneRenderer.TableScale);
                    float spacing = cardSize.X * CardSpacingRatio;
                    float totalWidth = cards.Count * cardSize.X + (cards.Count - 1) * spacing;

                    float centerX = _game.Scale.CurrentWidth / 2;
                    float startX = centerX - totalWidth / 2;
                    float y = _game.Scale.S(StatusTextHeight + 100f) + cardSize.Y * 0.5f;

                    for (int i = 0; i < choice.Options.Count; i++)
                    {
                        var option = choice.Options[i];
                        // Find the card in the zone
                        int cardIndex = cards.FindIndex(c => c.Id == option.Card.Id);
                        if (cardIndex < 0) continue;

                        float cardX = startX + cardIndex * (cardSize.X + spacing);
                        var cardRect = new FloatRect(new Vector2f(cardX, y), cardSize);

                        if (cardRect.Contains(new Vector2f(_mousePosition.X, _mousePosition.Y)))
                        {
                            _hoveredCard = option.Card;
                            _hoveredChoiceIndex = option.Index;
                            return;
                        }
                    }
                }
                break;

            case ChoiceType.SelectFromHand:
            case ChoiceType.DiscardFromHand:
                {
                    var cards = State.Hand.Cards.ToList();
                    if (cards.Count == 0) return;

                    var cardSize = _cardRenderer.GetCardSize(ZoneRenderer.HandScale);
                    float spacing = cardSize.X * CardSpacingRatio;
                    float totalWidth = cards.Count * cardSize.X + (cards.Count - 1) * spacing;

                    float centerX = _game.Scale.CurrentWidth / 2;
                    float startX = centerX - totalWidth / 2;
                    float y = _game.Scale.CurrentHeight - _game.Scale.S(280f);

                    for (int i = 0; i < choice.Options.Count; i++)
                    {
                        var option = choice.Options[i];
                        int cardIndex = cards.FindIndex(c => c.Id == option.Card.Id);
                        if (cardIndex < 0) continue;

                        float cardX = startX + cardIndex * (cardSize.X + spacing);
                        var cardRect = new FloatRect(new Vector2f(cardX, y), cardSize);

                        if (cardRect.Contains(new Vector2f(_mousePosition.X, _mousePosition.Y)))
                        {
                            _hoveredCard = option.Card;
                            _hoveredChoiceIndex = option.Index;
                            return;
                        }
                    }
                }
                break;

            case ChoiceType.SelectFromStomach:
                {
                    var cards = State.Stomach.Cards.ToList();
                    if (cards.Count == 0) return;

                    float x = _game.Scale.CurrentWidth - _game.Scale.S(SidePanelWidth + PaddingHorizontal);
                    float baseY = _game.Scale.S(StatusTextHeight + PaddingVertical + 25f);
                    var cardSize = _cardRenderer.GetCardSize(ZoneRenderer.StomachScale);
                    float spacing = _game.Scale.S(StomachCardSpacing);

                    // Karty bez nakładania, odwrócona kolejność (najnowsza na górze)
                    int maxVisible = Math.Min(cards.Count, 6);
                    for (int i = 0; i < choice.Options.Count; i++)
                    {
                        var option = choice.Options[i];
                        int cardIndex = cards.FindIndex(c => c.Id == option.Card.Id);
                        if (cardIndex < 0) continue;

                        // Oblicz pozycję wizualną (odwrócona kolejność)
                        int visualIndex = cards.Count - 1 - cardIndex;
                        if (visualIndex >= maxVisible) continue;

                        float cardY = baseY + visualIndex * (cardSize.Y + spacing);
                        var cardRect = new FloatRect(new Vector2f(x, cardY), cardSize);

                        if (cardRect.Contains(new Vector2f(_mousePosition.X, _mousePosition.Y)))
                        {
                            _hoveredCard = option.Card;
                            _hoveredChoiceIndex = option.Index;
                            return;
                        }
                    }
                }
                break;
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

        // Animations (na wierzchu, nad kartami ale pod UI)
        _animationManager.Render(window);

        // Error message (na wierzchu)
        DrawErrorMessage(window);
    }

    private void DrawStatusBar(RenderWindow window)
    {
        float y = _game.Scale.S(PaddingVertical);
        float spacing = _game.Scale.S(40f);  // odstęp między elementami

        // Oblicz całkowitą szerokość wszystkich elementów
        var fatBounds = _fatLabel.GetLocalBounds();
        var wpBounds = _willpowerLabel.GetLocalBounds();
        var turnBounds = _turnLabel.GetLocalBounds();
        float totalWidth = fatBounds.Size.X + wpBounds.Size.X + turnBounds.Size.X + spacing * 2;

        // Wyśrodkuj wszystko
        float startX = (_game.Scale.CurrentWidth - totalWidth) / 2;

        _fatLabel.Position = new Vector2f(startX, y);
        window.Draw(_fatLabel);

        _willpowerLabel.Position = new Vector2f(startX + fatBounds.Size.X + spacing, y);
        window.Draw(_willpowerLabel);

        _turnLabel.Position = new Vector2f(startX + fatBounds.Size.X + wpBounds.Size.X + spacing * 2, y);
        window.Draw(_turnLabel);
    }

    private void DrawPreviewPanel(RenderWindow window)
    {
        float x = _game.Scale.S(PaddingHorizontal);
        float baseY = _game.Scale.S(PaddingVertical);
        float width = _game.Scale.S(PreviewPanelWidth);
        float buttonHeight = _game.Scale.S(35f);

        // Przycisk "Lista Kart"
        _cardListButtonRect = new FloatRect(new Vector2f(x, baseY), new Vector2f(width, buttonHeight));

        var buttonBg = new RectangleShape(new Vector2f(width, buttonHeight))
        {
            Position = new Vector2f(x, baseY),
            FillColor = new Color(50, 50, 55),
            OutlineColor = new Color(70, 70, 75),
            OutlineThickness = _game.Scale.S(1f)
        };
        window.Draw(buttonBg);

        var buttonText = new Text(_game.Assets.DefaultFont, "Lista Kart", _game.Scale.S(Theme.FontSizeSmall))
        {
            FillColor = Theme.TextSecondary
        };
        var btnBounds = buttonText.GetLocalBounds();
        buttonText.Position = new Vector2f(
            x + (width - btnBounds.Size.X) / 2,
            baseY + (buttonHeight - btnBounds.Size.Y) / 2 - _game.Scale.S(3f)
        );
        window.Draw(buttonText);

        // Podgląd karty (bez szarego tła) - tylko gdy najechano na kartę
        if (_hoveredCard != null)
        {
            var cardSize = _cardRenderer.GetCardSize(PreviewScale);
            // Wyśrodkuj pionowo (między przyciskiem a dołem ekranu)
            float availableHeight = _game.Scale.CurrentHeight - baseY - buttonHeight - _game.Scale.S(HandYOffset);
            float cardY = baseY + buttonHeight + (availableHeight - cardSize.Y) / 2;
            var cardPos = new Vector2f(x, cardY);
            _cardRenderer.Draw(window, _hoveredCard, cardPos, CardDisplayMode.Full, PreviewScale);
        }
    }

    private void DrawPantryZone(RenderWindow window)
    {
        float x = _game.Scale.S(PaddingHorizontal);
        // Ta sama pozycja Y co Ręka
        float y = _game.Scale.CurrentHeight - _game.Scale.S(HandYOffset);
        var cardSize = _cardRenderer.GetCardSize(ZoneRenderer.HandScale);

        // Label
        var label = new Text(_game.Assets.DefaultFont, $"Spiżarnia [{State.Pantry.Count}]", _game.Scale.S(Theme.FontSizeSmall))
        {
            FillColor = Theme.TextSecondary,
            Position = new Vector2f(x, y - _game.Scale.S(25f))
        };
        window.Draw(label);

        var cards = State.Pantry.Cards.ToList();
        if (cards.Count == 0)
        {
            // Puste miejsce
            var slot = new RectangleShape(cardSize)
            {
                Position = new Vector2f(x, y),
                FillColor = new Color(40, 40, 45),
                OutlineColor = new Color(60, 60, 65),
                OutlineThickness = _game.Scale.S(2f)
            };
            window.Draw(slot);

            // Seed pod Spiżarnią (nawet gdy pusta)
            DrawSeedText(window, x, y + cardSize.Y);
            return;
        }

        // Efekt stosu (offsetowane cienie)
        int shadowCount = Math.Min(3, cards.Count - 1);
        for (int i = shadowCount; i > 0; i--)
        {
            float offset = _game.Scale.S(2f) * i;
            var shadowPos = new Vector2f(x + offset, y + offset);
            var shadow = new RectangleShape(cardSize)
            {
                Position = shadowPos,
                FillColor = new Color(30, 30, 35)
            };
            window.Draw(shadow);
        }

        // Górna karta (rewers z widocznym smakiem)
        var topCard = cards[^1];
        _cardRenderer.Draw(window, topCard, new Vector2f(x, y), CardDisplayMode.Back, ZoneRenderer.HandScale);

        // Seed pod Spiżarnią (dla debugowania)
        DrawSeedText(window, x, y + cardSize.Y);
    }

    private void DrawSeedText(RenderWindow window, float x, float aboveY)
    {
        if (_controller.Seed.HasValue)
        {
            var seedText = new Text(_game.Assets.DefaultFont,
                $"Seed: {_controller.Seed.Value}",
                _game.Scale.S(Theme.FontSizeSmall))
            {
                FillColor = Theme.TextMuted,
                Position = new Vector2f(x, aboveY + _game.Scale.S(10f))
            };
            window.Draw(seedText);
        }
    }

    private void DrawToiletZone(RenderWindow window)
    {
        var cardSize = _cardRenderer.GetCardSize(ZoneRenderer.HandScale);
        // Ta sama pozycja Y co Ręka, wyrównany do prawej
        float x = _game.Scale.CurrentWidth - _game.Scale.S(PaddingHorizontal) - cardSize.X;
        float y = _game.Scale.CurrentHeight - _game.Scale.S(HandYOffset);

        // Label
        var label = new Text(_game.Assets.DefaultFont, $"Kibelek [{State.Toilet.Count}]", _game.Scale.S(Theme.FontSizeSmall))
        {
            FillColor = Theme.TextSecondary,
            Position = new Vector2f(x, y - _game.Scale.S(25f))
        };
        window.Draw(label);

        var cards = State.Toilet.Cards.ToList();
        if (cards.Count == 0)
        {
            // Puste miejsce
            var slot = new RectangleShape(cardSize)
            {
                Position = new Vector2f(x, y),
                FillColor = new Color(40, 40, 45),
                OutlineColor = new Color(60, 60, 65),
                OutlineThickness = _game.Scale.S(2f)
            };
            window.Draw(slot);
            return;
        }

        // Efekt stosu (offsetowane cienie)
        int shadowCount = Math.Min(3, cards.Count - 1);
        for (int i = shadowCount; i > 0; i--)
        {
            float offset = _game.Scale.S(2f) * i;
            var shadowPos = new Vector2f(x + offset, y + offset);
            var shadow = new RectangleShape(cardSize)
            {
                Position = shadowPos,
                FillColor = new Color(30, 30, 35)
            };
            window.Draw(shadow);
        }

        // Górna karta (awers)
        var topCard = cards[^1];
        _cardRenderer.Draw(window, topCard, new Vector2f(x, y), CardDisplayMode.Small, ZoneRenderer.HandScale);
    }

    private void DrawStomachZone(RenderWindow window)
    {
        float x = _game.Scale.CurrentWidth - _game.Scale.S(SidePanelWidth + PaddingHorizontal);
        float y = _game.Scale.S(StatusTextHeight + PaddingVertical);

        var cards = State.Stomach.Cards.ToList();

        // Label
        var label = new Text(_game.Assets.DefaultFont, $"Żołądek [{cards.Count}]", _game.Scale.S(Theme.FontSizeSmall))
        {
            FillColor = Theme.TextSecondary,
            Position = new Vector2f(x, y)
        };
        window.Draw(label);

        if (cards.Count == 0) return;

        // Karty jedna pod drugą (bez nakładania), odwrócona kolejność
        // Najnowsza (ostatnia dodana) na górze, najstarsza (do kibelka) na dole
        float cardY = y + _game.Scale.S(25f);
        var cardSize = _cardRenderer.GetCardSize(ZoneRenderer.StomachScale);
        float spacing = _game.Scale.S(StomachCardSpacing);

        bool isChoosing = _controller.IsAwaitingChoice &&
                          State.PendingChoice?.Type == ChoiceType.SelectFromStomach;

        int maxVisible = Math.Min(cards.Count, 6);
        for (int i = 0; i < maxVisible; i++)
        {
            // Odwrócona kolejność: i=0 → najnowsza (cards[last]), i=maxVisible-1 → najstarsza
            int cardIndex = cards.Count - 1 - i;
            var card = cards[cardIndex];

            // Skip cards that are currently being animated
            if (_animationManager.IsCardAnimating(card.Id)) continue;

            var cardPos = new Vector2f(x, cardY + i * (cardSize.Y + spacing));
            bool isHovered = cardIndex == _hoveredStomachIndex;
            bool isChoiceOption = isChoosing && State.PendingChoice!.Options.Any(o => o.Card.Id == card.Id);

            // Tint dla podświetlenia (hover lub choice)
            Color? tint = (isHovered || isChoiceOption) ? new Color(255, 255, 180) : null;

            _cardRenderer.Draw(window, card, cardPos, CardDisplayMode.Tiny, ZoneRenderer.StomachScale, tint);
        }

        if (cards.Count > 6)
        {
            var moreLabel = new Text(_game.Assets.DefaultFont, $"+{cards.Count - 6} więcej", _game.Scale.S(12u))
            {
                FillColor = Theme.TextSecondary,
                Position = new Vector2f(x, cardY + maxVisible * (cardSize.Y + spacing))
            };
            window.Draw(moreLabel);
        }
    }

    private void DrawTableZone(RenderWindow window)
    {
        float centerX = _game.Scale.CurrentWidth / 2;
        var cardSize = _cardRenderer.GetCardSize(ZoneRenderer.TableScale);
        float y = _game.Scale.S(StatusTextHeight + 100f) + cardSize.Y * 0.5f;  // przesunięte w dół o 1/2 wysokości karty

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

        // Calculate card positions
        float spacing = cardSize.X * CardSpacingRatio;
        float totalWidth = cards.Count * cardSize.X + (cards.Count - 1) * spacing;
        float startX = centerX - totalWidth / 2;

        bool isChoosing = _controller.IsAwaitingChoice &&
                          State.PendingChoice?.Type == ChoiceType.SelectFromTable;

        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i];

            // Skip cards that are currently being animated
            if (_animationManager.IsCardAnimating(card.Id)) continue;

            float cardX = startX + i * (cardSize.X + spacing);
            float cardY = y;
            float scale = ZoneRenderer.TableScale;

            bool isHovered = i == _hoveredTableIndex;
            bool isChoiceOption = isChoosing && State.PendingChoice!.Options.Any(o => o.Card.Id == card.Id);

            // Tint dla podświetlenia (hover lub choice) - bez unoszenia/powiększania
            Color? tint = (isHovered || isChoiceOption) ? new Color(255, 255, 180) : null;

            _cardRenderer.Draw(window, card, new Vector2f(cardX, cardY), CardDisplayMode.Small, scale, tint);
        }
    }

    private void DrawHandZone(RenderWindow window)
    {
        float centerX = _game.Scale.CurrentWidth / 2;
        float y = _game.Scale.CurrentHeight - _game.Scale.S(280f);

        var cards = State.Hand.Cards.ToList();
        var cardSize = _cardRenderer.GetCardSize(ZoneRenderer.HandScale);

        if (cards.Count > 0)
        {
            // Calculate card positions
            float spacing = cardSize.X * CardSpacingRatio;
            float totalWidth = cards.Count * cardSize.X + (cards.Count - 1) * spacing;
            float startX = centerX - totalWidth / 2;

            bool isChoosing = _controller.IsAwaitingChoice &&
                              (State.PendingChoice?.Type == ChoiceType.SelectFromHand ||
                               State.PendingChoice?.Type == ChoiceType.DiscardFromHand);

            // Draw cards with highlight for hovered one
            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];

                // Skip cards that are currently being animated
                if (_animationManager.IsCardAnimating(card.Id)) continue;

                float cardX = startX + i * (cardSize.X + spacing);
                float cardY = y;
                float scale = ZoneRenderer.HandScale;

                // Wysunięcie dla grywalnych kart (animowane)
                if (_cardElevation.TryGetValue(card.Id, out var elevation) && elevation > 0)
                {
                    cardY -= cardSize.Y * ElevationRatio * elevation;
                }

                bool isHovered = i == _hoveredHandIndex;
                bool isChoiceOption = isChoosing && State.PendingChoice!.Options.Any(o => o.Card.Id == card.Id);

                // Tint dla podświetlenia (hover lub choice) - bez dodatkowego unoszenia
                Color? tint = (isHovered || isChoiceOption) ? new Color(255, 255, 180) : null;

                _cardRenderer.Draw(window, card, new Vector2f(cardX, cardY), CardDisplayMode.Small, scale, tint);
            }
        }

        // Label pod kartami
        var label = new Text(_game.Assets.DefaultFont, "Ręka", _game.Scale.S(Theme.FontSizeSmall))
        {
            FillColor = Theme.TextSecondary
        };
        var labelBounds = label.GetLocalBounds();
        label.Position = new Vector2f(centerX - labelBounds.Size.X / 2, y + cardSize.Y + _game.Scale.S(10f));
        window.Draw(label);
    }

    private void DrawInfoBar(RenderWindow window)
    {
        // Zawsze duża czcionka i pozycja między stołem a ręką
        _infoText.CharacterSize = _game.Scale.S(Theme.FontSizeTitle);
        var bounds = _infoText.GetLocalBounds();
        float y = _game.Scale.CurrentHeight * 0.55f;

        _infoText.Position = new Vector2f(
            (_game.Scale.CurrentWidth - bounds.Size.X) / 2,
            y
        );

        window.Draw(_infoText);
    }

    private void DrawErrorMessage(RenderWindow window)
    {
        if (_errorMessageTimer <= 0 || string.IsNullOrEmpty(_errorMessage))
            return;

        // Oblicz alpha na podstawie pozostałego czasu (fade out w ostatniej sekundzie)
        byte alpha = 255;
        if (_errorMessageTimer < 1.0f)
        {
            alpha = (byte)(_errorMessageTimer * 255);
        }

        // Tekst komunikatu
        var errorText = new Text(_game.Assets.DefaultFont, _errorMessage, _game.Scale.S(Theme.FontSizeNormal))
        {
            FillColor = new Color(255, 255, 255, alpha)
        };
        var textBounds = errorText.GetLocalBounds();

        // Prostokąt tła
        float padding = _game.Scale.S(20f);
        float rectWidth = textBounds.Size.X + padding * 2;
        float rectHeight = textBounds.Size.Y + padding * 2;

        float rectX = (_game.Scale.CurrentWidth - rectWidth) / 2;
        float rectY = (_game.Scale.CurrentHeight - rectHeight) / 2;

        var bgRect = new RectangleShape(new Vector2f(rectWidth, rectHeight))
        {
            Position = new Vector2f(rectX, rectY),
            FillColor = new Color(180, 60, 60, alpha),
            OutlineColor = new Color(220, 80, 80, alpha),
            OutlineThickness = _game.Scale.S(2f)
        };
        window.Draw(bgRect);

        // Wyśrodkuj tekst w prostokącie
        errorText.Position = new Vector2f(
            rectX + padding,
            rectY + padding - _game.Scale.S(4f)  // korekta pionowa dla fontu
        );
        window.Draw(errorText);
    }

    private void UpdateLayout()
    {
        // Update font sizes
        _fatLabel.CharacterSize = _game.Scale.S(Theme.FontSizeNormal);
        _willpowerLabel.CharacterSize = _game.Scale.S(Theme.FontSizeNormal);
        _turnLabel.CharacterSize = _game.Scale.S(Theme.FontSizeNormal);
        _infoText.CharacterSize = _game.Scale.S(Theme.FontSizeSmall);
    }

    private void OpenCardListScene(CardListMode mode, Flavor? flavorFilter = null)
    {
        if (_cardListSceneOpen) return;

        _cardListSceneOpen = true;

        CardListScene cardListScene;

        if (mode == CardListMode.Select && State.PendingChoice != null)
        {
            // Select mode - użyj opcji z PendingChoice (zachowuje indeksy)
            cardListScene = new CardListScene(_game, State.PendingChoice.Options, flavorFilter)
            {
                OnClose = () => _cardListSceneOpen = false,
                OnCardSelected = selectedIndex =>
                {
                    _game.SceneManager.PopScene();
                    _cardListSceneOpen = false;
                    _controller.MakeChoice(selectedIndex);
                }
            };

            // Ustaw tytuł kontekstowy dla strefy
            cardListScene.CustomTitle = State.PendingChoice.Type switch
            {
                ChoiceType.SelectFromToilet => "Wybierz z Kibelka",
                ChoiceType.SelectFromPantry => "Wybierz ze Spiżarni",
                _ => null
            };
        }
        else
        {
            // Browse mode - pokaż wszystkie karty
            cardListScene = new CardListScene(_game, flavorFilter)
            {
                OnClose = () => _cardListSceneOpen = false
            };
        }

        _game.SceneManager.PushScene(cardListScene);
    }

    public void OnCardListSceneClosed()
    {
        _cardListSceneOpen = false;
    }

    #region Animation System

    /// <summary>
    /// Przetwarza eventy z silnika gry i tworzy odpowiednie animacje.
    /// </summary>
    private void ProcessGameEvents()
    {
        var events = _controller.FlushEvents();

        foreach (var evt in events)
        {
            switch (evt)
            {
                case CardDrawnEvent drawn:
                    AnimateCardDraw(drawn.Card);
                    break;

                case CardPlayedEvent played:
                    // Karta została zagrana - animuj z ręki do stołu lub kibelka
                    // W tym momencie karta już jest w nowej strefie w State
                    AnimateCardFromHand(played.Card, played.HandIndex);
                    break;

                case CardEatenEvent eaten:
                    // Karta została zjedzona - animuj z ręki do żołądka
                    AnimateCardEat(eaten.Card, eaten.HandIndex);
                    break;

                case CardDiscardedEvent discarded:
                    // Karta trafiła do kibelka
                    AnimateCardDiscard(discarded.Card, discarded.FromZone);
                    break;

                // Inne eventy (FatChanged, WillpowerChanged, etc.) - można dodać animacje liczników później
            }
        }
    }

    /// <summary>
    /// Animuje dobieranie karty ze spiżarni do ręki.
    /// </summary>
    private void AnimateCardDraw(Card card)
    {
        var startPos = GetPantryPosition();
        var endPos = GetHandCardPosition(State.Hand.Count - 1);

        var animation = new CardMoveAnimation(
            _cardRenderer,
            card,
            startPos,
            endPos,
            duration: 0.25f,
            easing: Easing.EaseOutBack,
            startScale: ZoneRenderer.HandScale,
            endScale: ZoneRenderer.HandScale,
            startMode: CardDisplayMode.Back,
            endMode: CardDisplayMode.Small
        );

        _animationManager.StartImmediate(animation);
    }

    /// <summary>
    /// Animuje kartę zagraną z ręki (do stołu lub kibelka).
    /// </summary>
    private void AnimateCardFromHand(Card card, int handIndex)
    {
        var startPos = GetHandCardPositionForCount(handIndex, State.Hand.Count + 1); // +1 bo karta już usunięta

        // Sprawdź gdzie karta trafiła
        Vector2f endPos;
        float endScale;
        CardDisplayMode endMode;

        if (State.Table.Cards.Any(c => c.Id == card.Id))
        {
            // Karta trafiła na stół
            int tableIndex = State.Table.Cards.ToList().FindIndex(c => c.Id == card.Id);
            endPos = GetTableCardPosition(tableIndex);
            endScale = ZoneRenderer.TableScale;
            endMode = CardDisplayMode.Small;
        }
        else
        {
            // Karta trafiła do kibelka
            endPos = GetToiletPosition();
            endScale = ZoneRenderer.HandScale;
            endMode = CardDisplayMode.Back;
        }

        var animation = new CardMoveAnimation(
            _cardRenderer,
            card,
            startPos,
            endPos,
            duration: 0.3f,
            easing: Easing.EaseInOutCubic,
            startScale: ZoneRenderer.HandScale,
            endScale: endScale,
            startMode: CardDisplayMode.Small,
            endMode: endMode
        );

        _animationManager.StartImmediate(animation);
    }

    /// <summary>
    /// Animuje zjedzenie karty (z ręki do żołądka).
    /// </summary>
    private void AnimateCardEat(Card card, int handIndex)
    {
        var startPos = GetHandCardPositionForCount(handIndex, State.Hand.Count + 1); // +1 bo karta już usunięta
        var endPos = GetStomachCardPosition(0); // Najnowsza karta na górze

        var animation = new CardMoveAnimation(
            _cardRenderer,
            card,
            startPos,
            endPos,
            duration: 0.3f,
            easing: Easing.EaseInOutCubic,
            startScale: ZoneRenderer.HandScale,
            endScale: ZoneRenderer.StomachScale,
            startMode: CardDisplayMode.Small,
            endMode: CardDisplayMode.Tiny
        );

        _animationManager.StartImmediate(animation);
    }

    /// <summary>
    /// Animuje odrzucenie karty do kibelka.
    /// </summary>
    private void AnimateCardDiscard(Card card, ZoneType fromZone)
    {
        Vector2f startPos = fromZone switch
        {
            ZoneType.Hand => GetHandCardPosition(0),
            ZoneType.Table => GetTableCardPosition(0),
            ZoneType.Stomach => GetStomachCardPosition(0),
            _ => GetPantryPosition()
        };

        var (startScale, startMode) = GetZoneScaleAndMode(fromZone);
        var endPos = GetToiletPosition();

        var animation = new CardMoveAnimation(
            _cardRenderer,
            card,
            startPos,
            endPos,
            duration: 0.25f,
            easing: Easing.EaseInOutQuad,
            startScale: startScale,
            endScale: ZoneRenderer.HandScale,
            startMode: startMode,
            endMode: CardDisplayMode.Back
        );

        _animationManager.StartImmediate(animation);
    }

    #endregion

    #region Position Helpers

    /// <summary>
    /// Oblicza pozycję karty w ręce dla bieżącej liczby kart.
    /// </summary>
    private Vector2f GetHandCardPosition(int index)
    {
        return GetHandCardPositionForCount(index, State.Hand.Count);
    }

    /// <summary>
    /// Oblicza pozycję karty w ręce dla określonej liczby kart.
    /// </summary>
    private Vector2f GetHandCardPositionForCount(int index, int cardCount)
    {
        if (cardCount == 0) return new Vector2f(0, 0);

        var cardSize = _cardRenderer.GetCardSize(ZoneRenderer.HandScale);
        float spacing = cardSize.X * CardSpacingRatio;
        float totalWidth = cardCount * cardSize.X + (cardCount - 1) * spacing;

        float centerX = _game.Scale.CurrentWidth / 2;
        float startX = centerX - totalWidth / 2;
        float y = _game.Scale.CurrentHeight - _game.Scale.S(280f);

        return new Vector2f(startX + index * (cardSize.X + spacing), y);
    }

    /// <summary>
    /// Oblicza pozycję karty na stole.
    /// </summary>
    private Vector2f GetTableCardPosition(int index)
    {
        int cardCount = State.Table.Count;
        if (cardCount == 0) cardCount = 1;

        var cardSize = _cardRenderer.GetCardSize(ZoneRenderer.TableScale);
        float spacing = cardSize.X * CardSpacingRatio;
        float totalWidth = cardCount * cardSize.X + (cardCount - 1) * spacing;

        float centerX = _game.Scale.CurrentWidth / 2;
        float startX = centerX - totalWidth / 2;
        float y = _game.Scale.S(StatusTextHeight + 100f) + cardSize.Y * 0.5f;

        return new Vector2f(startX + index * (cardSize.X + spacing), y);
    }

    /// <summary>
    /// Oblicza pozycję karty w żołądku.
    /// </summary>
    private Vector2f GetStomachCardPosition(int visualIndex)
    {
        float x = _game.Scale.CurrentWidth - _game.Scale.S(SidePanelWidth + PaddingHorizontal);
        float baseY = _game.Scale.S(StatusTextHeight + PaddingVertical + 25f);
        var cardSize = _cardRenderer.GetCardSize(ZoneRenderer.StomachScale);
        float spacing = _game.Scale.S(StomachCardSpacing);

        return new Vector2f(x, baseY + visualIndex * (cardSize.Y + spacing));
    }

    /// <summary>
    /// Zwraca pozycję spiżarni.
    /// </summary>
    private Vector2f GetPantryPosition()
    {
        float x = _game.Scale.S(PaddingHorizontal);
        float y = _game.Scale.CurrentHeight - _game.Scale.S(HandYOffset);
        return new Vector2f(x, y);
    }

    /// <summary>
    /// Zwraca pozycję kibelka.
    /// </summary>
    private Vector2f GetToiletPosition()
    {
        var cardSize = _cardRenderer.GetCardSize(ZoneRenderer.HandScale);
        float x = _game.Scale.CurrentWidth - _game.Scale.S(PaddingHorizontal) - cardSize.X;
        float y = _game.Scale.CurrentHeight - _game.Scale.S(HandYOffset);
        return new Vector2f(x, y);
    }

    /// <summary>
    /// Zwraca skalę i tryb wyświetlania dla danej strefy.
    /// </summary>
    private (float scale, CardDisplayMode mode) GetZoneScaleAndMode(ZoneType zone)
    {
        return zone switch
        {
            ZoneType.Pantry => (ZoneRenderer.HandScale, CardDisplayMode.Back),
            ZoneType.Hand => (ZoneRenderer.HandScale, CardDisplayMode.Small),
            ZoneType.Table => (ZoneRenderer.TableScale, CardDisplayMode.Small),
            ZoneType.Stomach => (ZoneRenderer.StomachScale, CardDisplayMode.Tiny),
            ZoneType.Toilet => (ZoneRenderer.HandScale, CardDisplayMode.Back),
            _ => (1.0f, CardDisplayMode.Small)
        };
    }

    #endregion
}
