using Caladabra.Console.Rendering;
using Caladabra.Core.Cards;
using Caladabra.Core.Cards.Definitions;
using Caladabra.Core.Engine;
using Caladabra.Core.State;

namespace Caladabra.Console;

class Program
{
    static void Main(string[] args)
    {
        // Zarejestruj wszystkie karty
        CardDefinitions.RegisterAll();

        if (args.Length == 0 || args[0] == "interactive")
        {
            RunInteractive();
        }
        else
        {
            RunCommand(args);
        }
    }

    /// <summary>
    /// Czeka na klawisz (lub pomija w trybie pipe/redirected input).
    /// </summary>
    static void WaitForKey()
    {
        if (System.Console.IsInputRedirected)
            return; // W trybie pipe nie czekamy
        try
        {
            WaitForKey();
        }
        catch (InvalidOperationException)
        {
            // Konsola niedostępna
        }
    }

    static void RunInteractive()
    {
        System.Console.OutputEncoding = System.Text.Encoding.UTF8;
        System.Console.Title = "Caladabra - Willpower is Magic";

        ConsoleRenderer.RenderMessage("=== CALADABRA ===", ConsoleColor.Cyan);
        ConsoleRenderer.RenderMessage("Willpower is Magic", ConsoleColor.DarkCyan);
        ConsoleRenderer.RenderMessage("");
        ConsoleRenderer.RenderMessage("Rozpoczynam nową grę...", ConsoleColor.Yellow);

        var deck = DeckBuilder.BuildPrototypeDeck();
        var engine = GameEngine.NewGame(deck);
        engine.DrawInitialHand();  // Dobierz początkową rękę (wywołuje OnDraw)

        // Główna pętla gry
        while (engine.State.Phase != GamePhase.Won && engine.State.Phase != GamePhase.Lost)
        {
            ConsoleRenderer.RenderGameState(engine.State);

            System.Console.Write("  > ");
            var input = System.Console.ReadLine();

            if (input == null) // EOF - koniec inputu (pipe mode)
                break;

            input = input.Trim().ToLower();
            if (string.IsNullOrEmpty(input))
                continue;

            ProcessInput(engine, input);
        }

        // Koniec gry
        ConsoleRenderer.RenderGameState(engine.State);
        System.Console.WriteLine();
        System.Console.WriteLine("  Naciśnij Enter, aby zakończyć...");
        System.Console.ReadLine();
    }

    static void ProcessInput(GameEngine engine, string input)
    {
        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;

        var command = parts[0];
        var arg = parts.Length > 1 ? parts[1] : null;

        switch (command)
        {
            case "p":
            case "play":
                if (int.TryParse(arg, out int playIndex) && playIndex >= 1)
                {
                    if (engine.CanPlay(playIndex - 1))
                    {
                        var events = engine.Play(playIndex - 1);
                        // Events można wykorzystać do logowania
                    }
                    else
                    {
                        ConsoleRenderer.RenderMessage("Nie możesz zagrać tej karty (za mało SW lub nieprawidłowy indeks).", ConsoleColor.Red);
                        WaitForKey();
                    }
                }
                else
                {
                    ConsoleRenderer.RenderMessage("Użycie: play <numer_karty>", ConsoleColor.Yellow);
                    WaitForKey();
                }
                break;

            case "e":
            case "eat":
                if (int.TryParse(arg, out int eatIndex) && eatIndex >= 1)
                {
                    if (engine.CanEat(eatIndex - 1))
                    {
                        var events = engine.Eat(eatIndex - 1);
                    }
                    else
                    {
                        ConsoleRenderer.RenderMessage("Nie możesz zjeść tej karty.", ConsoleColor.Red);
                        WaitForKey();
                    }
                }
                else
                {
                    ConsoleRenderer.RenderMessage("Użycie: eat <numer_karty>", ConsoleColor.Yellow);
                    WaitForKey();
                }
                break;

            case "c":
            case "choose":
                if (engine.State.Phase == GamePhase.AwaitingChoice)
                {
                    if (int.TryParse(arg, out int choiceIndex) && choiceIndex >= 1)
                    {
                        var events = engine.Choose(choiceIndex - 1);
                    }
                    else
                    {
                        ConsoleRenderer.RenderMessage("Użycie: choose <numer_opcji>", ConsoleColor.Yellow);
                        WaitForKey();
                    }
                }
                else
                {
                    ConsoleRenderer.RenderMessage("Nie ma teraz żadnej decyzji do podjęcia.", ConsoleColor.Red);
                    WaitForKey();
                }
                break;

            case "s":
            case "status":
                // Status już się wyświetla
                break;

            case "i":
            case "info":
            case "inspect":
                HandleInfoCommand(engine, parts);
                break;

            case "h":
            case "help":
            case "?":
                ConsoleRenderer.RenderHelp();
                System.Console.WriteLine("  Naciśnij dowolny klawisz...");
                WaitForKey();
                break;

            case "q":
            case "quit":
            case "exit":
                engine.State.Phase = GamePhase.Lost; // Wymuszenie końca
                ConsoleRenderer.RenderMessage("Zakończono grę.", ConsoleColor.Yellow);
                break;

            default:
                ConsoleRenderer.RenderMessage($"Nieznana komenda: {command}", ConsoleColor.Red);
                ConsoleRenderer.RenderMessage("Dostępne: play, eat, choose, status, info, help, quit", ConsoleColor.Yellow);
                WaitForKey();
                break;
        }
    }

