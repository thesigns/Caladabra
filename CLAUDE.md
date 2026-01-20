# Caladabra - Notatki Techniczne

> Zasady gry i mechaniki opisane są w `Docs/CaladabraGDD.md` - nie duplikuj tutaj.

## Struktura Projektu

```
Caladabra/
├── Caladabra.Core/          # Logika gry (biblioteka)
├── Caladabra.Console/       # Aplikacja konsolowa
├── Caladabra.Desktop/       # SFML.Net (Faza 2 - niezaimplementowane)
└── Docs/CaladabraGDD.md     # Game Design Document
```

## Kluczowe Pliki

| Plik | Opis |
|------|------|
| `Core/Engine/GameEngine.cs` | Silnik gry - `Play()`, `Eat()`, `Choose()` |
| `Core/Engine/GameRules.cs` | Stałe (StartingFat=100, MaxWillpower=30, etc.) |
| `Core/State/GameState.cs` | Pełny stan gry (Fat, Willpower, strefy) |
| `Core/State/PendingChoice.cs` | System decyzji gracza |
| `Core/Cards/Definitions/CardDefinitions.cs` | Definicje wszystkich 18 kart |
| `Core/Cards/CardRegistry.cs` | Singleton z rejestrem kart |
| `Console/Program.cs` | Punkt wejścia, tryby: interactive, test, JSON |
| `Console/JsonRunner.cs` | Runner JSON dla trybu turn-by-turn (AI-friendly) |

## Architektura Efektów Kart

Hybrydowy system kompozycji:

```csharp
// Przykład karty z sekwencją efektów
OnPlay = new Sequence(
    new ReduceFat(6),
    new ChooseCardFromZone(ZoneType.Table, "Prompt:",
        continuation: new DiscardChosenCard(ZoneType.Table))
)

// Przykład warunkowego efektu (OnTurnOnTable)
OnTurnOnTable = new Conditional(
    new HasFlavorsInZone(ZoneType.Stomach, Flavor.Sweet, Flavor.Sour),
    thenEffect: new GainWillpower(8),
    elseEffect: new Sequence(new LoseWillpower(2), new GainFat(2))
)
```

### Dostępne Klocki Efektów (`Core/Effects/Actions/`)

| Klocek | Opis |
|--------|------|
| `ReduceFat(n)` | -n Tłuszczu |
| `GainFat(n)` | +n Tłuszczu |
| `GainWillpower(n)` | +n SW |
| `LoseWillpower(n)` | -n SW |
| `PlaceOnTable(turns)` | Połóż kartę na stole z licznikiem (-1 = permanentnie) |
| `Sequence(...)` | Wykonaj efekty po kolei |
| `Conditional(cond, then, else)` | Warunkowe wykonanie |
| `ForEachCardInZone` | Iteruj po kartach w strefie |
| `ChooseCardFromZone` | Wymagaj decyzji gracza |
| `DiscardChosenCard` | Odrzuć wybraną kartę |
| `EmptyStomachToToilet` | Opróżnij żołądek |
| `SkipDraw` | Pomiń dobieranie karty |

### Warunki (`Core/Effects/Conditions/`)

- `HasFlavorInZone(zone, flavor)` - czy jest smak w strefie
- `HasFlavorsInZone(zone, flavor1, flavor2)` - czy są OBA smaki
- `CountUniqueFlavorsInZone(zone, minCount)` - ile różnych smaków

## System Decyzji (Choice)

Gdy efekt wymaga wyboru gracza:

1. `ChooseCardFromZone.Execute()` zwraca `EffectResult.NeedsChoice`
2. `GameEngine` ustawia `State.Phase = AwaitingChoice` i `State.PendingChoice`
3. UI wyświetla opcje, gracz wybiera
4. `GameEngine.Choose(index)` kontynuuje z `PendingChoice.Continuation`

## Klasa Card

