using System.Text.Json;
using System.Text.Json.Serialization;
using Caladabra.Console.Rendering;
using Caladabra.Core.Cards;
using Caladabra.Core.Cards.Definitions;
using Caladabra.Core.Effects;
using Caladabra.Core.Effects.Actions;
using Caladabra.Core.Engine;
using Caladabra.Core.State;
using Caladabra.Core.Zones;

namespace Caladabra.Console;

/// <summary>
/// Runner JSON dla trybu turn-by-turn (AI-friendly).
/// </summary>
public static class JsonRunner
{
    private const string DefaultStatePath = "game.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public static void NewGame(string? statePath, int? seed, string? deckPath = null)
    {
        statePath ??= DefaultStatePath;

        CardDefinitions.RegisterAll();

        List<Card> deck;
        bool shouldShuffle;

        if (!string.IsNullOrEmpty(deckPath))
        {
            // Talia z pliku - bez tasowania (kolejność z pliku)
            deck = DeckBuilder.BuildDeckFromFile(deckPath);
            shouldShuffle = false;
        }
        else
        {
            // Standardowa talia - tasowanie
            deck = DeckBuilder.BuildPrototypeDeck();
            shouldShuffle = true;
        }

        var engine = GameEngine.NewGame(deck, seed, shouldShuffle);
        engine.DrawInitialHand();  // Dobierz początkową rękę (wywołuje OnDraw)

        SaveState(engine.State, statePath);
        var message = deckPath != null
            ? $"Nowa gra rozpoczęta z talią z pliku: {deckPath} ({deck.Count} kart)"
            : "Nowa gra rozpoczęta.";
        OutputState(engine.State, message);
    }

    public static void Status(string? statePath)
    {
        statePath ??= DefaultStatePath;

        var state = LoadState(statePath);
        if (state == null)
        {
            OutputError("Brak zapisanej gry. Użyj 'new' aby rozpocząć.");
            return;
        }

        OutputState(state, null);
    }

    public static void Play(string? statePath, int index)
    {
        statePath ??= DefaultStatePath;

        var state = LoadState(statePath);
        if (state == null)
        {
            OutputError("Brak zapisanej gry. Użyj 'new' aby rozpocząć.");
            return;
        }

        var engine = new GameEngine(state);

        // Przetwórz początek tury jeśli trzeba
        if (state.Phase == GamePhase.AwaitingAction)
        {
            engine.ProcessStartOfTurn();
        }

        if (!engine.CanPlay(index - 1))
        {
            OutputError($"Nie możesz zagrać karty {index}. Za mało SW lub nieprawidłowy indeks.");
            return;
        }

        var card = state.Hand.GetAt(index - 1);
        engine.Play(index - 1);

        SaveState(state, statePath);
        OutputState(state, $"Zagrałeś kartę: {card?.Name}");
    }

    public static void Eat(string? statePath, int index)
    {
        statePath ??= DefaultStatePath;

        var state = LoadState(statePath);
        if (state == null)
        {
            OutputError("Brak zapisanej gry. Użyj 'new' aby rozpocząć.");
            return;
        }

        var engine = new GameEngine(state);

        // Przetwórz początek tury jeśli trzeba
        if (state.Phase == GamePhase.AwaitingAction)
        {
            engine.ProcessStartOfTurn();
        }

        if (!engine.CanEat(index - 1))
        {
            OutputError($"Nie możesz zjeść karty {index}.");
            return;
        }

        var card = state.Hand.GetAt(index - 1);
        engine.Eat(index - 1);

        SaveState(state, statePath);
        OutputState(state, $"Zjadłeś kartę: {card?.Name}. Tłuszcz +{card?.Calories}");
    }

    public static void Choose(string? statePath, int index)
    {
        statePath ??= DefaultStatePath;

        var state = LoadState(statePath);
        if (state == null)
        {
            OutputError("Brak zapisanej gry. Użyj 'new' aby rozpocząć.");
            return;
        }

        if (state.Phase != GamePhase.AwaitingChoice || state.PendingChoice == null)
        {
            OutputError("Nie ma teraz żadnej decyzji do podjęcia.");
            return;
        }

        var engine = new GameEngine(state);
        engine.Choose(index - 1);

        SaveState(state, statePath);
        OutputState(state, $"Wybrano opcję {index}.");
    }

