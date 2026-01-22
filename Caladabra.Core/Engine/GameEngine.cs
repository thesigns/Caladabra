using Caladabra.Core.Cards;
using Caladabra.Core.Effects;
using Caladabra.Core.Effects.Actions;
using Caladabra.Core.Events;
using Caladabra.Core.State;
using Caladabra.Core.Zones;

namespace Caladabra.Core.Engine;

/// <summary>
/// Silnik gry Caladabra.
/// Zarządza stanem gry i wykonuje akcje gracza.
/// </summary>
public sealed class GameEngine
{
    public GameState State { get; }
    private int _lastProcessedTurn = 0;

    public GameEngine(GameState state)
    {
        State = state;
    }

    /// <summary>
    /// Tworzy nową grę z podaną talią.
    /// </summary>
    public static GameEngine NewGame(List<Card> deck, int? seed = null, bool shouldShuffle = true)
    {
        var state = new GameState();
        var engine = new GameEngine(state);

        // Załaduj CardList
        CardRegistry.Instance.PopulateCardList(state.CardList);

        // Załaduj talię do Spiżarni
        state.Pantry.AddToBottom(deck);

        // Tasuj (chyba że wyłączone - np. talia z pliku)
        if (shouldShuffle)
        {
            // Generuj seed jeśli nie podano
            int actualSeed = seed ?? Random.Shared.Next();
            state.Seed = actualSeed;

            var random = new Random(actualSeed);
            state.Pantry.Shuffle(random);
        }
        // else: state.Seed pozostaje null (custom deck bez tasowania)

        // NIE dobieraj kart tutaj - zrób to przez DrawInitialHand()
        // Pozwala to na generowanie eventów i wywołanie OnDraw

        return engine;
    }

    /// <summary>
    /// Dobiera początkową rękę ze Spiżarni. Wywołuje OnDraw.
    /// Zwraca eventy do animacji (CardDrawnEvent dla każdej karty).
    /// </summary>
    public List<IGameEvent> DrawInitialHand()
    {
        var events = new List<IGameEvent>();

        for (int i = 0; i < GameRules.StartingHandSize; i++)
        {
            if (State.Hand.IsFull) break;

            var card = State.Pantry.Draw();
            if (card == null) break;

            State.Hand.Add(card);
            events.Add(new CardDrawnEvent(card));

            // Wykonaj OnDraw
            if (card.OnDraw != null)
            {
                var context = CreateEffectContext(card, events);
                var result = card.OnDraw.Execute(context);

                if (result is EffectResult.NeedsChoiceResult needsChoice)
                {
                    State.Phase = GamePhase.AwaitingChoice;
                    needsChoice.Choice.EffectTrigger = "OnDraw";
                    State.PendingChoice = needsChoice.Choice;
                    events.Add(new ChoiceRequestedEvent(needsChoice.Choice));
                    break; // Gracz musi wybrać zanim dobierze więcej
                }
            }
        }

        return events;
    }

    /// <summary>
    /// Dokańcza dobieranie początkowej ręki po przerwaniu przez OnDraw choice.
    /// </summary>
    private void CompleteInitialHand(List<IGameEvent> events)
    {
        while (State.Hand.Count < GameRules.StartingHandSize && State.Pantry.Count > 0)
        {
            var card = State.Pantry.Draw();
            if (card == null) break;

            State.Hand.Add(card);
            events.Add(new CardDrawnEvent(card));

            // Wykonaj OnDraw - jeśli wymaga wyboru, przerwij
            if (card.OnDraw != null)
            {
                var context = CreateEffectContext(card, events);
                var result = card.OnDraw.Execute(context);

                if (result is EffectResult.NeedsChoiceResult needsChoice)
                {
                    State.Phase = GamePhase.AwaitingChoice;
                    needsChoice.Choice.EffectTrigger = "OnDraw";
                    State.PendingChoice = needsChoice.Choice;
                    events.Add(new ChoiceRequestedEvent(needsChoice.Choice));
                    break; // Gracz musi wybrać - CompleteInitialHand zostanie wywołane ponownie
                }
            }
        }
    }

