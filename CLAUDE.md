# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands

```bash
# Build entire solution
dotnet build

# Run Desktop (graphical - SFML.Net)
dotnet run --project Caladabra.Desktop

# Run Console interactive mode
dotnet run --project Caladabra.Console -- interactive

# Run Console JSON mode (AI-friendly, turn-by-turn)
dotnet run --project Caladabra.Console -- new [--seed N] [--deck file.json]
dotnet run --project Caladabra.Console -- play N
dotnet run --project Caladabra.Console -- eat N
dotnet run --project Caladabra.Console -- choose N
dotnet run --project Caladabra.Console -- status

# Test with custom deck (cards in exact order, no shuffle)
dotnet run --project Caladabra.Console -- new --deck test_deck.json
```

## Project Structure

```
Caladabra/
├── Caladabra.Core/          # Game logic library (.NET 9)
├── Caladabra.Console/       # Console app (interactive + JSON API)
├── Caladabra.Desktop/       # SFML.Net 3.0.0 graphical client
└── Docs/CaladabraGDD.md     # Game Design Document (rules, cards)
```

## Architecture: Core First

**If something can be fixed in Core, fix it in Core - not in Desktop/Console.**

- `Core` contains all game logic and should work independently of UI
- `Desktop` and `Console` are thin presentation layers
- Bug in game mechanics → fix in `Core`
- Need a UI workaround → reconsider if it belongs in `Core`

## Key Files

| File | Purpose |
|------|---------|
| `Core/Engine/GameEngine.cs` | Main engine: `Play()`, `Eat()`, `Choose()`, `DrawCards()` |
| `Core/Engine/GameRules.cs` | Constants: StartingFat=100, MaxWillpower=30, MaxTurns=30 |
| `Core/State/GameState.cs` | Full game state (Fat, Willpower, zones, ActiveModifiers) |
| `Core/State/PendingChoice.cs` | Player choice system |
| `Core/Cards/Definitions/CardDefinitions.cs` | All 18 card definitions |
| `Core/Cards/CardRegistry.cs` | Singleton card registry |
| `Desktop/Scenes/GameScene.cs` | Main gameplay scene |
| `Desktop/Integration/GameController.cs` | GameEngine wrapper for UI |
| `Desktop/Animation/AnimationManager.cs` | Animation queue, interaction blocking |

## Card Effect System

Hybrid composition pattern in `Core/Effects/`:

```csharp
// Sequence of effects
OnPlay = new Sequence(
    new ReduceFat(6),
    new ChooseCardFromZone(ZoneType.Table, "Prompt:",
        continuation: new DiscardChosenCard(ZoneType.Table))
)

// Conditional effect
OnTurnOnTable = new Conditional(
    new HasFlavorsInZone(ZoneType.Stomach, Flavor.Sweet, Flavor.Sour),
    thenEffect: new GainWillpower(8),
    elseEffect: new Sequence(new LoseWillpower(2), new GainFat(2))
)
```

### Effect Building Blocks (`Core/Effects/Actions/`)

| Effect | Description |
|--------|-------------|
| `ReduceFat(n)` / `GainFat(n)` | Modify Fat |
| `GainWillpower(n)` / `LoseWillpower(n)` | Modify Willpower |
| `PlaceOnTable(turns)` | Place card on Table with counter (-1 = permanent) |
| `Sequence(...)` | Execute effects in order |
| `Conditional(cond, then, else)` | Conditional execution |
| `ChooseCardFromZone` | Request player choice |
| `AddModifier(type, value)` | Add continuous modifier |
| `RemoveModifiersFromSource` | Remove source card's modifiers |

### Conditions (`Core/Effects/Conditions/`)

- `HasFlavorInZone(zone, flavor)` - flavor exists in zone
- `HasFlavorsInZone(zone, f1, f2)` - BOTH flavors exist
- `CountUniqueFlavorsInZone(zone, min)` - unique flavor count

### Continuous Modifiers

Cards on Table can have ongoing effects via `ModifierType`:
- `ExtraDrawThenDiscard` - draw extra cards, choose which one to keep (rest discarded via `KeepChosenDiscardRest`)
- `ReduceCaloriesOnDraw` - reduce calories on drawn cards

```csharp
OnEnterTable = new AddModifier(ModifierType.ExtraDrawThenDiscard, 1),
OnLeaveTable = RemoveModifiersFromSource.Instance
```

## Choice System Flow

1. `ChooseCardFromZone.Execute()` returns `EffectResult.NeedsChoice`
2. `GameEngine` sets `State.Phase = AwaitingChoice` and `State.PendingChoice`
3. UI displays options, player selects
4. `GameEngine.Choose(index)` continues with `PendingChoice.Continuation`

## Card Triggers

```csharp
OnPlay           // After playing
OnEat            // After eating
OnDraw           // After drawing to hand
OnEnterTable     // After placed on table
OnLeaveTable     // After removed from table (any reason)
OnTurnOnTable    // Each turn while on table
OnTableCounterZero // When counter reaches 0
OnDiscard        // After going to toilet
```

## Events System

`GameEngine` emits events (`Core/Events/`) for UI animations:
- `CardPlayedEvent`, `CardEatenEvent`, `CardDrawnEvent`
- `FatChangedEvent`, `WillpowerChangedEvent`
- `CardMovedEvent`, `CardDiscardedEvent`
- `ChoiceRequestedEvent`, `ChoiceMadeEvent`
- `TurnStartedEvent`, `TableCounterTickedEvent`
- `GameWonEvent`, `GameLostEvent`

## Critical: JSON Serialization Pitfalls

**Never compare card references after JSON restore** - cards are cloned from `CardRegistry`:

```csharp
// WRONG - fails after JSON restore
var index = hand.Cards.IndexOf(context.SourceCard);  // returns -1!

// CORRECT - use ID or stored indices
var index = hand.Cards.ToList().FindIndex(c => c.Id == context.SourceCard.Id);
context.ChosenIndices[0]  // use saved index
```

**Always set `EffectTrigger` on `PendingChoice`** - needed for continuation restore:
- `"OnPlay"` → `sourceCard.OnPlay`
- `"OnEat"` → `sourceCard.OnEat`
- `"OnDraw"` → `sourceCard.OnDraw`
- `"Discard"` → `DiscardChosenFromHand.Instance`

## Custom Deck Order

Cards loaded via `--deck` go to Pantry with `AddToBottom`. Drawing takes from top.

**Last cards in JSON = first cards drawn:**
```json
[
  "some_card",        // drawn last (bottom of deck)
  "another_card",     // ...
  "jasnowidzenie"     // drawn first (top of deck, goes to starting hand)
]
```

Last 5 cards in JSON become the starting hand.

## Debugging Tips

1. Check `State.Phase` when game seems stuck on choice
2. Check `State.PendingChoice.EffectTrigger` is set correctly
3. Use `--state custom.json` to avoid overwriting `game.json`
4. Compare JSON state before/after actions

## Common Issues

- **Card not going to Toilet** → effect moved it elsewhere (Table, Pantry)
- **Modifier not working** → check `OnEnterTable` adds and `OnLeaveTable` removes
- **Choice fails after JSON restore** → check `EffectTrigger` and `RestorePendingChoice`
- **Card "disappears"** → reference comparison instead of ID comparison

## Conventions

- Code language: English (class names, methods)
- UI/messages language: Polish
- Card IDs: snake_case (e.g., `wyprawa_do_toalety`)
- Zones: `CardList`, `Pantry`, `Hand`, `Table`, `Stomach`, `Toilet`
- Flavors: `Salty`, `Sweet`, `Bitter`, `Spicy`, `Sour`, `Umami`