    public static void Info(string cardName)
    {
        CardDefinitions.RegisterAll();
        var registry = CardRegistry.Instance;

        var card = registry.GetAll()
            .FirstOrDefault(c => c.Name.Contains(cardName, StringComparison.OrdinalIgnoreCase));

        if (card == null)
        {
            OutputError($"Nie znaleziono karty: {cardName}");
            return;
        }

        var dto = CardToDto(card, 0);
        System.Console.WriteLine(JsonSerializer.Serialize(dto, JsonOptions));
    }

    // === Serializacja stanu ===

    private static void SaveState(GameState state, string path)
    {
        var dto = StateToDto(state);
        var json = JsonSerializer.Serialize(dto, JsonOptions);
        File.WriteAllText(path, json);
    }

    private static GameState? LoadState(string path)
    {
        if (!File.Exists(path))
            return null;

        try
        {
            var json = File.ReadAllText(path);
            var dto = JsonSerializer.Deserialize<GameStateDto>(json, JsonOptions);
            if (dto == null) return null;

            // Odtwórz stan z DTO
            CardDefinitions.RegisterAll();
            return DtoToState(dto);
        }
        catch
        {
            return null;
        }
    }

    private static void OutputState(GameState state, string? message)
    {
        var dto = StateToDto(state);
        dto.LastAction = message;
        System.Console.WriteLine(JsonSerializer.Serialize(dto, JsonOptions));
    }

    private static void OutputError(string message)
    {
        var error = new { error = message };
        System.Console.WriteLine(JsonSerializer.Serialize(error, JsonOptions));
    }

    // === Konwersja State <-> DTO ===

    private static GameStateDto StateToDto(GameState state)
    {
        return new GameStateDto
        {
            Turn = state.Turn,
            Phase = state.Phase.ToString(),
            Fat = state.Fat,
            Willpower = state.Willpower,
            Seed = state.Seed,
            Hand = state.Hand.Cards.Select((c, i) => CardToDto(c, i + 1)).ToList(),
            Table = state.Table.Entries.Select((e, i) => new TableEntryDto
            {
                Index = i + 1,
                Card = CardToDto(e.Card, i + 1),
                TurnsRemaining = e.TurnsRemaining
            }).ToList(),
            Stomach = state.Stomach.Cards.Select((c, i) => CardToDto(c, i + 1)).ToList(),
            Pantry = new PantryDto
            {
                Count = state.Pantry.Count,
                TopFlavor = state.Pantry.TopCardFlavor?.ToString()
            },
            Toilet = new ToiletDto { Count = state.Toilet.Count },
            PendingChoice = state.PendingChoice != null ? new PendingChoiceDto
            {
                Prompt = state.PendingChoice.Prompt,
                Options = state.PendingChoice.Options.Select(o => new ChoiceOptionDto
                {
                    Index = o.Index + 1,
                    DisplayText = o.DisplayText,
                    CardId = o.Card.Id
                }).ToList(),
                ChoiceType = state.PendingChoice.Type.ToString(),
                FlavorFilter = state.PendingChoice.FlavorFilter?.ToString(),
                SourceCardId = state.PendingChoice.SourceCard?.Id,
                EffectTrigger = state.PendingChoice.EffectTrigger,
                PlayedCardId = state.PendingChoice.PlayedCard?.Id
            } : null,
            GameOver = state.Phase == GamePhase.Won || state.Phase == GamePhase.Lost,
            Won = state.Phase == GamePhase.Won,

            // Zapisz pełny stan do odtworzenia
            _pantryCards = state.Pantry.Cards.Select(c => c.Id).ToList(),
            _handCards = state.Hand.Cards.Select(c => c.Id).ToList(),
            _tableCards = state.Table.Entries.Select(e => new TableCardDto
            {
                Id = e.Card.Id,
                TurnsRemaining = e.TurnsRemaining
            }).ToList(),
            _stomachCards = state.Stomach.Cards.Select(c => c.Id).ToList(),
            _toiletCards = state.Toilet.Cards.Select(c => c.Id).ToList(),
            _activeModifiers = state.ActiveModifiers.Select(m => new ActiveModifierDto
            {
                Type = m.Type.ToString(),
                Value = m.Value,
                SourceCardId = m.SourceCard.Id
            }).ToList()
        };
    }