    static void HandleInfoCommand(GameEngine engine, string[] parts)
    {
        if (parts.Length < 2)
        {
            ConsoleRenderer.RenderMessage("Użycie: info <nr> | info <nazwa> | info <strefa> <nr>", ConsoleColor.Yellow);
            ConsoleRenderer.RenderMessage("Strefy: hand, table, stomach, toilet", ConsoleColor.Yellow);
            WaitForKey();
            return;
        }

        Card? card = null;

        // Wariant 1: info <nr> - karta z ręki
        if (parts.Length == 2 && int.TryParse(parts[1], out int handIndex) && handIndex >= 1)
        {
            card = engine.State.Hand.GetAt(handIndex - 1);
            if (card == null)
            {
                ConsoleRenderer.RenderMessage($"Brak karty o numerze {handIndex} w ręce.", ConsoleColor.Red);
                WaitForKey();
                return;
            }
        }
        // Wariant 2: info <strefa> <nr> - karta z konkretnej strefy
        else if (parts.Length >= 3 && int.TryParse(parts[2], out int zoneIndex) && zoneIndex >= 1)
        {
            var zone = parts[1].ToLower();
            card = zone switch
            {
                "hand" or "reka" or "ręka" => engine.State.Hand.GetAt(zoneIndex - 1),
                "table" or "stol" or "stół" => engine.State.Table.Entries.ElementAtOrDefault(zoneIndex - 1)?.Card,
                "stomach" or "zoladek" or "żołądek" => engine.State.Stomach.Cards.ElementAtOrDefault(zoneIndex - 1),
                "toilet" or "kibelek" => engine.State.Toilet.Cards.ElementAtOrDefault(zoneIndex - 1),
                _ => null
            };

            if (card == null)
            {
                ConsoleRenderer.RenderMessage($"Brak karty o numerze {zoneIndex} w strefie '{zone}'.", ConsoleColor.Red);
                WaitForKey();
                return;
            }
        }
        // Wariant 3: info <nazwa> - szukaj po nazwie w CardList
        else
        {
            var searchName = string.Join(" ", parts.Skip(1));
            card = engine.State.CardList.Cards
                .FirstOrDefault(c => c.Name.Contains(searchName, StringComparison.OrdinalIgnoreCase));

            if (card == null)
            {
                ConsoleRenderer.RenderMessage($"Nie znaleziono karty o nazwie zawierającej '{searchName}'.", ConsoleColor.Red);
                WaitForKey();
                return;
            }
        }

        ConsoleRenderer.RenderCard(card);
        System.Console.WriteLine("  Naciśnij dowolny klawisz...");
        WaitForKey();
    }

