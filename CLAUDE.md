# Caladabra - Notatki Techniczne

> Zasady gry i mechaniki opisane są w `Docs/CaladabraGDD.md` - nie duplikuj tutaj.

## Struktura Projektu

```
Caladabra/
├── Caladabra.Core/          # Logika gry (biblioteka)
├── Caladabra.Console/       # Aplikacja konsolowa
├── Caladabra.Desktop/       # Aplikacja graficzna SFML.Net
└── Docs/CaladabraGDD.md     # Game Design Document
```

## Zasada Architektury: Core First

**WAŻNE: Jeśli coś można naprawić w Core, napraw to w Core - nie w Desktop/Console.**

- `Core` zawiera całą logikę gry i powinien działać niezależnie od UI
- `Desktop` i `Console` to tylko "cienkie" warstwy prezentacji
- Jeśli bug dotyczy mechaniki gry → napraw w `Core`
- Jeśli trzeba dodać "workaround" w UI → zastanów się czy to nie powinno być w `Core`

Przykład: Liczniki kart na stole (`ProcessStartOfTurn`) - powinny być automatycznie obsługiwane przez `GameEngine`, nie przez każdą warstwę UI osobno.

## Kluczowe Pliki

| Plik | Opis |
|------|------|
| `Core/Engine/GameEngine.cs` | Silnik gry - `Play()`, `Eat()`, `Choose()`, `DrawCards()` |
| `Core/Engine/GameRules.cs` | Stałe (StartingFat=100, MaxWillpower=30, MaxTurns=30) |
| `Core/Engine/DeckBuilder.cs` | Budowanie talii (prototype, test, z pliku JSON) |
| `Core/State/GameState.cs` | Pełny stan gry (Fat, Willpower, strefy, ActiveModifiers) |
| `Core/State/PendingChoice.cs` | System decyzji gracza |
| `Core/State/ModifierType.cs` | Typy modyfikatorów ciągłych |
| `Core/State/ActiveModifier.cs` | Aktywny modyfikator (karta na stole) |
| `Core/Cards/Definitions/CardDefinitions.cs` | Definicje wszystkich 18 kart |
| `Core/Cards/CardRegistry.cs` | Singleton z rejestrem kart |
| `Console/Program.cs` | Punkt wejścia, tryby: interactive, test, JSON |
| `Console/JsonRunner.cs` | Runner JSON dla trybu turn-by-turn (AI-friendly) |
| `Desktop/Scenes/GameScene.cs` | Główna scena rozgrywki |
| `Desktop/Scenes/CardListScene.cs` | Overlay z listą kart (browser/picker) |
| `Desktop/Rendering/CardRenderer.cs` | Renderowanie kart (tekstury, tryby) |
| `Desktop/Integration/GameController.cs` | Wrapper na GameEngine dla UI |
| `Desktop/Animation/AnimationManager.cs` | System animacji (kolejka, blokada interakcji) |
| `Desktop/Animation/CardMoveAnimation.cs` | Animacja przelotu karty |
| `Desktop/Animation/Easing.cs` | Funkcje easingu |

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
| `DiscardChosenFromHand` | Odrzuć wybraną kartę z ręki (Jasnowidzenie) |
| `EmptyStomachToToilet` | Opróżnij żołądek |
| `SkipDraw` | Pomiń dobieranie karty |
| `AddModifier(type, value)` | Dodaj modyfikator ciągły (patrz sekcja Modyfikatory) |
| `RemoveModifiersFromSource` | Usuń modyfikatory karty źródłowej |
| `TransformIntoChosen` | Zamień kartę w ręce na wybraną (Kwantowa próżnia) |

### Warunki (`Core/Effects/Conditions/`)

- `HasFlavorInZone(zone, flavor)` - czy jest smak w strefie
- `HasFlavorsInZone(zone, flavor1, flavor2)` - czy są OBA smaki
- `CountUniqueFlavorsInZone(zone, minCount)` - ile różnych smaków

### System Modyfikatorów (efekty ciągłe)

Karty na Stole mogą mieć efekty ciągłe poprzez system modyfikatorów.

```csharp
// Core/State/ModifierType.cs
public enum ModifierType
{
    ExtraDrawThenDiscard,   // Jasnowidzenie: +1 dobrana karta, potem wybór do odrzucenia
    ReduceCaloriesOnDraw    // Dieta cud: -X kalorii na dobranych kartach
}

// Użycie w definicji karty
OnEnterTable = new AddModifier(ModifierType.ExtraDrawThenDiscard, 1),
OnLeaveTable = RemoveModifiersFromSource.Instance
```