    private static GameState DtoToState(GameStateDto dto)
    {
        var state = new GameState
        {
            Turn = dto.Turn,
            Phase = Enum.Parse<GamePhase>(dto.Phase),
            Fat = dto.Fat,
            Willpower = dto.Willpower,
            Seed = dto.Seed
        };

        var registry = CardRegistry.Instance;

        // Odtwórz karty w strefach
        foreach (var id in dto._pantryCards ?? [])
        {
            var card = registry.CloneCard(id);
            state.Pantry.AddToTop(card);
        }

        foreach (var id in dto._handCards ?? [])
        {
            var card = registry.CloneCard(id);
            state.Hand.Add(card);
        }

        foreach (var tableCard in dto._tableCards ?? [])
        {
            var card = registry.CloneCard(tableCard.Id);
            state.Table.Add(card, tableCard.TurnsRemaining);
        }

        foreach (var id in dto._stomachCards ?? [])
        {
            var card = registry.CloneCard(id);
            state.Stomach.Add(card);
        }

        foreach (var id in dto._toiletCards ?? [])
        {
            var card = registry.CloneCard(id);
            state.Toilet.Add(card);
        }

        // Załaduj CardList przez registry
        registry.PopulateCardList(state.CardList);

        // Odtwórz ActiveModifiers
        foreach (var modDto in dto._activeModifiers ?? [])
        {
            // Znajdź kartę źródłową na stole
            var sourceCard = state.Table.Cards.FirstOrDefault(c => c.Id == modDto.SourceCardId);
            if (sourceCard != null)
            {
                state.ActiveModifiers.Add(new ActiveModifier
                {
                    Type = Enum.Parse<ModifierType>(modDto.Type),
                    Value = modDto.Value,
                    SourceCard = sourceCard
                });
            }
        }

        // Odtwórz PendingChoice jeśli istnieje
        if (dto.PendingChoice != null && state.Phase == GamePhase.AwaitingChoice)
        {
            state.PendingChoice = RestorePendingChoice(dto.PendingChoice, state, registry);
        }

        return state;
    }

    private static PendingChoice? RestorePendingChoice(PendingChoiceDto dto, GameState state, CardRegistry registry)
    {
        // Określ ZoneType z ChoiceType
        var choiceType = Enum.Parse<ChoiceType>(dto.ChoiceType ?? "SelectFromHand");

        // Parsuj flavor filter
        Flavor? flavorFilter = null;
        if (!string.IsNullOrEmpty(dto.FlavorFilter))
        {
            flavorFilter = Enum.Parse<Flavor>(dto.FlavorFilter);
        }

        // Odtwórz opcje z kartami
        var options = dto.Options.Select(o =>
        {
            Card card;
            if (!string.IsNullOrEmpty(o.CardId))
            {
                card = registry.CloneCard(o.CardId);
            }
            else
            {
                // Fallback - stwórz placeholder
                card = new Card
                {
                    Id = "unknown",
                    Name = o.DisplayText,
                    Flavor = Flavor.Umami,
                    WillpowerCost = 0,
                    Calories = 0
                };
            }

            return new ChoiceOption
            {
                Index = o.Index - 1, // Konwertuj z powrotem do 0-indexed
                Card = card,
                DisplayText = o.DisplayText
            };
        }).ToList();

        // Odtwórz pełny efekt z karty źródłowej
        IEffect continuation;
        Card? sourceCard = null;

        if (!string.IsNullOrEmpty(dto.SourceCardId))
        {
            sourceCard = registry.CloneCard(dto.SourceCardId);

            // Użyj pełnego efektu karty jako kontynuacji
            continuation = dto.EffectTrigger switch
            {
                "OnPlay" => sourceCard.OnPlay!,
                "OnEat" => sourceCard.OnEat!,
                "OnDraw" => sourceCard.OnDraw!,
                "Discard" => DiscardChosenFromHand.Instance, // Jasnowidzenie - odrzucenie karty
                _ => sourceCard.OnPlay!
            };
        }
        else
        {
            // Fallback - stary sposób (tylko ChooseCardFromZone bez reszty efektu)
            var zoneType = choiceType switch
            {
                ChoiceType.SelectFromHand => ZoneType.Hand,
                ChoiceType.SelectFromTable => ZoneType.Table,
                ChoiceType.SelectFromStomach => ZoneType.Stomach,
                ChoiceType.SelectFromToilet => ZoneType.Toilet,
                ChoiceType.SelectFromCardList => ZoneType.CardList,
                ChoiceType.SelectFromCardListFiltered => ZoneType.CardList,
                _ => ZoneType.Hand
            };
            continuation = new ChooseCardFromZone(zoneType, dto.Prompt, flavorFilter);
        }

        // Odtwórz PlayedCard jeśli istnieje
        Card? playedCard = null;
        if (!string.IsNullOrEmpty(dto.PlayedCardId))
        {
            playedCard = registry.CloneCard(dto.PlayedCardId);
        }

        return new PendingChoice
        {
            Type = choiceType,
            Prompt = dto.Prompt,
            Options = options,
            FlavorFilter = flavorFilter,
            Continuation = continuation,
            SourceCard = sourceCard,
            EffectTrigger = dto.EffectTrigger,
            PlayedCard = playedCard
        };
    }