    /// <summary>
    /// Sprawdza czy można zagrać kartę o podanym indeksie.
    /// </summary>
    public bool CanPlay(int cardIndex)
    {
        if (State.Phase != GamePhase.AwaitingAction)
            return false;

        var card = State.Hand.GetAt(cardIndex);
        if (card == null)
            return false;

        return State.Willpower >= card.WillpowerCost;
    }

    /// <summary>
    /// Sprawdza czy można zjeść kartę o podanym indeksie.
    /// </summary>
    public bool CanEat(int cardIndex)
    {
        if (State.Phase != GamePhase.AwaitingAction)
            return false;

        return State.Hand.GetAt(cardIndex) != null;
    }

    /// <summary>
    /// Zagrywa kartę z ręki.
    /// </summary>
    public List<IGameEvent> Play(int cardIndex)
    {
        var events = new List<IGameEvent>();

        // Automatycznie przetwórz początek tury (liczniki na stole)
        EnsureTurnStartProcessed(events);

        if (!CanPlay(cardIndex))
        {
            return events;
        }

        var card = State.Hand.RemoveAt(cardIndex)!;
        events.Add(new CardPlayedEvent(card, cardIndex));

        // Zapłać koszt SW
        int oldWP = State.Willpower;
        State.ModifyWillpower(-card.WillpowerCost);
        events.Add(new WillpowerChangedEvent(oldWP, State.Willpower));

        // Wykonaj efekt OnPlay
        if (card.OnPlay != null)
        {
            var context = CreateEffectContext(card, events);
            var result = card.OnPlay.Execute(context);

            if (result is EffectResult.NeedsChoiceResult needsChoice)
            {
                State.Phase = GamePhase.AwaitingChoice;
                needsChoice.Choice.EffectTrigger = "OnPlay";
                needsChoice.Choice.PlayedCard = card;
                State.PendingChoice = needsChoice.Choice;
                events.Add(new ChoiceRequestedEvent(needsChoice.Choice));
                return events;
            }
        }

        // Jeśli karta nie trafiła na stół, idzie do kibelka
        if (!State.Table.Cards.Contains(card))
        {
            State.Toilet.Add(card);
            events.Add(new CardDiscardedEvent(card, ZoneType.Hand));
        }

        // Dobierz kartę (jeśli efekt nie zawiera SkipDraw)
        if (!ContainsSkipDraw(card.OnPlay))
        {
            DrawCards(GameRules.DrawAfterPlay, events);
        }

        // Sprawdź warunki końca gry
        CheckEndConditions(events);

        // Następna tura
        if (State.Phase == GamePhase.AwaitingAction)
        {
            State.Turn++;
        }

        return events;
    }