**Pliki**: `Core/State/ModifierType.cs`, `Core/State/ActiveModifier.cs`

Modyfikatory są przechowywane w `GameState.ActiveModifiers` i aplikowane w `GameEngine.DrawCards()`.

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
    int Calories;              // Kaloryczność (get/set - może być modyfikowana przez efekty)

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
# Desktop (graficzny interfejs - zalecane)
dotnet run --project Caladabra.Desktop

# Tryb interaktywny (gra w konsoli)
dotnet run --project Caladabra.Console -- interactive

# Test mechanik
dotnet run --project Caladabra.Console -- test

# Tryb JSON (turn-by-turn, AI-friendly)
dotnet run --project Caladabra.Console -- new [--seed N] [--state path] [--deck plik.json]
dotnet run --project Caladabra.Console -- status [--state path]
dotnet run --project Caladabra.Console -- play N [--state path]
dotnet run --project Caladabra.Console -- eat N [--state path]
dotnet run --project Caladabra.Console -- choose N [--state path]
dotnet run --project Caladabra.Console -- info <nazwa>
```

Tryb JSON zapisuje stan gry do `game.json` (domyślnie) i zwraca JSON na stdout.

### Opcja --deck (custom deck)

Pozwala załadować talię z pliku JSON zamiast standardowej 60-kartowej:

```json
["jasnowidzenie", "diabelski_bumerang", "diabelski_bumerang", "lizak_na_oslode"]
```

Karty są w dokładnie takiej kolejności jak w pliku (bez tasowania). Przydatne do testowania.

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

### Efekty Kart - WSZYSTKIE ZAIMPLEMENTOWANE ✅

Wszystkie 18 kart z prototypowej talii ma działające efekty.

### Inne

- [x] ~~Turn-by-turn mode z JSON (JsonRunner)~~ - ZAIMPLEMENTOWANE
- [x] ~~Serializacja GameState do JSON~~ - ZAIMPLEMENTOWANE
- [x] ~~System modyfikatorów (efekty ciągłe)~~ - ZAIMPLEMENTOWANE
- [x] ~~Custom deck z pliku JSON (--deck)~~ - ZAIMPLEMENTOWANE
- [x] ~~Wszystkie efekty kart~~ - ZAIMPLEMENTOWANE
- [x] ~~Faza 2: SFML.Net Desktop~~ - ZAIMPLEMENTOWANE
- [x] ~~Animacje kart~~ - ZAIMPLEMENTOWANE (CardMoveAnimation, elevation system)
- [ ] Unit testy (odłożone)
- [ ] Balans kart

## Testowanie Gry

### Tworzenie testowych talii (--deck)

Użyj opcji `--deck` do załadowania własnej talii z pliku JSON:

```bash
dotnet run --project Caladabra.Console -- new --deck test_deck.json
```

**WAŻNE: Kolejność kart w JSON**
- Karty są ładowane do Spiżarni metodą `AddToBottom`
- Dobieranie (`Draw`) pobiera karty z góry stosu
- **Ostatnie karty w JSON będą pierwszymi dobranym** (trafią na górę stosu)
- **Pierwsze karty w JSON będą ostatnimi dobranym** (trafią na spód stosu)

Przykład - chcesz zacząć z Jasnowidzeniem na ręce:
```json
[
  "lizak_na_oslode",    // dobrana jako 9ta (na spodzie)
  "lizak_na_oslode",    // dobrana jako 8ma
  "baton_energetyczny", // dobrana jako 7ma
  "baton_energetyczny", // dobrana jako 6ta
  "hat_trick",          // dobrana jako 5ta
  "hat_trick",          // na ręce (4ta)
  "diabelski_bumerang", // na ręce (3cia)
  "diabelski_bumerang", // na ręce (2ga)
  "wilczy_glod",        // na ręce (1sza)
  "jasnowidzenie"       // na ręce (ostatnia = 0)
]
```

Ostatnie 5 kart (indeksy 5-9) trafia na rękę startową.

### Typowe błędy z serializacją JSON

**Problem: Porównywanie referencji po załadowaniu z JSON**

Po zapisie/odczycie stanu gry z JSON, karty są klonowane z `CardRegistry`. Nie są to te same instancje obiektów!

❌ **ŹLE** - nie zadziała po JSON restore:
```csharp
var index = hand.Cards.IndexOf(context.SourceCard);  // -1 bo różne instancje!
hand.Cards.Contains(card)  // false!
```

✅ **DOBRZE** - używaj ID lub indeksów:
```csharp
var index = hand.Cards.ToList().FindIndex(c => c.Id == context.SourceCard.Id);
context.ChosenIndices[0]  // użyj zapisanego indeksu
```

**Pliki które musiały być naprawione z tego powodu:**
- `DiscardChosenFromHand.cs` - używa `context.ChosenIndices[0]` zamiast `ChosenCard`
- `TransformIntoChosen.cs` - szuka po `c.Id == context.SourceCard.Id`

### EffectTrigger dla PendingChoice

Gdy tworzysz `PendingChoice`, **zawsze ustaw `EffectTrigger`**! Jest potrzebny do odtworzenia kontynuacji po JSON restore.

Wartości w `JsonRunner.RestorePendingChoice`:
- `"OnPlay"` → `sourceCard.OnPlay`
- `"OnEat"` → `sourceCard.OnEat`
- `"OnDraw"` → `sourceCard.OnDraw`
- `"Discard"` → `DiscardChosenFromHand.Instance` (Jasnowidzenie)

Jeśli `EffectTrigger` jest null/nieznany, domyślnie używa `OnPlay` - co może spowodować dziwne błędy!

### Testowanie konkretnych mechanik

#### Karty z modyfikatorami (Jasnowidzenie, Dieta cud)

1. Zagraj kartę → trafia na Stół → `OnEnterTable` dodaje modyfikator
2. Wykonaj akcję (Play/Eat) → `DrawCards` sprawdza `ActiveModifiers`
3. Gdy karta schodzi ze Stołu → `OnLeaveTable` usuwa modyfikatory

#### Kwantowa próżnia (OnDraw + transformacja)

1. Dobierz Kwantową próżnię → `OnDraw` requestuje choice z CardList
2. Wybierz kartę z listy → `TransformIntoChosen` zamienia kartę w ręce
3. Nowa karta ma pełną funkcjonalność (można ją zagrać/zjeść normalnie)

#### Karty na Stole z licznikiem

1. `PlaceOnTable(3)` → karta trafia na Stół z licznikiem 3
2. Co turę: `ProcessStartOfTurn()` zmniejsza licznik, wywołuje `OnTurnOnTable`
3. Gdy licznik = 0: `OnTableCounterZero`, potem `OnLeaveTable`, potem do Kibelka

### Przykładowe talie testowe

Pliki w głównym katalogu projektu:

| Plik | Testuje |
|------|---------|
| `test_jasnowidzenie.json` | ExtraDrawThenDiscard, odrzucanie kart |
| `test_kwantowa_proznia.json` | OnDraw, transformacja, CardList choice |

### Debugowanie

1. **Sprawdź `State.Phase`** - jeśli gra czeka na choice a nie wiesz dlaczego
2. **Sprawdź `State.PendingChoice.EffectTrigger`** - czy jest ustawiony poprawnie
3. **Użyj `--state custom.json`** - żeby nie nadpisywać `game.json` podczas testów
4. **Porównaj stan przed/po** - zapisz JSON, wykonaj akcję, porównaj zmiany

### Częste pułapki

- **Karta nie trafia do Kibelka** → może efekt przeniósł ją do innej strefy (Table, Pantry)
- **Modyfikator nie działa** → sprawdź czy `OnEnterTable` dodaje i `OnLeaveTable` usuwa
- **Choice nie działa po JSON restore** → sprawdź `EffectTrigger` i `RestorePendingChoice`
- **Karta "znika"** → porównywanie referencji zamiast ID (patrz sekcja wyżej)

## Konwencje

- Język kodu: angielski (nazwy klas, metod)
- Język komunikatów/UI: polski
- Wszystkie karty mają unikalne `Id` (snake_case, np. `wyprawa_do_toalety`)
- Strefy: `CardList`, `Pantry`, `Hand`, `Table`, `Stomach`, `Toilet`
- Smaki: `Salty`, `Sweet`, `Bitter`, `Spicy`, `Sour`, `Umami`
