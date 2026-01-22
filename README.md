# Caladabra

**Willpower is Magic**

A solo card game where you fight the toughest enemy of all: yourself.

---

In a world where magic is real, wizards don't live in towers. They live in cramped apartments, have mortgages, weight problems, and issues with self-control like everyone else. After graduating from Caladabra — a not-so-prestigious but wonderfully absurd magic academy — students don't get epic quests or dragon-slaying contracts. Instead, they land in gray reality where the biggest enemy isn't a demon, but everyday life.

The school reunion is coming up. It's your last chance to prove your robe still fits. You want to lose that excess Fat, but that takes a lot of Willpower.

Every day brings new cards to your hand — or new problems. You exercise, eat, cast spells, and face the whims of fate... but above all, you face your appetite and the ever-present wolf hunger. Will you choose the salad and a dawn jog, or take shortcuts and burn fat with a spell?

Caladabra doesn't pretend to be educational. It's an absurd, irony-filled battle with yourself, where every turn is a new attempt to reconcile dieting with magic.

---

## The Core Dilemma

You have a hand of cards. Each turn, you must do ONE thing:

**Play a card** — spend Willpower, trigger its effect.

**Eat a card** — gain calories (Fat), recover Willpower.

That's it. Simple rules. Brutal decisions.

Playing cards costs Willpower. To regain Willpower, you must eat. But eating adds Fat. And you're trying to *lose* Fat. See the problem?

---

## Your Resources

| Resource | Start | Goal |
|----------|-------|------|
| **Fat** | 100 | Get to 0 to win |
| **Willpower** | 12 | Spend to play cards, recover by eating |

- **Win:** Reduce Fat to 0 within 30 turns
- **Lose:** Run out of cards OR exceed 30 turns with Fat > 0

---

## Card Flavors

Every card has a taste. Literally.

| Flavor | Archetype | Personality |
|--------|-----------|-------------|
| **Salty** | Burners | Hard work. Sweat. Direct fat loss. |
| **Sweet** | Motivators | Quick Willpower boost... with a catch. |
| **Bitter** | Blockers | Bad choices. High calories, nasty effects. |
| **Spicy** | Bombs | Chaos. Rule-breakers. High risk, high reward. |
| **Sour** | Recipes | Delayed effects. Patience pays off. |
| **Umami** | Mutations | Transform cards. Change the game. |

---

## Status

Work in progress. Playable in **Desktop** (SFML.Net) and **Console** modes.

### Desktop (recommended)
```bash
dotnet run --project Caladabra.Desktop
```
- Full graphical interface with card textures
- Smooth card animations between zones
- Playable cards visually raised (elevation system)
- Card preview on hover
- LPM = play card, PPM = eat card
- CardList browser with scrolling
- Zone pickers (Toilet, Pantry selection)
- Custom seed input for reproducible games
- Options menu with resolution settings

### Console
```bash
# Interactive mode
dotnet run --project Caladabra.Console -- interactive

# JSON mode (AI-friendly)
dotnet run --project Caladabra.Console -- new
dotnet run --project Caladabra.Console -- play 0
dotnet run --project Caladabra.Console -- eat 2
```

---

## Tech Stack

- C# / .NET 9
- Core library (game logic)
- Desktop client (SFML.Net 3.0.0)
- Console runner (interactive + JSON API)

---

## License

See [LICENSE.txt](LICENSE.txt)

Copyright (C) 2025-2026 Jakub W. Adamczyk

---

*While making this game, the author was struggling with obesity. And with himself.*
