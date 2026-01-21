# Caladabra - Lista TODO

> Plik dla kontynuacji pracy w kolejnych sesjach.
> Ostatnia aktualizacja: 2026-01-21

---

## Stan Projektu

### Ukończone (Faza 1-3)

#### Fundament (Faza 1)
- [x] `Game.cs` - okno SFML z game loop
- [x] `GameSettings.cs` - persystentne ustawienia
- [x] `ScaleManager.cs` - skalowanie UI dla różnych rozdzielczości
- [x] `AssetManager.cs` - ładowanie fontu Lato
- [x] `IScene` + `SceneManager` - system scen (stos z Push/Pop)
- [x] `Theme.cs` - kolory i rozmiary

#### Minimalna Gra (Faza 2)
- [x] `FlavorColors.cs` - mapowanie kolorów smaków
- [x] `TextBox.cs` - tekst z zawijaniem i wyrównaniem
- [x] `CardRenderer.cs` - renderowanie kart (Full, Small, Tiny, Back)
- [x] `ZoneRenderer.cs` - renderowanie stref
- [x] `GameScene.cs` - layout planszy
- [x] `GameController.cs` - integracja z Core

#### Interaktywność (Faza 3)
- [x] Hover na kartach (Hand, Table, Stomach) z podglądem
- [x] Sterowanie: LPM = zagraj, PPM = zjedz (zmienione z double-click/hold)
- [x] Komunikat błędu przy braku SW (czerwony prostokąt, fade-out)
- [x] `PendingChoice` system dla efektów kart
- [x] `CardListScene.cs` - przeglądanie kart (grid 6x2, scrollbar, Browse/Select mode)
- [x] Integracja CardListScene z efektami (Kwantowa próżnia, Dostawa jedzenia)

#### Poprawki Layoutu
- [x] Spiżarnia i Kibelek na tym samym poziomie Y co Ręka
- [x] Podgląd karty większy (skala 2.3) i wyśrodkowany pionowo
- [x] Żołądek - karty jedna pod drugą (bez nakładania), najnowsza na górze
- [x] Przycisk "Lista Kart" nad podglądem
- [x] Usunięty szary pasek statusu - statystyki wyśrodkowane

#### Bug Fixes
- [x] OnDraw dla kart startowych (DrawInitialHand w GameEngine)
- [x] Hat trick OnDraw trigger
- [x] ObjectDisposedException dla Clock przy overlay (nie dispose w Exit())
- [x] CardListScene - zachowanie indeksów wyboru (CardListItem z ChoiceIndex)

---

## Do Zrobienia

### Faza 4: Polish (Priorytet: Wysoki)

#### Animacje
- [ ] `AnimationManager.cs` - kolejka animacji
- [ ] `CardMoveAnimation.cs` - ruch karty między strefami
- [ ] `NumberAnimation.cs` / `NumberPulseAnimation.cs` - zmiana Tłuszczu/SW
- [ ] `FadeInAnimation.cs` - przejścia między scenami
- [ ] Integracja z `EventProcessor.cs` - mapowanie IGameEvent → Animation

#### Menu Główne
- [x] `MainMenuScene.cs`:
  - [x] Przycisk "Nowa Gra"
  - [x] Przycisk "Kontynuuj" (jeśli istnieje zapis)
  - [x] Przycisk "Opcje"
  - [x] Przycisk "Wyjście"
  - [ ] Tło/logo gry

#### Opcje
- [x] `OptionsScene.cs`:
  - [x] Zmiana rozdzielczości (1280x720, 1600x900, 1920x1080, 2560x1440)
  - [x] Fullscreen toggle
  - [ ] Głośność (master volume) - wymaga systemu audio
  - [ ] Język (pl/en) - wymaga LocalizationManager

#### UI
- [x] `Button.cs` - komponent przycisku z hover/press states

### Faza 5: Finalizacja (Priorytet: Średni)

#### Lokalizacja
- [ ] `LocalizationManager.cs` - system stringów
- [ ] `Assets/Localization/pl.json` - polski (domyślny)
- [ ] `Assets/Localization/en.json` - angielski
- [ ] Zamiana hardcoded stringów w UI na klucze lokalizacji

#### UI Improvements
- [ ] `IntroScene.cs` - splash screen z logo
- [ ] `ProgressBar.cs` - paski wizualne dla Tłuszczu i SW (zamiast tekstu)
- [ ] Hover preview dla Spiżarni (wierzchnia karta)
- [ ] Liczniki tur na kartach na Stole (wizualne)

