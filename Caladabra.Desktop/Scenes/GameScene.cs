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

    // Mouse interaction state (LPM-only: double-click to play, hold to eat)
    private bool _isMousePressed = false;
    private float _mouseHoldTime = 0f;
    private int _mouseHoldHandIndex = -1;
    private float _totalTime = 0f;  // Total elapsed time for double-click detection
    private float _lastClickTime = 0f;
    private int _lastClickHandIndex = -1;
    private const float DoubleClickThreshold = 0.3f;  // 300ms
    private const float EatProgressBarDelay = 0.3f;    // Wait before showing bar
    private const float EatProgressBarDuration = 0.7f; // Time to fill the bar

    // Eating progress bar
    private ProgressBar _eatProgressBar = null!;

    // CardList scene state
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

    // Inline card selection (zamiast CardListScene)
    private bool _inlineSelectionActive = false;
    private List<ChoiceOption> _selectionOptions = new();
    private int _selectionScrollOffset = 0;
    private HashSet<int> _selectedIndices = new();
    private int _hoveredSelectionIndex = -1;
    private const int MaxVisibleSelectionCards = 5;
    private const float SelectionCardScale = 1.0f;
    private const float ArrowButtonSize = 40f;

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

        // Status bar labels (2x większa czcionka)
        _fatLabel = new Text(font, "", _game.Scale.S(Theme.FontSizeStats))
        {
            FillColor = Theme.TextPrimary
        };

        _willpowerLabel = new Text(font, "", _game.Scale.S(Theme.FontSizeStats))
        {
            FillColor = Theme.TextPrimary
        };

        _turnLabel = new Text(font, "", _game.Scale.S(Theme.FontSizeStats))
        {
            FillColor = Theme.TextPrimary
        };

        _infoText = new Text(font, "ESC = wyjście | 2xLPM = zagraj | przytrzymaj = zjedz", _game.Scale.S(Theme.FontSizeInfoText))
        {
            FillColor = Theme.TextSecondary
        };

        // Eating progress bar
        _eatProgressBar = new ProgressBar(font, _game.Scale)
        {
            Label = "Zjadam",
            IsVisible = false
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

        // LPM pressed - start tracking for double-click or hold
        if (sfmlEvent.Type == EventType.MouseButtonPressed && sfmlEvent.MouseButton.Button == Mouse.Button.Left)
        {
            HandleLeftMouseDown();
        }

        // LPM released - detect single click (show hint) or cancel hold
        if (sfmlEvent.Type == EventType.MouseButtonReleased && sfmlEvent.MouseButton.Button == Mouse.Button.Left)
        {
            HandleLeftMouseUp();
        }

        // Scroll = przewijanie inline selection
        if (sfmlEvent.Type == EventType.MouseWheelScrolled && _inlineSelectionActive)
        {
            if (sfmlEvent.MouseWheelScroll.Delta > 0)
                _selectionScrollOffset = Math.Max(0, _selectionScrollOffset - 1);
            else
                _selectionScrollOffset = Math.Min(
                    Math.Max(0, _selectionOptions.Count - MaxVisibleSelectionCards),
                    _selectionScrollOffset + 1);
        }
    }

    private void HandleLeftMouseDown()
    {
        // Blokada interakcji podczas animacji
        if (_animationManager.IsAnimating) return;

        // Gra skończona - nie reaguj na kliknięcia
        if (_controller.IsGameOver) return;

        // Obsługa inline selection (immediate click, no double-click/hold)
        if (_inlineSelectionActive && State.PendingChoice != null)
        {
            HandleInlineSelectionClick();
            return;
        }

        // If awaiting choice, handle choice selection (immediate)
        if (_controller.IsAwaitingChoice && _hoveredChoiceIndex >= 0)
        {
            _controller.MakeChoice(_hoveredChoiceIndex);
            _hoveredChoiceIndex = -1;
            return;
        }

        // Start tracking mouse hold for cards in hand
        if (_hoveredHandIndex >= 0 && !_controller.IsAwaitingChoice)
        {
            // Check for double-click (play card)
            if (_hoveredHandIndex == _lastClickHandIndex &&
                (_totalTime - _lastClickTime) < DoubleClickThreshold)
            {
                // Double-click detected - try to play
                TryPlayCard(_hoveredHandIndex);
                _lastClickHandIndex = -1;  // Reset to prevent triple-click
                return;
            }

            // Start hold tracking for eating
            _isMousePressed = true;
            _mouseHoldTime = 0f;
            _mouseHoldHandIndex = _hoveredHandIndex;
        }
    }

    private void HandleLeftMouseUp()
    {
        if (!_isMousePressed)
        {
            return;
        }

        var totalEatTime = EatProgressBarDelay + EatProgressBarDuration;

        // If released before eat completes and on a hand card - it was a single click
        if (_mouseHoldTime < totalEatTime && _mouseHoldHandIndex >= 0)
        {
            // Record click time for double-click detection
            _lastClickTime = _totalTime;
            _lastClickHandIndex = _mouseHoldHandIndex;
        }

        ResetMouseHold();
    }

    private void HandleInlineSelectionClick()
    {
        var choice = State.PendingChoice!;
        int requiredCount = choice.MaxChoices;

        // Klik na strzałkę lewą
        if (_selectionScrollOffset > 0 && IsMouseOver(GetLeftArrowRect()))
        {
            _selectionScrollOffset--;
            return;
        }

        // Klik na strzałkę prawą
        if (_selectionScrollOffset + MaxVisibleSelectionCards < _selectionOptions.Count &&
            IsMouseOver(GetRightArrowRect()))
        {
            _selectionScrollOffset++;
            return;
        }

        // Klik na przycisk Zatwierdź (multi-select)
        if (requiredCount > 1 && _selectedIndices.Count == requiredCount &&
            IsMouseOver(GetConfirmButtonRect()))
        {
            var indices = _selectedIndices.Select(i => _selectionOptions[i].Index).ToArray();
            CloseInlineSelection();
            _controller.MakeChoice(indices[0]); // Na razie single - TODO: multi
            return;
        }

        // Klik na kartę
        if (_hoveredSelectionIndex >= 0)
        {
            if (requiredCount == 1)
            {
                var choiceIndex = _selectionOptions[_hoveredSelectionIndex].Index;
                CloseInlineSelection();
                _controller.MakeChoice(choiceIndex);
            }
            else
            {
                if (_selectedIndices.Contains(_hoveredSelectionIndex))
                    _selectedIndices.Remove(_hoveredSelectionIndex);
                else if (_selectedIndices.Count < requiredCount)
                    _selectedIndices.Add(_hoveredSelectionIndex);
            }
        }
    }

    private void TryPlayCard(int handIndex)
    {
        ResetMouseHold();

        if (!_controller.CanPlayCard(handIndex))
        {
            ShowErrorMessage("Masz za mało Siły Woli żeby zagrać tę kartę");
            return;
        }

        if (_controller.PlayCard(handIndex))
        {
            _hoveredCard = null;
            _hoveredHandIndex = -1;
        }
    }

    private void TryEatCard(int handIndex)
    {
        ResetMouseHold();

        if (_controller.EatCard(handIndex))
        {
            _hoveredCard = null;
            _hoveredHandIndex = -1;
        }
    }

    private void ResetMouseHold()
    {
        _isMousePressed = false;
        _mouseHoldTime = 0f;
        _mouseHoldHandIndex = -1;
        _eatProgressBar.IsVisible = false;
    }

    private void ShowErrorMessage(string message)
    {
        _errorMessage = message;
        _errorMessageTimer = ErrorMessageDuration;
    }

    public void Update(float deltaTime)
    {
        // Track total time for double-click detection
        _totalTime += deltaTime;

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

        // Update mouse hold for eating
        UpdateMouseHold(deltaTime);

        // Auto-show inline selection for CardList/Toilet/Pantry/DiscardFromHand choices
        if (_controller.IsAwaitingChoice && State.PendingChoice != null && !_inlineSelectionActive)
        {
            var choiceType = State.PendingChoice.Type;
            if (IsInlineSelectionChoiceType(choiceType))
            {
                ShowInlineSelection(State.PendingChoice);
            }
        }

        // Zamknij inline selection jeśli gra nie czeka już na wybór
        if (_inlineSelectionActive && !_controller.IsAwaitingChoice)
        {
            CloseInlineSelection();
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
                _infoText.DisplayedString = "Kliknij podwójnie kartę LPM by ją zagrać, przytrzymaj LPM by ją zjeść.";
                _infoText.FillColor = new Color(255, 200, 100);  // Żółty jak reszta
            }
            else
            {
                _infoText.DisplayedString = "Nie masz dość siły woli! Przytrzymaj kartę by zjeść.";
                _infoText.FillColor = new Color(255, 150, 100);
            }
        }
    }

    private void UpdateMouseHold(float deltaTime)
    {
        if (!_isMousePressed || _mouseHoldHandIndex < 0)
        {
            _eatProgressBar.IsVisible = false;
            return;
        }

        // Check if mouse moved away from the card
        if (_hoveredHandIndex != _mouseHoldHandIndex)
        {
            ResetMouseHold();
            return;
        }

        _mouseHoldTime += deltaTime;

        // Phase 1: Delay before showing progress bar (0.3s)
        if (_mouseHoldTime < EatProgressBarDelay)
        {
            _eatProgressBar.IsVisible = false;
            return;
        }

        // Phase 2: Show and fill progress bar (0.7s)
        var progressTime = _mouseHoldTime - EatProgressBarDelay;
        _eatProgressBar.Progress = progressTime / EatProgressBarDuration;
        _eatProgressBar.IsVisible = true;

        // Position progress bar at center of held card
        PositionEatProgressBar();

        // Check if hold time exceeded - eat the card
        if (progressTime >= EatProgressBarDuration)
        {
            TryEatCard(_mouseHoldHandIndex);
        }
    }

    private void PositionEatProgressBar()
    {
        // Calculate card position in hand
        var handCards = State.Hand.Cards.ToList();
        if (_mouseHoldHandIndex < 0 || _mouseHoldHandIndex >= handCards.Count)
            return;

        var card = handCards[_mouseHoldHandIndex];
        var cardSize = _cardRenderer.GetCardSize(ZoneRenderer.HandScale);
        float spacing = cardSize.X * CardSpacingRatio;
        float totalWidth = handCards.Count * cardSize.X + (handCards.Count - 1) * spacing;
        float startX = (_game.Scale.CurrentWidth - totalWidth) / 2;
        float y = _game.Scale.CurrentHeight - _game.Scale.S(HandYOffset);

        // Apply elevation offset (same as in DrawHandZone)
        if (_cardElevation.TryGetValue(card.Id, out var elevation) && elevation > 0)
        {
            y -= cardSize.Y * ElevationRatio * elevation;
        }

        float cardX = startX + _mouseHoldHandIndex * (cardSize.X + spacing);
        float cardCenterX = cardX + cardSize.X / 2;
        float cardCenterY = y + cardSize.Y / 2;

        // Position progress bar centered on card
        _eatProgressBar.Position = new Vector2f(
            cardCenterX - _eatProgressBar.Size.X / 2,
            cardCenterY - _eatProgressBar.Size.Y / 2
        );
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
        _hoveredSelectionIndex = -1;

        // Check inline selection first (gdy aktywna)
        if (CheckInlineSelectionHover()) return;

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
        float y = _game.Scale.CurrentHeight - _game.Scale.S(HandYOffset);

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
                    float y = _game.Scale.CurrentHeight - _game.Scale.S(HandYOffset);

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

        // Stół lub inline selection (wzajemnie się wykluczają)
        if (_inlineSelectionActive)
            DrawInlineSelection(window);
        else
            DrawTableZone(window);

        DrawHandZone(window);

        // Eating progress bar (on top of card)
        _eatProgressBar.Draw(window);

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
        // Podgląd karty - tylko gdy najechano na kartę
        if (_hoveredCard != null)
        {
            float x = _game.Scale.S(PaddingHorizontal);
            float baseY = _game.Scale.S(PaddingVertical);
            var cardSize = _cardRenderer.GetCardSize(PreviewScale);
            // Wyśrodkuj pionowo
            float availableHeight = _game.Scale.CurrentHeight - baseY - _game.Scale.S(HandYOffset);
            float cardY = baseY + (availableHeight - cardSize.Y) / 2;
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
        }
        else
        {
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
        }

        // Label POD kartą, wycentrowany
        var label = new Text(_game.Assets.DefaultFont, $"Spiżarnia [{State.Pantry.Count}]", _game.Scale.S(Theme.FontSizeNormal))
        {
            FillColor = Theme.TextSecondary
        };
        var labelBounds = label.GetLocalBounds();
        label.Position = new Vector2f(
            x + (cardSize.X - labelBounds.Size.X) / 2,
            y + cardSize.Y + _game.Scale.S(10f)
        );
        window.Draw(label);
    }

    private void DrawToiletZone(RenderWindow window)
    {
        var cardSize = _cardRenderer.GetCardSize(ZoneRenderer.HandScale);
        // Ta sama pozycja Y co Ręka, wyrównany do prawej
        float x = _game.Scale.CurrentWidth - _game.Scale.S(PaddingHorizontal) - cardSize.X;
        float y = _game.Scale.CurrentHeight - _game.Scale.S(HandYOffset);

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
        }
        else
        {
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

        // Label POD kartą, wycentrowany
        var label = new Text(_game.Assets.DefaultFont, $"Kibelek [{State.Toilet.Count}]", _game.Scale.S(Theme.FontSizeNormal))
        {
            FillColor = Theme.TextSecondary
        };
        var labelBounds = label.GetLocalBounds();
        label.Position = new Vector2f(
            x + (cardSize.X - labelBounds.Size.X) / 2,
            y + cardSize.Y + _game.Scale.S(10f)
        );
        window.Draw(label);
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
            var moreLabel = new Text(_game.Assets.DefaultFont, $"+{cards.Count - 6} więcej", _game.Scale.S(Theme.FontSizeSmall))
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

        var entries = State.Table.Entries.ToList();

        // Label
        var label = new Text(_game.Assets.DefaultFont, "Stół", _game.Scale.S(Theme.FontSizeSmall))
        {
            FillColor = Theme.TextSecondary
        };
        var labelBounds = label.GetLocalBounds();
        label.Position = new Vector2f(centerX - labelBounds.Size.X / 2, y - _game.Scale.S(25f));
        window.Draw(label);

        if (entries.Count == 0) return;

        // Calculate card positions
        float spacing = cardSize.X * CardSpacingRatio;
        float totalWidth = entries.Count * cardSize.X + (entries.Count - 1) * spacing;
        float startX = centerX - totalWidth / 2;

        bool isChoosing = _controller.IsAwaitingChoice &&
                          State.PendingChoice?.Type == ChoiceType.SelectFromTable;

        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var card = entry.Card;

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

            // Rysuj licznik jeśli karta go ma
            if (entry.TurnsRemaining.HasValue && entry.TurnsRemaining.Value > 0)
            {
                _cardRenderer.DrawCounter(window, new Vector2f(cardX, cardY), scale, entry.TurnsRemaining.Value);
            }
        }
    }

    private void DrawHandZone(RenderWindow window)
    {
        float centerX = _game.Scale.CurrentWidth / 2;
        float y = _game.Scale.CurrentHeight - _game.Scale.S(HandYOffset);

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
        var label = new Text(_game.Assets.DefaultFont, "Ręka", _game.Scale.S(Theme.FontSizeNormal))
        {
            FillColor = Theme.TextSecondary
        };
        var labelBounds = label.GetLocalBounds();
        label.Position = new Vector2f(centerX - labelBounds.Size.X / 2, y + cardSize.Y + _game.Scale.S(10f));
        window.Draw(label);
    }

    private void DrawInfoBar(RenderWindow window)
    {
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
        // Update font sizes (stats 2x większe)
        _fatLabel.CharacterSize = _game.Scale.S(Theme.FontSizeStats);
        _willpowerLabel.CharacterSize = _game.Scale.S(Theme.FontSizeStats);
        _turnLabel.CharacterSize = _game.Scale.S(Theme.FontSizeStats);
        _infoText.CharacterSize = _game.Scale.S(Theme.FontSizeInfoText);
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

    #region Inline Selection

    private void ShowInlineSelection(PendingChoice choice)
    {
        _inlineSelectionActive = true;
        _selectionOptions = choice.Options;
        _selectionScrollOffset = 0;
        _selectedIndices.Clear();
        _hoveredSelectionIndex = -1;
    }

    private void CloseInlineSelection()
    {
        _inlineSelectionActive = false;
        _selectionOptions = new();
        _selectedIndices.Clear();
        _hoveredSelectionIndex = -1;
    }

    private bool IsInlineSelectionChoiceType(ChoiceType type)
    {
        return type == ChoiceType.SelectFromCardList ||
               type == ChoiceType.SelectFromCardListFiltered ||
               type == ChoiceType.SelectFromToilet ||
               type == ChoiceType.SelectFromPantry ||
               type == ChoiceType.DiscardFromHand;
    }

    private void DrawInlineSelection(RenderWindow window)
    {
        if (!_inlineSelectionActive || State.PendingChoice == null) return;

        var choice = State.PendingChoice;
        var cardSize = _cardRenderer.GetCardSize(SelectionCardScale);
        float spacing = cardSize.X * CardSpacingRatio;

        // Pozycja Y (tam gdzie stół)
        float y = _game.Scale.S(StatusTextHeight + 100f) + cardSize.Y * 0.5f;
        float centerX = _game.Scale.CurrentWidth / 2;

        // Nagłówek
        int requiredCount = choice.MaxChoices;
        string header = requiredCount == 1
            ? "Wybierz 1 kartę"
            : $"Wybierz {requiredCount} karty";
        var headerText = new Text(_game.Assets.DefaultFont, header, _game.Scale.S(Theme.FontSizeNormal))
        {
            FillColor = Theme.TextPrimary
        };
        var headerBounds = headerText.GetLocalBounds();
        headerText.Position = new Vector2f(centerX - headerBounds.Size.X / 2, y - _game.Scale.S(40f));
        window.Draw(headerText);

        // Oblicz widoczne karty
        int visibleCount = Math.Min(MaxVisibleSelectionCards, _selectionOptions.Count);
        float totalWidth = visibleCount * cardSize.X + (visibleCount - 1) * spacing;
        float arrowSpace = _game.Scale.S(ArrowButtonSize + 15f);

        // Strzałka lewa
        bool canScrollLeft = _selectionScrollOffset > 0;
        if (canScrollLeft)
        {
            var arrowSize = _game.Scale.S(ArrowButtonSize);
            var arrowRect = new FloatRect(
                new Vector2f(centerX - totalWidth / 2 - arrowSpace, y + (cardSize.Y - arrowSize) / 2),
                new Vector2f(arrowSize, arrowSize)
            );
            DrawArrowButton(window, arrowRect, true, IsMouseOver(arrowRect));
        }

        // Karty
        float startX = centerX - totalWidth / 2;
        for (int i = 0; i < visibleCount; i++)
        {
            int optionIndex = _selectionScrollOffset + i;
            if (optionIndex >= _selectionOptions.Count) break;

            var option = _selectionOptions[optionIndex];
            float cardX = startX + i * (cardSize.X + spacing);
            var cardPos = new Vector2f(cardX, y);

            // Tint: żółty dla zaznaczonej lub hovered
            bool isSelected = _selectedIndices.Contains(optionIndex);
            bool isHovered = _hoveredSelectionIndex == optionIndex;
            Color? tint = (isSelected || isHovered) ? new Color(255, 255, 180) : null;

            _cardRenderer.Draw(window, option.Card, cardPos, CardDisplayMode.Small, SelectionCardScale, tint);
        }

        // Strzałka prawa
        bool canScrollRight = _selectionScrollOffset + MaxVisibleSelectionCards < _selectionOptions.Count;
        if (canScrollRight)
        {
            var arrowSize = _game.Scale.S(ArrowButtonSize);
            var arrowRect = new FloatRect(
                new Vector2f(centerX + totalWidth / 2 + _game.Scale.S(15f), y + (cardSize.Y - arrowSize) / 2),
                new Vector2f(arrowSize, arrowSize)
            );
            DrawArrowButton(window, arrowRect, false, IsMouseOver(arrowRect));
        }

        // Przycisk "Zatwierdź" (tylko dla multi-select gdy wybrano wymaganą liczbę)
        if (requiredCount > 1 && _selectedIndices.Count == requiredCount)
        {
            var buttonWidth = _game.Scale.S(150f);
            var buttonHeight = _game.Scale.S(40f);
            var buttonRect = new FloatRect(
                new Vector2f(centerX - buttonWidth / 2, y + cardSize.Y + _game.Scale.S(20f)),
                new Vector2f(buttonWidth, buttonHeight)
            );
            DrawConfirmButton(window, buttonRect);
        }
    }

    private void DrawArrowButton(RenderWindow window, FloatRect rect, bool isLeft, bool isHovered)
    {
        var button = new RectangleShape(new Vector2f(rect.Size.X, rect.Size.Y))
        {
            Position = new Vector2f(rect.Position.X, rect.Position.Y),
            FillColor = isHovered ? new Color(80, 80, 90) : new Color(50, 50, 60),
            OutlineColor = new Color(100, 100, 110),
            OutlineThickness = 2f
        };
        window.Draw(button);

        // Tekst strzałki
        string arrow = isLeft ? "<" : ">";
        var text = new Text(_game.Assets.DefaultFont, arrow, _game.Scale.S(Theme.FontSizeLarge))
        {
            FillColor = Color.White
        };
        var bounds = text.GetLocalBounds();
        text.Position = new Vector2f(
            rect.Position.X + (rect.Size.X - bounds.Size.X) / 2,
            rect.Position.Y + (rect.Size.Y - bounds.Size.Y) / 2 - _game.Scale.S(5f)
        );
        window.Draw(text);
    }

    private void DrawConfirmButton(RenderWindow window, FloatRect rect)
    {
        bool isHovered = IsMouseOver(rect);
        var button = new RectangleShape(new Vector2f(rect.Size.X, rect.Size.Y))
        {
            Position = new Vector2f(rect.Position.X, rect.Position.Y),
            FillColor = isHovered ? new Color(60, 120, 60) : new Color(40, 100, 40),
            OutlineColor = new Color(80, 140, 80),
            OutlineThickness = 2f
        };
        window.Draw(button);

        var text = new Text(_game.Assets.DefaultFont, "Zatwierdź", _game.Scale.S(Theme.FontSizeSmall))
        {
            FillColor = Color.White
        };
        var bounds = text.GetLocalBounds();
        text.Position = new Vector2f(
            rect.Position.X + (rect.Size.X - bounds.Size.X) / 2,
            rect.Position.Y + (rect.Size.Y - bounds.Size.Y) / 2 - _game.Scale.S(3f)
        );
        window.Draw(text);
    }

    private bool IsMouseOver(FloatRect rect)
    {
        return rect.Contains(new Vector2f(_mousePosition.X, _mousePosition.Y));
    }

    private bool CheckInlineSelectionHover()
    {
        if (!_inlineSelectionActive) return false;

        _hoveredSelectionIndex = -1;

        var cardSize = _cardRenderer.GetCardSize(SelectionCardScale);
        float spacing = cardSize.X * CardSpacingRatio;
        float y = _game.Scale.S(StatusTextHeight + 100f) + cardSize.Y * 0.5f;
        float centerX = _game.Scale.CurrentWidth / 2;

        int visibleCount = Math.Min(MaxVisibleSelectionCards, _selectionOptions.Count);
        float totalWidth = visibleCount * cardSize.X + (visibleCount - 1) * spacing;
        float startX = centerX - totalWidth / 2;

        for (int i = 0; i < visibleCount; i++)
        {
            int optionIndex = _selectionScrollOffset + i;
            if (optionIndex >= _selectionOptions.Count) break;

            float cardX = startX + i * (cardSize.X + spacing);
            var cardRect = new FloatRect(new Vector2f(cardX, y), cardSize);

            if (cardRect.Contains(new Vector2f(_mousePosition.X, _mousePosition.Y)))
            {
                _hoveredSelectionIndex = optionIndex;
                _hoveredCard = _selectionOptions[optionIndex].Card;
                return true;
            }
        }
        return false;
    }

    private FloatRect GetLeftArrowRect()
    {
        var cardSize = _cardRenderer.GetCardSize(SelectionCardScale);
        float spacing = cardSize.X * CardSpacingRatio;
        float y = _game.Scale.S(StatusTextHeight + 100f) + cardSize.Y * 0.5f;
        float centerX = _game.Scale.CurrentWidth / 2;
        int visibleCount = Math.Min(MaxVisibleSelectionCards, _selectionOptions.Count);
        float totalWidth = visibleCount * cardSize.X + (visibleCount - 1) * spacing;
        float arrowSpace = _game.Scale.S(ArrowButtonSize + 15f);

        var arrowSize = _game.Scale.S(ArrowButtonSize);
        return new FloatRect(
            new Vector2f(centerX - totalWidth / 2 - arrowSpace, y + (cardSize.Y - arrowSize) / 2),
            new Vector2f(arrowSize, arrowSize)
        );
    }

    private FloatRect GetRightArrowRect()
    {
        var cardSize = _cardRenderer.GetCardSize(SelectionCardScale);
        float spacing = cardSize.X * CardSpacingRatio;
        float y = _game.Scale.S(StatusTextHeight + 100f) + cardSize.Y * 0.5f;
        float centerX = _game.Scale.CurrentWidth / 2;
        int visibleCount = Math.Min(MaxVisibleSelectionCards, _selectionOptions.Count);
        float totalWidth = visibleCount * cardSize.X + (visibleCount - 1) * spacing;

        var arrowSize = _game.Scale.S(ArrowButtonSize);
        return new FloatRect(
            new Vector2f(centerX + totalWidth / 2 + _game.Scale.S(15f), y + (cardSize.Y - arrowSize) / 2),
            new Vector2f(arrowSize, arrowSize)
        );
    }

    private FloatRect GetConfirmButtonRect()
    {
        var cardSize = _cardRenderer.GetCardSize(SelectionCardScale);
        float y = _game.Scale.S(StatusTextHeight + 100f) + cardSize.Y * 0.5f;
        float centerX = _game.Scale.CurrentWidth / 2;
        var buttonWidth = _game.Scale.S(150f);
        var buttonHeight = _game.Scale.S(40f);

        return new FloatRect(
            new Vector2f(centerX - buttonWidth / 2, y + cardSize.Y + _game.Scale.S(20f)),
            new Vector2f(buttonWidth, buttonHeight)
        );
    }

    #endregion

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
        float y = _game.Scale.CurrentHeight - _game.Scale.S(HandYOffset);

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

    public void OnResolutionChanged() => UpdateLayout();
}