    private static CardDto CardToDto(Card card, int index)
    {
        return new CardDto
        {
            Index = index,
            Id = card.Id,
            Name = card.Name,
            Flavor = card.Flavor.ToString(),
            Cost = card.WillpowerCost,
            Calories = card.Calories,
            Instruction = card.Instruction
        };
    }
}

// === DTO ===

public class GameStateDto
{
    public int Turn { get; set; }
    public string Phase { get; set; } = "";
    public int Fat { get; set; }
    public int Willpower { get; set; }
    public int? Seed { get; set; }
    public List<CardDto> Hand { get; set; } = [];
    public List<TableEntryDto> Table { get; set; } = [];
    public List<CardDto> Stomach { get; set; } = [];
    public PantryDto Pantry { get; set; } = new();
    public ToiletDto Toilet { get; set; } = new();
    public PendingChoiceDto? PendingChoice { get; set; }
    public bool GameOver { get; set; }
    public bool Won { get; set; }
    public string? LastAction { get; set; }

    // Wewnętrzne pola do persystencji
    public List<string>? _pantryCards { get; set; }
    public List<string>? _handCards { get; set; }
    public List<TableCardDto>? _tableCards { get; set; }
    public List<string>? _stomachCards { get; set; }
    public List<string>? _toiletCards { get; set; }
    public List<ActiveModifierDto>? _activeModifiers { get; set; }
}

public class CardDto
{
    public int Index { get; set; }
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Flavor { get; set; } = "";
    public int Cost { get; set; }
    public int Calories { get; set; }
    public string Instruction { get; set; } = "";
}

public class TableEntryDto
{
    public int Index { get; set; }
    public CardDto Card { get; set; } = new();
    public int? TurnsRemaining { get; set; }
}

public class TableCardDto
{
    public string Id { get; set; } = "";
    public int? TurnsRemaining { get; set; }
}

public class PantryDto
{
    public int Count { get; set; }
    public string? TopFlavor { get; set; }
}

public class ToiletDto
{
    public int Count { get; set; }
}

public class PendingChoiceDto
{
    public string Prompt { get; set; } = "";
    public List<ChoiceOptionDto> Options { get; set; } = [];

    // Pola do odtworzenia efektu
    public string? ChoiceType { get; set; }
    public string? ZoneType { get; set; }
    public string? FlavorFilter { get; set; }

    // Nowe pola do pełnego odtworzenia efektu
    public string? SourceCardId { get; set; }
    public string? EffectTrigger { get; set; } // "OnPlay", "OnEat", etc.
    public string? PlayedCardId { get; set; }
}

public class ChoiceOptionDto
{
    public int Index { get; set; }
    public string DisplayText { get; set; } = "";
    public string? CardId { get; set; }
}

public class ActiveModifierDto
{
    public string Type { get; set; } = "";
    public int Value { get; set; }
    public string SourceCardId { get; set; } = "";
}