    /// <summary>
    /// Zjada kartę z ręki.
    /// </summary>
    public List<IGameEvent> Eat(int cardIndex)
    {
        var events = new List<IGameEvent>();

        // Automatycznie przetwórz początek tury (liczniki na stole)
        EnsureTurnStartProcessed(events);

        if (!CanEat(cardIndex))
        {
            return events;
        }

        var card = State.Hand.RemoveAt(cardIndex)!;
        events.Add(new CardEatenEvent(card, cardIndex));

        // Dodaj kalorie (Tłuszcz)
        int oldFat = State.Fat;
        State.ModifyFat(card.Calories);
        events.Add(new FatChangedEvent(oldFat, State.Fat));

        // Odzyskaj SW
        int oldWP = State.Willpower;
        State.ModifyWillpower(card.WillpowerCost);
        events.Add(new WillpowerChangedEvent(oldWP, State.Willpower));

        // Przenieś do żołądka
        var expelled = State.Stomach.Add(card);
        events.Add(new CardMovedEvent(card, ZoneType.Hand, ZoneType.Stomach));

        // Jeśli żołądek był pełny, karta wypadła do kibelka
        if (expelled != null)
        {
            State.Toilet.Add(expelled);
            events.Add(new CardDiscardedEvent(expelled, ZoneType.Stomach));

            // Wykonaj OnDiscard dla wypchniętej karty
            if (expelled.OnDiscard != null)
            {
                var expelledContext = CreateEffectContext(expelled, events);
                expelled.OnDiscard.Execute(expelledContext);
            }
        }

        // Wykonaj efekt OnEat
        if (card.OnEat != null)
        {
            var context = CreateEffectContext(card, events);
            var result = card.OnEat.Execute(context);

            if (result is EffectResult.NeedsChoiceResult needsChoice)
            {
                State.Phase = GamePhase.AwaitingChoice;
                needsChoice.Choice.EffectTrigger = "OnEat";
                State.PendingChoice = needsChoice.Choice;
                events.Add(new ChoiceRequestedEvent(needsChoice.Choice));
                return events;
            }
        }

        // Dobierz karty
        int cardsToDraw = State.Hand.Count >= GameRules.EatDrawThreshold
            ? GameRules.DrawAfterEatWith4Cards
            : GameRules.DrawAfterEatWithLessThan4Cards;

        DrawCards(cardsToDraw, events);

        // Sprawdź warunki końca gry
        CheckEndConditions(events);

        // Następna tura
        if (State.Phase == GamePhase.AwaitingAction)
        {
            State.Turn++;
        }

        return events;
    }

    /// <summary>
    /// Wykonuje wybór gracza.
    /// </summary>
    public List<IGameEvent> Choose(params int[] indices)
    {
        var events = new List<IGameEvent>();

        if (State.Phase != GamePhase.AwaitingChoice || State.PendingChoice == null)
        {
            return events;
        }

        events.Add(new ChoiceMadeEvent(indices));

        var choice = State.PendingChoice;
        var continuation = choice.Continuation;
        State.PendingChoice = null;
        State.Phase = GamePhase.AwaitingAction;

        // Kontynuuj efekt z wybranym indeksem
        var context = new EffectContext
        {
            State = State,
            SourceCard = choice.SourceCard ?? choice.Options.First().Card,
            Events = events,
            ChosenIndices = indices,
            PendingChoice = choice
        };

        // Ustaw ChosenCard jeśli wybrano dokładnie jedną opcję
        if (indices.Length == 1 && indices[0] >= 0 && indices[0] < choice.Options.Count)
        {
            context.ChosenCard = choice.Options[indices[0]].Card;
        }

        var result = continuation.Execute(context);

        if (result is EffectResult.NeedsChoiceResult needsChoice)
        {
            State.Phase = GamePhase.AwaitingChoice;
            State.PendingChoice = needsChoice.Choice;
            events.Add(new ChoiceRequestedEvent(needsChoice.Choice));
            return events;
        }

        // Jeśli to był OnPlay i karta nie trafiła na stół, idzie do kibelka
        if (choice.EffectTrigger == "OnPlay" && choice.PlayedCard != null)
        {
            if (!State.Table.Cards.Contains(choice.PlayedCard))
            {
                State.Toilet.Add(choice.PlayedCard);
                events.Add(new CardDiscardedEvent(choice.PlayedCard, ZoneType.Hand));
            }
        }

        // Dobierz kartę jeśli efekt nie zawiera SkipDraw
        if (!context.ShouldSkipDraw && State.Hand.Count < GameRules.MaxHandSize && State.Pantry.Count > 0)
        {
            // Jeśli to tura 1 i ręka ma mniej niż 5 kart, dokończ początkowe dobieranie
            // (bo OnDraw przerwało DrawInitialHand)
            if (State.Turn == 1 && choice.EffectTrigger == "OnDraw" && State.Hand.Count < GameRules.StartingHandSize)
            {
                CompleteInitialHand(events);
            }
            else
            {
                DrawCards(1, events);
            }
        }

        // Sprawdź czy ręka przekracza limit (efekt uboczny Jasnowidzenia + OnDraw przerwanie)
        if (State.Hand.Count > GameRules.MaxHandSize)
        {
            var extraDrawModifier = State.ActiveModifiers
                .FirstOrDefault(m => m.Type == ModifierType.ExtraDrawThenDiscard);

            if (extraDrawModifier != null)
            {
                // Wymuś wybór odrzucenia jednej karty (powtórzy się jeśli wciąż za dużo)
                var options = State.Hand.Cards.Select((c, idx) => new ChoiceOption
                {
                    Index = idx,
                    Card = c,
                    DisplayText = c.ToString()
                }).ToList();

                var discardChoice = new PendingChoice
                {
                    Type = ChoiceType.DiscardFromHand,
                    Prompt = $"Ręka przepełniona ({State.Hand.Count}/{GameRules.MaxHandSize}). Odrzuć kartę.",
                    Options = options,
                    Continuation = DiscardChosenFromHand.Instance,
                    SourceCard = extraDrawModifier.SourceCard,
                    EffectTrigger = "Discard"
                };

                State.Phase = GamePhase.AwaitingChoice;
                State.PendingChoice = discardChoice;
                events.Add(new ChoiceRequestedEvent(discardChoice));
                return events;
            }
        }

        CheckEndConditions(events);

        return events;
    }