    static void RunCommand(string[] args)
    {
        var command = args[0].ToLower();
        var statePath = GetArg(args, "--state");

        switch (command)
        {
            case "test":
                RunTest();
                break;

            // === JSON Runner (AI-friendly) ===

            case "new":
                var seed = GetArgInt(args, "--seed");
                var deckPath = GetArg(args, "--deck");
                JsonRunner.NewGame(statePath, seed, deckPath);
                break;

            case "status":
                JsonRunner.Status(statePath);
                break;

            case "play":
                if (args.Length < 2 || !int.TryParse(args[1], out int playIndex))
                {
                    System.Console.WriteLine("{\"error\": \"Użycie: play <numer_karty>\"}");
                    return;
                }
                JsonRunner.Play(statePath, playIndex);
                break;

            case "eat":
                if (args.Length < 2 || !int.TryParse(args[1], out int eatIndex))
                {
                    System.Console.WriteLine("{\"error\": \"Użycie: eat <numer_karty>\"}");
                    return;
                }
                JsonRunner.Eat(statePath, eatIndex);
                break;

            case "choose":
                if (args.Length < 2 || !int.TryParse(args[1], out int chooseIndex))
                {
                    System.Console.WriteLine("{\"error\": \"Użycie: choose <numer_opcji>\"}");
                    return;
                }
                JsonRunner.Choose(statePath, chooseIndex);
                break;

            case "info":
                if (args.Length < 2)
                {
                    System.Console.WriteLine("{\"error\": \"Użycie: info <nazwa_karty>\"}");
                    return;
                }
                JsonRunner.Info(string.Join(" ", args.Skip(1)));
                break;

            default:
                System.Console.WriteLine($"Nieznana komenda: {command}");
                System.Console.WriteLine("Dostępne komendy:");
                System.Console.WriteLine("  interactive - tryb interaktywny");
                System.Console.WriteLine("  test        - test inicjalizacji gry");
                System.Console.WriteLine();
                System.Console.WriteLine("JSON Runner (AI-friendly):");
                System.Console.WriteLine("  new [--seed N] [--deck plik.json] - nowa gra");
                System.Console.WriteLine("      --deck: talia z pliku JSON (tablica ID kart), bez tasowania");
                System.Console.WriteLine("  status           - pokaż stan gry");
                System.Console.WriteLine("  play N           - zagraj kartę N");
                System.Console.WriteLine("  eat N            - zjedz kartę N");
                System.Console.WriteLine("  choose N         - wybierz opcję N");
                System.Console.WriteLine("  info <nazwa>     - info o karcie");
                break;
        }
    }

