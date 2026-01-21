using Caladabra.Core.Engine;
using Caladabra.Core.Events;
using Caladabra.Core.State;

namespace Caladabra.Desktop.Integration;

/// <summary>
/// Most między warstwą Desktop a Core.
/// Zarządza silnikiem gry i przekazuje eventy do przetwarzania.
/// </summary>
public sealed class GameController
{
    private readonly GameEngine _engine;
    private readonly List<IGameEvent> _pendingEvents = new();

    public GameState State => _engine.State;
    public int? Seed => State.Seed;
    public IReadOnlyList<IGameEvent> PendingEvents => _pendingEvents;
    public bool IsGameOver => State.Phase == GamePhase.Won || State.Phase == GamePhase.Lost;
    public bool IsAwaitingChoice => State.Phase == GamePhase.AwaitingChoice;

    public GameController(GameEngine engine)
    {
        _engine = engine;
    }

    /// <summary>
    /// Tworzy nową grę z domyślną talią.
    /// </summary>
    public static GameController NewGame(int? seed = null)
    {
        var deck = DeckBuilder.BuildPrototypeDeck();
        var engine = GameEngine.NewGame(deck, seed);
        var controller = new GameController(engine);

        // Dobierz początkową rękę (z eventami dla animacji i OnDraw)
        var initialEvents = engine.DrawInitialHand();
        controller._pendingEvents.AddRange(initialEvents);

        return controller;
    }

    /// <summary>
    /// Zagraj kartę z ręki.
    /// </summary>
    public bool PlayCard(int handIndex)
    {
        if (!CanPlayCard(handIndex)) return false;

        var events = _engine.Play(handIndex);
        _pendingEvents.AddRange(events);
        return true;
    }

    /// <summary>
    /// Zjedz kartę z ręki.
    /// </summary>
    public bool EatCard(int handIndex)
    {
        if (!CanEatCard(handIndex)) return false;

        var events = _engine.Eat(handIndex);
        _pendingEvents.AddRange(events);
        return true;
    }

    /// <summary>
    /// Dokonaj wyboru (gdy gra czeka na decyzję).
    /// </summary>
    public bool MakeChoice(int choiceIndex)
    {
        if (!IsAwaitingChoice) return false;
        if (State.PendingChoice == null) return false;
        if (choiceIndex < 0 || choiceIndex >= State.PendingChoice.Options.Count) return false;

        var events = _engine.Choose(choiceIndex);
        _pendingEvents.AddRange(events);
        return true;
    }

    /// <summary>
    /// Sprawdza czy można zagrać kartę.
    /// </summary>
    public bool CanPlayCard(int handIndex)
    {
        if (State.Phase != GamePhase.AwaitingAction) return false;
        if (handIndex < 0 || handIndex >= State.Hand.Count) return false;
        return _engine.CanPlay(handIndex);
    }

    /// <summary>
    /// Sprawdza czy można zjeść kartę.
    /// </summary>
    public bool CanEatCard(int handIndex)
    {
        if (State.Phase != GamePhase.AwaitingAction) return false;
        if (handIndex < 0 || handIndex >= State.Hand.Count) return false;
        return true;  // Zawsze można zjeść kartę
    }

    /// <summary>
    /// Pobiera i czyści kolejkę eventów do przetworzenia.
    /// </summary>
    public IReadOnlyList<IGameEvent> FlushEvents()
    {
        var events = _pendingEvents.ToList();
        _pendingEvents.Clear();
        return events;
    }

    /// <summary>
    /// Pobiera szczegóły wygranej/przegranej.
    /// </summary>
    public string GetGameOverMessage()
    {
        return State.Phase switch
        {
            GamePhase.Won => $"Gratulacje! Wygrałeś w {State.Turn} turach. Pozostały tłuszcz: {State.Fat}",
            GamePhase.Lost => State.Fat <= 0
                ? "Przegrałeś - tłuszcz spadł do zera!"
                : "Przegrałeś - skończyły się karty w ręce!",
            _ => ""
        };
    }
}
