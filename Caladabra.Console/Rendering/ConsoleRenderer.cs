using System.IO;
using Caladabra.Core.Cards;
using Caladabra.Core.Engine;
using Caladabra.Core.State;
using Caladabra.Core.Zones;

namespace Caladabra.Console.Rendering;

/// <summary>
/// Renderuje stan gry w konsoli.
/// </summary>
public static class ConsoleRenderer
{
    public static void RenderGameState(GameState state)
    {
        try
        {
            System.Console.Clear();
        }
        catch (IOException)
        {
            // Nie można wyczyścić konsoli (np. w trybie non-interactive)
        }
        RenderHeader(state);
        RenderTable(state);
        RenderStomach(state);
        RenderHand(state);
        RenderFooter(state);
    }

    private static void RenderHeader(GameState state)
    {
        var line = new string('═', 60);
        System.Console.WriteLine(line);

        System.Console.Write("  TŁUSZCZ: ");
        WriteColored(state.Fat.ToString(), state.Fat > 50 ? ConsoleColor.Red : ConsoleColor.Green);

        System.Console.Write("          SIŁA WOLI: ");
        WriteColored($"{state.Willpower}/{GameRules.MaxWillpower}", ConsoleColor.Cyan);

        System.Console.Write("       TURA: ");
        WriteColored(state.Turn.ToString(), ConsoleColor.Yellow);
        System.Console.WriteLine();

        System.Console.WriteLine(line);
        System.Console.WriteLine();
    }

    private static void RenderTable(GameState state)
    {
        System.Console.WriteLine($"  STÓŁ ({state.Table.Count}/{GameRules.MaxTableSize}):");

        if (state.Table.Count == 0)
        {
            System.Console.WriteLine("    (pusty)");
        }
        else
        {
            for (int i = 0; i < state.Table.Entries.Count; i++)
            {
                var entry = state.Table.Entries[i];
                System.Console.Write($"    [{i + 1}] ");
                WriteCardColored(entry.Card);

                if (entry.TurnsRemaining.HasValue && entry.TurnsRemaining.Value >= 0)
                {
                    System.Console.Write($" ({entry.TurnsRemaining} tur)");
                }
                System.Console.WriteLine();
            }
        }
        System.Console.WriteLine();
    }

    private static void RenderStomach(GameState state)
    {
        System.Console.WriteLine($"  ŻOŁĄDEK ({state.Stomach.Count}/{GameRules.MaxStomachSize}):");

        if (state.Stomach.Count == 0)
        {
            System.Console.WriteLine("    (pusty)");
        }
        else
        {
            System.Console.Write("    ");
            for (int i = 0; i < state.Stomach.Cards.Count; i++)
            {
                if (i > 0) System.Console.Write(" | ");
                WriteColored(state.Stomach.Cards[i].Name, state.Stomach.Cards[i].Flavor.ToConsoleColor());
            }
            System.Console.WriteLine();
        }
        System.Console.WriteLine();
    }

    private static void RenderHand(GameState state)
    {
        System.Console.WriteLine($"  RĘKA ({state.Hand.Count}/{GameRules.MaxHandSize}):");

        if (state.Hand.Count == 0)
        {
            WriteColored("    (pusta ręka - PRZEGRANA!)", ConsoleColor.Red);
            System.Console.WriteLine();
        }
        else
        {
            for (int i = 0; i < state.Hand.Cards.Count; i++)
            {
                var card = state.Hand.Cards[i];
                System.Console.Write($"    [{i + 1}] ");
                WriteCardColored(card);
                System.Console.Write($"    (SW:{card.WillpowerCost}, Kal:{card.Calories}) ");
                WriteColored(card.Flavor.ToPolishName(), card.Flavor.ToConsoleColor());
                System.Console.WriteLine();
            }
        }
        System.Console.WriteLine();
    }

    private static void RenderFooter(GameState state)
    {
        System.Console.Write($"  SPIŻARNIA: {state.Pantry.Count} kart");

        if (state.Pantry.TopCardFlavor.HasValue)
        {
            System.Console.Write(" | Wierzchnia: ");
            WriteColored(state.Pantry.TopCardFlavor.Value.ToPolishName(), state.Pantry.TopCardFlavor.Value.ToConsoleColor());
        }
        System.Console.WriteLine();

        System.Console.Write($"  KIBELEK: {state.Toilet.Count} kart");
        System.Console.WriteLine();

        var line = new string('─', 60);
        System.Console.WriteLine(line);

        // Status gry
        switch (state.Phase)
        {
            case GamePhase.Won:
                WriteColored("  *** WYGRANA! Tłuszcz zredukowany do zera! ***", ConsoleColor.Green);
                System.Console.WriteLine();
                break;

            case GamePhase.Lost:
                WriteColored("  *** PRZEGRANA! Brak możliwości ruchu! ***", ConsoleColor.Red);
                System.Console.WriteLine();
                break;

            case GamePhase.AwaitingChoice:
                RenderPendingChoice(state);
                break;

            case GamePhase.AwaitingAction:
                System.Console.WriteLine("  [P]lay <nr>  |  [E]at <nr>  |  [I]nfo <nr>  |  [H]elp  |  [Q]uit");
                break;
        }
    }