    /// <summary>
    /// Automatycznie przetwarza początek tury, jeśli jeszcze nie był przetworzony.
    /// Wywoływane na początku Play() i Eat().
    /// </summary>
    private void EnsureTurnStartProcessed(List<IGameEvent> events)
    {
        if (State.Turn > _lastProcessedTurn)
        {
            var turnEvents = ProcessStartOfTurn();
            events.AddRange(turnEvents);
            _lastProcessedTurn = State.Turn;
        }
    }

    /// <summary>
    /// Przetwarza początek tury (liczniki na stole).
    /// </summary>
    public List<IGameEvent> ProcessStartOfTurn()
    {
        var events = new List<IGameEvent>();
        events.Add(new TurnStartedEvent(State.Turn));

        // Zmniejsz liczniki kart na stole
        var expiredCards = State.Table.TickCounters();

        foreach (var entry in State.Table.Entries.ToList())
        {
            var card = entry.Card;

            // Emituj event tick
            if (entry.TurnsRemaining.HasValue)
            {
                events.Add(new TableCounterTickedEvent(card, entry.TurnsRemaining.Value + 1, entry.TurnsRemaining.Value));
            }

            // Wykonaj OnTurnOnTable
            if (card.OnTurnOnTable != null)
            {
                var context = CreateEffectContext(card, events);
                card.OnTurnOnTable.Execute(context);
            }
        }

        // Usuń wygasłe karty
        foreach (var card in expiredCards)
        {
            State.Table.Remove(card);

            // Wykonaj OnTableCounterZero
            if (card.OnTableCounterZero != null)
            {
                var context = CreateEffectContext(card, events);
                card.OnTableCounterZero.Execute(context);
            }

            // Wykonaj OnLeaveTable
            if (card.OnLeaveTable != null)
            {
                var context = CreateEffectContext(card, events);
                card.OnLeaveTable.Execute(context);
            }

            // Dodaj do Kibelka tylko jeśli efekt nie przeniósł karty gdzie indziej
            if (!State.Pantry.Cards.Contains(card) && !State.Hand.Cards.Contains(card))
            {
                State.Toilet.Add(card);
                events.Add(new CardDiscardedEvent(card, ZoneType.Table));
            }
        }

        return events;
    }

    // === Metody pomocnicze ===

    private EffectContext CreateEffectContext(Card card, List<IGameEvent> events)
    {
        return new EffectContext
        {
            State = State,
            SourceCard = card,
            Events = events
        };
    }