#### Dźwięk
- [ ] System audio (SFML Audio)
- [ ] Efekty dźwiękowe:
  - [ ] Zagranie karty
  - [ ] Zjedzenie karty
  - [ ] Dobieranie karty
  - [ ] Zmiana Tłuszczu/SW
  - [ ] Wygrana/Przegrana
- [ ] Muzyka w tle (opcjonalnie)

### Tryby Gry (Priorytet: Niski - po podstawowej grze)

#### Samouczek (Tutorial)
- [ ] Predefiniowana talia z ustaloną kolejnością
- [ ] System podpowiedzi krok po kroku
- [ ] Blokowanie niedozwolonych akcji
- [ ] Przejście do normalnej gry po zakończeniu

#### Kampania (Campaign)
- [ ] System odblokowywania talii
- [ ] Zapis postępu kampanii
- [ ] Różne poziomy trudności (różne talie)

#### Gra Starannie Dobrana (Curated Game)
- [ ] Wagi kart w budowaniu talii
- [ ] Limity ilościowe kart
- [ ] Kontrola kolejności (anty-frustration)

#### Gra Losowa (Random Game)
- [ ] Pełna losowość z wagami
- [ ] Ostrzeżenie dla gracza o chaosie

### Grafika (Priorytet: Niski)

- [ ] Ilustracje kart (czarno-białe, styl komiksowy)
- [ ] Ikonki smaków
- [ ] Logo gry
- [ ] Tło menu
- [ ] Efekty wizualne (particle effects dla spalania tłuszczu?)

### Balans (Ongoing)

- [ ] Testowanie wszystkich 18 kart
- [ ] Dostosowanie wartości SW/Kal/efektów
- [ ] Sprawdzenie kombinacji kart (synergies/broken combos)
- [ ] Plik `balans.json` z notatkami

---

## Znane Problemy / Technical Debt

1. **ZoneRenderer** - częściowo nieużywany (GameScene ma własne metody Draw*)
2. **InputManager** / **CardInteraction** - planowane ale niezaimplementowane (logika w GameScene)
3. **EventProcessor** - planowany ale niezaimplementowany (eventy Core nieużywane do animacji)
4. **GameBoardRenderer** - planowany ale niezaimplementowany

---

## Notatki dla Kolejnej Sesji

### Aktualne Sterowanie
- **LPM** (lewy przycisk myszy) = zagraj kartę
- **PPM** (prawy przycisk myszy) = zjedz kartę
- **ESC** = wyjście z gry
- **Scroll** = przewijanie w CardListScene

### Ważne Pliki
- `Caladabra.Desktop/Scenes/MainMenuScene.cs` - menu główne
- `Caladabra.Desktop/Scenes/OptionsScene.cs` - ekran opcji (overlay)
- `Caladabra.Desktop/Scenes/GameScene.cs` - główna scena gry (~900 linii)
- `Caladabra.Desktop/Scenes/CardListScene.cs` - overlay z listą kart
- `Caladabra.Desktop/UI/Button.cs` - komponent przycisku
- `Caladabra.Desktop/Rendering/CardRenderer.cs` - renderowanie kart
- `Caladabra.Core/Engine/GameEngine.cs` - silnik gry
- `Caladabra.Core/Cards/Definitions/CardDefinitions.cs` - definicje kart

### Plan Implementacji
Szczegółowy plan znajduje się w: `C:\Users\Jakub\.claude\plans\binary-humming-iverson.md`

### Uruchomienie
```bash
cd E:\Alu\CSharp\Caladabra
dotnet run --project Caladabra.Desktop
```

### Testowanie Mechanik (Console)
```bash
dotnet run --project Caladabra.Console -- interactive
dotnet run --project Caladabra.Console -- new --deck test_deck.json
```

---

## Changelog (Ostatnia Sesja)

### 2026-01-21 (sesja 2)
- Dodane `MainMenuScene.cs` - menu główne z przyciskami (Nowa Gra, Kontynuuj, Opcje, Wyjście)
- Dodane `OptionsScene.cs` - ekran opcji jako overlay (rozdzielczość, fullscreen)
- Dodane `Button.cs` - reużywalny komponent UI z hover/press states
- Game.cs teraz startuje z MainMenuScene zamiast bezpośrednio z GameScene
- Usunięty PlaceholderScene (nieużywany)

### 2026-01-21 (sesja 1)
- Zmiana sterowania: double-click/hold → LPM/PPM (single-click)
- Dodany komunikat błędu przy braku SW (czerwony prostokąt z fade-out)
- Zaktualizowany tekst info: "ESC = wyjście | LPM = zagraj | PPM = zjedz"
- Naprawiony bug z CardListScene i wyborem kart (CardListItem z ChoiceIndex)
- Naprawiony ObjectDisposedException dla Clock w GameScene