    private static void RenderPendingChoice(GameState state)
    {
        if (state.PendingChoice == null) return;

        System.Console.WriteLine();
        WriteColored($"  >>> DECYZJA: {state.PendingChoice.Prompt}", ConsoleColor.Yellow);
        System.Console.WriteLine();

        foreach (var option in state.PendingChoice.Options)
        {
            System.Console.Write($"    [{option.Index + 1}] ");
            WriteColored(option.DisplayText, option.Card.Flavor.ToConsoleColor());
            System.Console.WriteLine();
        }

        System.Console.WriteLine();
        System.Console.WriteLine("  [C]hoose <nr>");
    }

    public static void RenderMessage(string message, ConsoleColor color = ConsoleColor.White)
    {
        WriteColored($"  {message}", color);
        System.Console.WriteLine();
    }

    public static void RenderCard(Card card)
    {
        var line = new string('─', 40);
        System.Console.WriteLine(line);

        System.Console.Write("  ");
        WriteColored(card.Name, card.Flavor.ToConsoleColor());
        System.Console.WriteLine();

        System.Console.WriteLine($"  Smak: {card.Flavor.ToPolishName()}");
        System.Console.WriteLine($"  Koszt SW: {card.WillpowerCost}");
        System.Console.WriteLine($"  Kaloryczność: {card.Calories}");
        System.Console.WriteLine();

        if (!string.IsNullOrEmpty(card.Instruction))
        {
            System.Console.WriteLine($"  {card.Instruction}");
            System.Console.WriteLine();
        }

        if (!string.IsNullOrEmpty(card.FlavorText))
        {
            System.Console.ForegroundColor = ConsoleColor.DarkGray;
            System.Console.WriteLine($"  \"{card.FlavorText}\"");
            System.Console.ResetColor();
        }

        System.Console.WriteLine(line);
    }

    private static void WriteCardColored(Card card)
    {
        WriteColored(card.Name, card.Flavor.ToConsoleColor());
    }

    private static void WriteColored(string text, ConsoleColor color)
    {
        var oldColor = System.Console.ForegroundColor;
        System.Console.ForegroundColor = color;
        System.Console.Write(text);
        System.Console.ForegroundColor = oldColor;
    }

    public static void RenderHelp()
    {
        var line = new string('═', 78);
        System.Console.WriteLine(line);
        WriteColored("  CALADABRA - ZASADY GRY", ConsoleColor.Cyan);
        System.Console.WriteLine();
        System.Console.WriteLine(line);
        System.Console.WriteLine();

        WriteColored("  CEL GRY:", ConsoleColor.Yellow);
        System.Console.WriteLine(" Zredukuj Tłuszcz do 0 w ciągu 30 dni (tur).");
        System.Console.WriteLine();

        WriteColored("  ZAGRYWANIE KART [P]lay:", ConsoleColor.Green);
        System.Console.WriteLine();
        System.Console.WriteLine("    - Kosztuje Siłę Woli (SW) = koszt karty");
        System.Console.WriteLine("    - Karta trafia na Stół lub do Kibelka");
        System.Console.WriteLine("    - Wykonuje efekt karty (np. redukuje Tłuszcz)");
        System.Console.WriteLine();

        WriteColored("  JEDZENIE KART [E]at:", ConsoleColor.Red);
        System.Console.WriteLine();
        System.Console.WriteLine("    - Darmowe (nie kosztuje SW)");
        System.Console.WriteLine("    - Karta trafia do Żołądka (max 4)");
        System.Console.WriteLine("    - Dodaje Kalorie do Tłuszczu");
        System.Console.WriteLine("    - Gdy Żołądek pełny = trawienie do Kibelka");
        System.Console.WriteLine();

        WriteColored("  SIŁA WOLI:", ConsoleColor.Cyan);
        System.Console.WriteLine();
        System.Console.WriteLine("    - Start: 12 SW, Max: 30 SW");
        System.Console.WriteLine("    - Jedzenie karty = odzyskujesz jej koszt SW");
        System.Console.WriteLine("    - Jedzenie kart to sposób na oszczędzanie SW");
        System.Console.WriteLine();

        WriteColored("  PRZEGRANA:", ConsoleColor.DarkRed);
        System.Console.WriteLine(" Po 30 turach z Tłuszczem > 0, lub gdy ręka pusta.");
        System.Console.WriteLine();
        System.Console.WriteLine(line);
    }
}