```csharp
public class Card {
    // Identyfikacja
    string Id;                 // np. "wyprawa_do_toalety"
    string Name;               // "Wyprawa do toalety"
    string? FlavorText;        // Tekst klimatyczny (kursywa)
    string Instruction;        // Opis działania karty dla gracza

    // Statystyki
    Flavor Flavor;             // Smak (archetyp)
    int WillpowerCost;         // Koszt SW
    int Calories;              // Kaloryczność

    // Triggery (efekty)
    IEffect? OnPlay;           // Po zagraniu
    IEffect? OnEat;            // Po zjedzeniu
    IEffect? OnDraw;           // Po dobraniu do ręki
    IEffect? OnEnterTable;     // Po położeniu na stole
    IEffect? OnLeaveTable;     // Po usunięciu ze stołu (dowolnym)
    IEffect? OnTurnOnTable;    // Co turę gdy leży na stole
    IEffect? OnTableCounterZero; // Gdy licznik = 0
    IEffect? OnDiscard;        // Po trafieniu do kibelka
}
```

## System Zdarzeń (Events)

`GameEngine` emituje eventy (`Core/Events/`) dla UI:
- `CardPlayedEvent`, `CardEatenEvent`, `CardDrawnEvent`
- `FatChangedEvent`, `WillpowerChangedEvent`
- `CardMovedEvent`, `CardDiscardedEvent`
- `ChoiceRequestedEvent`, `ChoiceMadeEvent`
- `TurnStartedEvent`, `TableCounterTickedEvent`
- `GameWonEvent`, `GameLostEvent`

Eventy służą do animacji w SFML (Faza 2).

## Uruchamianie

```bash
# Tryb interaktywny (gra w konsoli)
dotnet run --project Caladabra.Console -- interactive

# Test mechanik
dotnet run --project Caladabra.Console -- test

# Tryb JSON (turn-by-turn, AI-friendly)
dotnet run --project Caladabra.Console -- new [--seed N] [--state path]
dotnet run --project Caladabra.Console -- status [--state path]
dotnet run --project Caladabra.Console -- play N [--state path]
dotnet run --project Caladabra.Console -- eat N [--state path]
dotnet run --project Caladabra.Console -- choose N [--state path]
dotnet run --project Caladabra.Console -- info <nazwa>
```

Tryb JSON zapisuje stan gry do `game.json` (domyślnie) i zwraca JSON na stdout.

### Komendy w trybie interaktywnym

| Komenda | Aliasy | Opis |
|---------|--------|------|
| `play <nr>` | `p` | Zagraj kartę o podanym numerze |
| `eat <nr>` | `e` | Zjedz kartę o podanym numerze |
| `choose <nr>` | `c` | Wybierz opcję (gdy gra czeka na decyzję) |
| `info <nr>` | `i`, `inspect` | Pokaż kartę z ręki |
| `info <nazwa>` | | Szukaj karty po nazwie (np. `info Wilczy`) |
| `info <strefa> <nr>` | | Pokaż kartę ze strefy (hand/table/stomach/toilet) |
| `status` | `s` | Pokaż stan gry |
| `quit` | `q`, `exit` | Zakończ grę |

## TODO / Niezaimplementowane

### Brakujące Efekty Kart (mają TODO w CardDefinitions.cs)

- `ReturnToPantryTop` - Diabelski bumerang
- `AddChosenToHand` - Dostawa jedzenia, Grzebanie w kibelku
- `MoveChosenToStomach` - Łapczywe jedzenie
- `TransformInto` - Kwantowa próżnia
- `SetTableCounterTo1` - Było i nie ma
- `ModifyDrawBehavior` - Jasnowidzenie
- `ModifyCaloriesOnDraw` - Dieta cud

### Inne

- [x] ~~Turn-by-turn mode z JSON (JsonRunner)~~ - ZAIMPLEMENTOWANE
- [x] ~~Serializacja GameState do JSON~~ - ZAIMPLEMENTOWANE
- [ ] Unit testy
- [ ] Faza 2: SFML.Net Desktop

## Konwencje

- Język kodu: angielski (nazwy klas, metod)
- Język komunikatów/UI: polski
- Wszystkie karty mają unikalne `Id` (snake_case, np. `wyprawa_do_toalety`)
- Strefy: `CardList`, `Pantry`, `Hand`, `Table`, `Stomach`, `Toilet`
- Smaki: `Salty`, `Sweet`, `Bitter`, `Spicy`, `Sour`, `Umami`