    static string? GetArg(string[] args, string name)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name)
                return args[i + 1];
        }
        return null;
    }

    static int? GetArgInt(string[] args, string name)
    {
        var value = GetArg(args, name);
        if (value != null && int.TryParse(value, out int result))
            return result;
        return null;
    }

    static void RunTest()
    {
        System.Console.WriteLine("=== TEST INICJALIZACJI GRY ===");
        System.Console.WriteLine();

        var deck = DeckBuilder.BuildPrototypeDeck();
        System.Console.WriteLine($"Utworzono talię: {deck.Count} kart");

        var engine = GameEngine.NewGame(deck, seed: 42);
        engine.DrawInitialHand();  // Dobierz początkową rękę (wywołuje OnDraw)
        System.Console.WriteLine($"Utworzono grę z ziarnem 42");
        System.Console.WriteLine();

        System.Console.WriteLine($"Tłuszcz: {engine.State.Fat}");
        System.Console.WriteLine($"Siła Woli: {engine.State.Willpower}");
        System.Console.WriteLine($"Tura: {engine.State.Turn}");
        System.Console.WriteLine($"Faza: {engine.State.Phase}");
        System.Console.WriteLine();

        System.Console.WriteLine($"Spiżarnia: {engine.State.Pantry.Count} kart");
        System.Console.WriteLine($"Ręka: {engine.State.Hand.Count} kart");
        System.Console.WriteLine($"Stół: {engine.State.Table.Count} kart");
        System.Console.WriteLine($"Żołądek: {engine.State.Stomach.Count} kart");
        System.Console.WriteLine($"Kibelek: {engine.State.Toilet.Count} kart");
        System.Console.WriteLine($"Lista Kart Caladabra: {engine.State.CardList.Count} kart");
        System.Console.WriteLine();

        System.Console.WriteLine("Karty w ręce:");
        for (int i = 0; i < engine.State.Hand.Count; i++)
        {
            var card = engine.State.Hand.Cards[i];
            System.Console.WriteLine($"  [{i + 1}] {card.Name} (SW:{card.WillpowerCost}, Kal:{card.Calories}) - {card.Flavor}");
        }
        System.Console.WriteLine();

        // Test zagrania karty
        System.Console.WriteLine("=== TEST AKCJI ===");
        var firstPlayable = -1;
        for (int i = 0; i < engine.State.Hand.Count; i++)
        {
            if (engine.CanPlay(i))
            {
                firstPlayable = i;
                break;
            }
        }

        if (firstPlayable >= 0)
        {
            var card = engine.State.Hand.Cards[firstPlayable];
            System.Console.WriteLine($"Granie karty [{firstPlayable + 1}] {card.Name}...");
            var events = engine.Play(firstPlayable);
            System.Console.WriteLine($"Wyemitowano {events.Count} zdarzeń:");
            foreach (var evt in events)
            {
                System.Console.WriteLine($"  - {evt.GetType().Name}");
            }
            System.Console.WriteLine();
            System.Console.WriteLine($"Tłuszcz po zagraniu: {engine.State.Fat}");
            System.Console.WriteLine($"Siła Woli po zagraniu: {engine.State.Willpower}");
            System.Console.WriteLine($"Faza po zagraniu: {engine.State.Phase}");
        }
        else
        {
            System.Console.WriteLine("Brak kart możliwych do zagrania (za mało SW)");
        }

        System.Console.WriteLine();

        // Test jedzenia karty
        System.Console.WriteLine("=== TEST JEDZENIA ===");
        if (engine.State.Hand.Count > 0 && engine.CanEat(0))
        {
            var cardToEat = engine.State.Hand.Cards[0];
            System.Console.WriteLine($"Jedzenie karty [{1}] {cardToEat.Name}...");
            var eatEvents = engine.Eat(0);
            System.Console.WriteLine($"Wyemitowano {eatEvents.Count} zdarzeń:");
            foreach (var evt in eatEvents)
            {
                System.Console.WriteLine($"  - {evt.GetType().Name}");
            }
            System.Console.WriteLine();
            System.Console.WriteLine($"Tłuszcz po jedzeniu: {engine.State.Fat}");
            System.Console.WriteLine($"Siła Woli po jedzeniu: {engine.State.Willpower}");
            System.Console.WriteLine($"Żołądek: {engine.State.Stomach.Count} kart");
        }

        System.Console.WriteLine();

        // Test przetwarzania tury (liczniki na stole)
        System.Console.WriteLine("=== TEST PRZETWARZANIA TURY ===");
        System.Console.WriteLine($"Stół przed przetworzeniem: {engine.State.Table.Count} kart");
        foreach (var entry in engine.State.Table.Entries)
        {
            System.Console.WriteLine($"  - {entry.Card.Name} (pozostało tur: {entry.TurnsRemaining})");
        }

        var turnEvents = engine.ProcessStartOfTurn();
        System.Console.WriteLine($"Wyemitowano {turnEvents.Count} zdarzeń:");
        foreach (var evt in turnEvents)
        {
            System.Console.WriteLine($"  - {evt.GetType().Name}");
        }

        System.Console.WriteLine($"Stół po przetworzeniu: {engine.State.Table.Count} kart");
        foreach (var entry in engine.State.Table.Entries)
        {
            System.Console.WriteLine($"  - {entry.Card.Name} (pozostało tur: {entry.TurnsRemaining})");
        }

        System.Console.WriteLine();

        // Test systemu wyborów
        System.Console.WriteLine("=== TEST SYSTEMU WYBORÓW ===");

        // Dodaj kartę "Wyprawa do toalety" do ręki jeśli jej nie ma
        var toiletTripIndex = -1;
        for (int i = 0; i < engine.State.Hand.Count; i++)
        {
            var c = engine.State.Hand.Cards[i];
            if (c.Id == "wyprawa_do_toalety")
            {
                toiletTripIndex = i;
                break;
            }
        }

        if (toiletTripIndex < 0)
        {
            // Dodaj kartę ręcznie z CardList
            var toiletTripCard = engine.State.CardList.FindById("wyprawa_do_toalety");
            if (toiletTripCard != null)
            {
                // Zwolnij miejsce w ręce jeśli pełna
                if (engine.State.Hand.IsFull)
                {
                    engine.State.Hand.RemoveAt(0);
                }
                engine.State.Hand.Add(toiletTripCard.Clone());
                toiletTripIndex = engine.State.Hand.Count - 1;
                System.Console.WriteLine("Dodano kartę 'Wyprawa do toalety' do ręki (na potrzeby testu)");
            }
        }

        // Upewnij się, że mamy wystarczająco SW
        if (engine.State.Willpower < 4)
        {
            engine.State.ModifyWillpower(10);
            System.Console.WriteLine("Dodano 10 SW (na potrzeby testu)");
        }

        if (toiletTripIndex >= 0 && engine.State.Table.Count > 0)
        {
            var wpCard = engine.State.Hand.Cards[toiletTripIndex];
            System.Console.WriteLine($"Granie karty 'Wyprawa do toalety' (wymaga wyboru karty ze stołu)...");
            System.Console.WriteLine($"Stół przed zagraniem: {engine.State.Table.Count} kart");

            // Wystarczy SW?
            if (engine.State.Willpower >= wpCard.WillpowerCost)
            {
                var choiceEvents = engine.Play(toiletTripIndex);
                System.Console.WriteLine($"Faza po zagraniu: {engine.State.Phase}");

                if (engine.State.Phase == GamePhase.AwaitingChoice)
                {
                    System.Console.WriteLine($"Gra czeka na wybór gracza!");
                    System.Console.WriteLine($"Pytanie: {engine.State.PendingChoice?.Prompt}");
                    System.Console.WriteLine($"Opcje:");
                    foreach (var opt in engine.State.PendingChoice?.Options ?? [])
                    {
                        System.Console.WriteLine($"  [{opt.Index + 1}] {opt.DisplayText}");
                    }

                    // Wybierz pierwszą opcję
                    System.Console.WriteLine("Wybieram opcję 1...");
                    var afterChoice = engine.Choose(0);
                    System.Console.WriteLine($"Faza po wyborze: {engine.State.Phase}");
                    System.Console.WriteLine($"Tłuszcz po wyborze: {engine.State.Fat} (karta redukuje o 6)");
                    System.Console.WriteLine($"Stół po wyborze: {engine.State.Table.Count} kart");
                    System.Console.WriteLine($"Kibelek: {engine.State.Toilet.Count} kart");
                }
            }
            else
            {
                System.Console.WriteLine($"Za mało SW ({engine.State.Willpower}) na kartę (wymaga {wpCard.WillpowerCost})");
            }
        }
        else
        {
            System.Console.WriteLine("Brak karty 'Wyprawa do toalety' w ręce lub stół jest pusty - pomijam test");
        }

        System.Console.WriteLine();

        // Test wyświetlania info o karcie
        System.Console.WriteLine("=== TEST KOMENDY INFO ===");

        // Test 1: info <nazwa> - wyszukiwanie po nazwie
        System.Console.WriteLine("Test: info Wilczy (szukaj po nazwie)");
        var searchCard = engine.State.CardList.Cards
            .FirstOrDefault(c => c.Name.Contains("Wilczy", StringComparison.OrdinalIgnoreCase));
        if (searchCard != null)
        {
            System.Console.WriteLine($"  Znaleziono: {searchCard.Name}");
        }

        // Test 2: info table <nr> - karta ze stołu
        System.Console.WriteLine("\nTest: info table 1 (karta ze stołu)");
        var tableCard = engine.State.Table.Entries.ElementAtOrDefault(0)?.Card;
        if (tableCard != null)
        {
            System.Console.WriteLine($"  Stół[1]: {tableCard.Name}");
        }
        else
        {
            System.Console.WriteLine("  (stół pusty)");
        }

        // Test 3: info stomach <nr> - karta z żołądka
        System.Console.WriteLine("\nTest: info stomach 1 (karta z żołądka)");
        var stomachCard = engine.State.Stomach.Cards.ElementAtOrDefault(0);
        if (stomachCard != null)
        {
            System.Console.WriteLine($"  Żołądek[1]: {stomachCard.Name}");
        }
        else
        {
            System.Console.WriteLine("  (żołądek pusty)");
        }

        System.Console.WriteLine();
        System.Console.WriteLine("=== TEST ZAKOŃCZONY ===");
    }
}