    private void DrawCards(int count, List<IGameEvent> events)
    {
        // Oblicz modyfikatory
        int caloriesReduction = State.ActiveModifiers
            .Where(m => m.Type == ModifierType.ReduceCaloriesOnDraw)
            .Sum(m => m.Value);

        int extraDraw = State.ActiveModifiers
            .Where(m => m.Type == ModifierType.ExtraDrawThenDiscard)
            .Sum(m => m.Value);

        int totalToDraw = count + extraDraw;
        var drawnCards = new List<Cards.Card>(); // Śledź dobrane karty dla Jasnowidzenia

        for (int i = 0; i < totalToDraw; i++)
        {
            // Przy Jasnowidzeniu pozwól na tymczasowe przepełnienie ręki
            if (State.Hand.IsFull && extraDraw == 0)
                break;

            var card = State.Pantry.Draw();
            if (card == null)
                break;

            // Zastosuj redukcję kalorii (trwale)
            if (caloriesReduction > 0)
            {
                card.Calories = Math.Max(0, card.Calories - caloriesReduction);
            }

            // Przy Jasnowidzeniu ignoruj limit ręki (tymczasowe przepełnienie)
            State.Hand.Add(card, ignoreLimit: extraDraw > 0);
            drawnCards.Add(card);
            events.Add(new CardDrawnEvent(card));

            // Wykonaj OnDraw
            if (card.OnDraw != null)
            {
                var context = CreateEffectContext(card, events);
                var result = card.OnDraw.Execute(context);

                // Jeśli OnDraw wymaga wyboru (np. Kwantowa próżnia)
                if (result is EffectResult.NeedsChoiceResult needsChoice)
                {
                    State.Phase = GamePhase.AwaitingChoice;
                    needsChoice.Choice.EffectTrigger = "OnDraw";
                    State.PendingChoice = needsChoice.Choice;
                    events.Add(new ChoiceRequestedEvent(needsChoice.Choice));
                    return; // Przerwij dobieranie, wróć po wyborze
                }
            }
        }

        // Jeśli dobrano dodatkowe karty przez Jasnowidzenie - wybierz którą zachować
        if (extraDraw > 0 && drawnCards.Count > count)
        {
            // Opcje tylko z nowo dobranych kart
            var options = drawnCards.Select((c, idx) => new ChoiceOption
            {
                Index = idx,
                Card = c,
                DisplayText = c.ToString()
            }).ToList();

            var choice = new PendingChoice
            {
                Type = ChoiceType.DiscardFromHand,
                Prompt = "Jasnowidzenie: wybierz kartę do zachowania.",
                Options = options,
                Continuation = KeepChosenDiscardRest.Instance,
                SourceCard = State.ActiveModifiers
                    .FirstOrDefault(m => m.Type == ModifierType.ExtraDrawThenDiscard)?.SourceCard,
                EffectTrigger = "Discard" // Specjalny marker dla odtwarzania z JSON
            };

            State.Phase = GamePhase.AwaitingChoice;
            State.PendingChoice = choice;
            events.Add(new ChoiceRequestedEvent(choice));
        }
    }

    private void CheckEndConditions(List<IGameEvent> events)
    {
        if (State.Phase == GamePhase.Won || State.Phase == GamePhase.Lost)
            return;

        if (State.CheckWinCondition())
        {
            State.Phase = GamePhase.Won;
            events.Add(new GameWonEvent(State.Turn));
        }
        else if (State.CheckLoseCondition())
        {
            State.Phase = GamePhase.Lost;
            var reason = State.Turn > GameRules.MaxTurns
                ? $"Minęło {GameRules.MaxTurns} dni diety. Tłuszcz: {State.Fat}."
                : "Brak kart w ręce i spiżarni.";
            events.Add(new GameLostEvent(State.Turn, reason));
        }
    }

    private bool ContainsSkipDraw(IEffect? effect)
    {
        if (effect == null)
            return false;

        if (effect is SkipDraw)
            return true;

        if (effect is Sequence sequence)
        {
            // Sprawdź czy Sequence zawiera SkipDraw
            // Na razie uproszczone - nie sprawdzamy rekurencyjnie
        }

        return false;
    }
}
