using Caladabra.Core.Effects;
using Caladabra.Core.Effects.Actions;
using Caladabra.Core.Effects.Conditions;
using Caladabra.Core.State;
using Caladabra.Core.Zones;

namespace Caladabra.Core.Cards.Definitions;

/// <summary>
/// Definicje wszystkich kart w grze.
/// </summary>
public static class CardDefinitions
{
    private static bool _registered = false;

    /// <summary>
    /// Rejestruje wszystkie karty w CardRegistry (tylko raz).
    /// </summary>
    public static void RegisterAll()
    {
        if (_registered) return;
        _registered = true;

        var registry = CardRegistry.Instance;

        // === SŁONE (Spalacze) ===
        RegisterSaltyCards(registry);

        // === SŁODKIE (Motywatory) ===
        RegisterSweetCards(registry);

        // === GORZKIE (Blokery) ===
        RegisterBitterCards(registry);

        // === OSTRE (Bomby) ===
        RegisterSpicyCards(registry);

        // === KWAŚNE (Receptury) ===
        RegisterSourCards(registry);

        // === UMAMI (Mutacje) ===
        RegisterUmamiCards(registry);
    }

    private static void RegisterSaltyCards(CardRegistry registry)
    {
        // Wyprawa do toalety - SW:4, Kal:1
        // Po zagraniu: Tłuszcz -8, odrzuć 1 kartę ze Stołu
        registry.Register(new Card
        {
            Id = "wyprawa_do_toalety",
            Name = "Wyprawa do toalety",
            Flavor = Flavor.Salty,
            WillpowerCost = 4,
            Calories = 1,
            FlavorText = "To była dwójka.",
            Instruction = "Po zagraniu tej karty redukujesz Tłuszcz o 8 i odrzucasz 1 dowolną kartę ze Stołu do Kibelka.",
            OnPlay = new Sequence(
                new ReduceFat(8),
                new ChooseCardFromZone(
                    ZoneType.Table,
                    "Wybierz kartę ze Stołu do odrzucenia:",
                    continuation: new DiscardChosenCard(ZoneType.Table)
                )
            )
        });

        // Wspinaczka na I piętro - SW:6, Kal:2
        // Po zagraniu: Tłuszcz -10, połóż na stole na 3 tury.
        // Jeśli usuniesz wcześniej ze stołu, Tłuszcz -10 dodatkowo.
        registry.Register(new Card
        {
            Id = "wspinaczka_na_i_pietro",
            Name = "Wspinaczka na I piętro",
            Flavor = Flavor.Salty,
            WillpowerCost = 6,
            Calories = 2,
            FlavorText = "Jeszcze trzeba zejść.",
            Instruction = "Po zagraniu tej karty redukujesz Tłuszcz o 10. Połóż tę kartę na stole na 3 tury. Jeśli usuniesz ją wcześniej ze stołu, redukujesz Tłuszcz dodatkowo o 10.",
            OnPlay = new Sequence(
                new ReduceFat(10),
                new PlaceOnTable(3)
            ),
            OnLeaveTable = new ReduceFat(10)
        });

        // Łowca dwóch smaków - SW:5, Kal:1
        // Po zagraniu: za każdą słodką/kwaśną w Żołądku -6 Tłuszczu, potem opróżnij Żołądek
        registry.Register(new Card
        {
            Id = "lowca_dwoch_smakow",
            Name = "Łowca dwóch smaków",
            Flavor = Flavor.Salty,
            WillpowerCost = 5,
            Calories = 1,
            FlavorText = "Życie jest słodko-kwaśne. A ja tylko dokładam porcje.",
            Instruction = "Po zagraniu policz wszystkie słodkie i kwaśne karty w Żołądku. Za każdą z nich redukujesz Tłuszcz o 6. Potem opróżniasz zawartość Żołądka do Kibelka.",
            OnPlay = new Sequence(
                ForEachCardInZone.Create(
                    [ZoneType.Stomach],
                    Flavor.Sweet,
                    new ReduceFat(6)
                ),
                ForEachCardInZone.Create(
                    [ZoneType.Stomach],
                    Flavor.Sour,
                    new ReduceFat(6)
                ),
                EmptyStomachToToilet.Instance
            )
        });
    }

    private static void RegisterSweetCards(CardRegistry registry)
    {
        // Lizak na osłodę - SW:1, Kal:0
        // Po zagraniu: za każdą gorzką kartę w Ręce/Żołądku/Stole +3 SW
        registry.Register(new Card
        {
            Id = "lizak_na_oslode",
            Name = "Lizak na osłodę",
            Flavor = Flavor.Sweet,
            WillpowerCost = 1,
            Calories = 0,
            FlavorText = "Czasem coś słodkiego pozwala zapomnieć o tym co gorzkie.",
            Instruction = "Po zagraniu tej karty za każdą gorzką kartę w Ręce, Żołądku i na Stole dostajesz 3 punkty SW.",
            OnPlay = ForEachCardInZone.Create(
                [ZoneType.Hand, ZoneType.Stomach, ZoneType.Table],
                Flavor.Bitter,
                new GainWillpower(3)
            )
        });

        // Baton energetyczny - SW:3, Kal:1
        // Po zagraniu: +10 SW, połóż na stole, po 2 turach -6 SW
        registry.Register(new Card
        {
            Id = "baton_energetyczny",
            Name = "Baton energetyczny",
            Flavor = Flavor.Sweet,
            WillpowerCost = 3,
            Calories = 1,
            FlavorText = "Mózg pracuje na cukrze.",
            Instruction = "Po zagraniu tej karty zyskujesz 10 SW. Połóż tę kartę na Stole. Po dwóch turach przenieś ją do Kibelka i tracisz 6 SW.",
            OnPlay = new Sequence(
                new GainWillpower(10),
                new PlaceOnTable(2)
            ),
            OnTableCounterZero = new LoseWillpower(6)
        });

        // Hat trick - SW:4, Kal:0
        // Po wzięciu do Ręki: +3 SW
        // Po zagraniu: połóż na Stole, po 2 turach +3 SW
        // Po przeniesieniu do Kibelka: +3 SW
        registry.Register(new Card
        {
            Id = "hat_trick",
            Name = "Hat trick",
            Flavor = Flavor.Sweet,
            WillpowerCost = 4,
            Calories = 0,
            FlavorText = "Po takiej serii na pewno przytyję.",
            Instruction = "Po wzięciu tej karty do Ręki zyskujesz 3 SW. Po zagraniu tej karty połóż ją na Stole. Po 2 turach na Stole zyskujesz 3 SW. Po przeniesieniu tej karty do Kibelka zyskujesz 3 SW.",
            OnDraw = new GainWillpower(3),
            OnPlay = new PlaceOnTable(2),
            OnTableCounterZero = new GainWillpower(3),
            OnDiscard = new GainWillpower(3)
        });
    }

    private static void RegisterBitterCards(CardRegistry registry)
    {
        // Wilczy głód - SW:3, Kal:5
        // Po zagraniu: nie ciągniesz karty (ręka -1)
        registry.Register(new Card
        {
            Id = "wilczy_glod",
            Name = "Wilczy głód",
            Flavor = Flavor.Bitter,
            WillpowerCost = 3,
            Calories = 5,
            FlavorText = "Jak czegoś nie zjem, to chyba umrę.",
            Instruction = "Po zagraniu tej karty nie ciągniesz karty ze Spiżarni (Ręka zmniejsza się o 1 kartę).",
            OnPlay = SkipDraw.Instance
        });

        // Diabelski bumerang - SW:1, Kal:4
        // Po zagraniu: połóż na Stole, po 3 turach wróć na wierzch Spiżarni, -3 SW
        registry.Register(new Card
        {
            Id = "diabelski_bumerang",
            Name = "Diabelski bumerang",
            Flavor = Flavor.Bitter,
            WillpowerCost = 1,
            Calories = 4,
            FlavorText = "Czy to wraca karma?",
            Instruction = "Po zagraniu tej karty połóż tę kartę na Stół. Po 3 turach połóż ją z powrotem na wierzchu Spiżarni i tracisz 3 SW.",
            OnPlay = new PlaceOnTable(3),
            OnTableCounterZero = new Sequence(
                ReturnToPantryTop.Instance,
                new LoseWillpower(3)
            )
        });

        // Dostawa jedzenia - SW:1, Kal:5
        // Po zagraniu: wybierz gorzką kartę z Listy Kart Caladabra, dodaj do Ręki
        registry.Register(new Card
        {
            Id = "dostawa_jedzenia",
            Name = "Dostawa jedzenia",
            Flavor = Flavor.Bitter,
            WillpowerCost = 1,
            Calories = 5,
            FlavorText = "Dostawcom chyba śnię się po nocach.",
            Instruction = "Po zagraniu tej karty zamiast dobierać kartę ze Spiżarni wybierz dowolną gorzką kartę z Listy Kart Caladabra i dodaj ją do Ręki.",
            OnPlay = new Sequence(
                new ChooseCardFromZone(
                    ZoneType.CardList,
                    "Wybierz gorzką kartę z Listy Kart Caladabra:",
                    Flavor.Bitter,
                    continuation: AddChosenToHand.Instance
                ),
                SkipDraw.Instance
            )
        });
    }

    private static void RegisterSpicyCards(CardRegistry registry)
    {
        // Łapczywe jedzenie - SW:5, Kal:5
        // Po zagraniu: przenieś 1 kartę z Ręki do Żołądka, dobierz kartę, -3 Tłuszcz jeśli była słona
        registry.Register(new Card
        {
            Id = "lapczywe_jedzenie",
            Name = "Łapczywe jedzenie",
            Flavor = Flavor.Spicy,
            WillpowerCost = 5,
            Calories = 5,
            FlavorText = "Zjadłem zanim postawiłem na stole.",
            Instruction = "Po zagraniu tej karty przenieś jedną wybraną kartę z Ręki do Żołądka i dobierz jeszcze jedną kartę. Zredukuj Tłuszcz o 3 jeśli karta przeniesiona była słona.",
            OnPlay = new ChooseCardFromZone(
                ZoneType.Hand,
                "Wybierz kartę z Ręki do przeniesienia do Żołądka:",
                continuation: new Sequence(
                    MoveChosenToStomach.Instance,
                    new IfChosenCardHasFlavor(Flavor.Salty, new ReduceFat(3)),
                    DrawCard.Instance,
                    SkipDraw.Instance  // Efekt sam dobiera, nie chcemy podwójnego dobierania
                )
            )
        });

        // Grzebanie w kibelku - SW:6, Kal:4
        // Po zagraniu: zamiast dobierać ze Spiżarni, wybierz kartę z Kibelka do Ręki
        registry.Register(new Card
        {
            Id = "grzebanie_w_kibelku",
            Name = "Grzebanie w kibelku",
            Flavor = Flavor.Spicy,
            WillpowerCost = 6,
            Calories = 4,
            FlavorText = "Coś mi tam wpadło.",
            Instruction = "Po zagraniu tej karty zamiast dobierać kartę ze Spiżarni, wybierz dowolną kartę z Kibelka i weź ją do Ręki.",
            OnPlay = new Sequence(
                new ChooseCardFromZone(
                    ZoneType.Toilet,
                    "Wybierz kartę z Kibelka do wzięcia do Ręki:",
                    continuation: AddChosenToHand.FromToilet
                ),
                SkipDraw.Instance
            )
        });

        // Jasnowidzenie - SW:7, Kal:2
        // Po zagraniu: połóż na Stole na 3 tury. Dopóki leży, przy dobieraniu bierzesz +1 i odrzucasz 1.
        registry.Register(new Card
        {
            Id = "jasnowidzenie",
            Name = "Jasnowidzenie",
            Flavor = Flavor.Spicy,
            WillpowerCost = 7,
            Calories = 2,
            FlavorText = "Zobaczyłem przyszłość. Znowu byłem gruby.",
            Instruction = "Po zagraniu tej karty połóż ją na Stole. Dopóki tam leży, dobierając kartę ze Spiżarni możesz wziąć jedną więcej - i jedną z nich odrzucasz. Po trzech turach usuń kartę ze stołu.",
            OnPlay = new PlaceOnTable(3),
            OnEnterTable = new AddModifier(ModifierType.ExtraDrawThenDiscard, 1),
            OnLeaveTable = RemoveModifiersFromSource.Instance
        });
    }

    private static void RegisterSourCards(CardRegistry registry)
    {
        // Sos słodko-kwaśny - SW:5, Kal:3
        // Po zagraniu: połóż na Stole na 3 tury.
        // Przez 3 tury: jeśli w Żołądku słodka I kwaśna -> +8 SW, inaczej -2 SW +2 Tłuszcz
        registry.Register(new Card
        {
            Id = "sos_slodko_kwasny",
            Name = "Sos słodko-kwaśny",
            Flavor = Flavor.Sour,
            WillpowerCost = 5,
            Calories = 3,
            FlavorText = "Idealne połączenie... albo kwaśny żart losu.",
            Instruction = "Po zagraniu tej karty połóż ją na Stole na 3 tury. Przez 3 tury jeśli w Żołądku znajduje się karta o smaku słodkim i kwaśnym – zyskujesz 8 SW i redukujesz 3 Tłuszczu. W przeciwnym razie – tracisz 2 SW i zyskujesz 2 Tłuszczu.",
            OnPlay = new PlaceOnTable(3),
            OnTurnOnTable = new Conditional(
                new HasFlavorsInZone(ZoneType.Stomach, Flavor.Sweet, Flavor.Sour),
                new Sequence(
                    new GainWillpower(8),
                    new ReduceFat(3)
                ),
                new Sequence(
                    new LoseWillpower(2),
                    new GainFat(2)
                )
            )
        });

        // Skutki dietetyczne - SW:5, Kal:3
        // Po zagraniu: połóż na Stole. Co turę: jeśli 3 różne smaki w Żołądku -> +3 SW -5 Tłuszcz
        // inaczej -2 SW +4 Tłuszcz
        registry.Register(new Card
        {
            Id = "skutki_dietetyczne",
            Name = "Skutki dietetyczne",
            Flavor = Flavor.Sour,
            WillpowerCost = 5,
            Calories = 3,
            FlavorText = "Różnorodność buduje... monotonia rujnuje.",
            Instruction = "Po zagraniu połóż tę kartę na Stole. W każdej turze, w której w Żołądku masz 3 różne Smaki, dostajesz +3 SW i redukujesz 5 Tłuszczu. W przeciwnym wypadku tracisz 2 SW i przybierasz 4 Tłuszcze.",
            OnPlay = new PlaceOnTable(-1), // Permanentnie (-1 = bez limitu tur)
            OnTurnOnTable = new Conditional(
                new CountUniqueFlavorsInZone(ZoneType.Stomach, 3),
                new Sequence(
                    new GainWillpower(3),
                    new ReduceFat(5)
                ),
                new Sequence(
                    new LoseWillpower(2),
                    new GainFat(4)
                )
            )
        });

        // Świąteczne śniadanie - SW:6, Kal:2
        // Po zagraniu: połóż na Stole. Po 2 turach: jeśli 3 różne smaki na Stole -> +8 SW -7 Tłuszcz
        // inaczej -5 SW. Usuń ze Stołu.
        registry.Register(new Card
        {
            Id = "swiateczne_sniadanie",
            Name = "Świąteczne śniadanie",
            Flavor = Flavor.Sour,
            WillpowerCost = 6,
            Calories = 2,
            FlavorText = "Czasem musisz poczekać, żeby zobaczyć, czy coś miało sens…",
            Instruction = "Po zagraniu tej karty połóż ją na Stole. Po 2 turach: jeśli na Stole będą 3 różne Smaki, zyskujesz 8 SW i redukujesz 7 Tłuszczu. Jeśli nie — tracisz 5 SW. Tak czy inaczej usuń tę kartę ze Stołu.",
            OnPlay = new PlaceOnTable(2),
            OnTableCounterZero = new Conditional(
                new CountUniqueFlavorsInZone(ZoneType.Table, 3),
                new Sequence(
                    new GainWillpower(8),
                    new ReduceFat(7)
                ),
                new LoseWillpower(5)
            )
        });
    }

    private static void RegisterUmamiCards(CardRegistry registry)
    {
        // Kwantowa próżnia - SW:6, Kal:3
        // Po dobraniu do Ręki: wybierz kartę z Listy Kart Caladabra, stań się tą kartą
        registry.Register(new Card
        {
            Id = "kwantowa_proznia",
            Name = "Kwantowa próżnia",
            Flavor = Flavor.Umami,
            WillpowerCost = 6,
            Calories = 3,
            FlavorText = "Czasem nauka jest nieodróżnialna od magii.",
            Instruction = "Po dobraniu tej karty do ręki wybierz jedną kartę z Listy Kart Caladabra. Kwantowa próżnia staje się tą kartą.",
            OnDraw = new ChooseCardFromZone(
                ZoneType.CardList,
                "Wybierz kartę z Listy Kart Caladabra - Kwantowa próżnia stanie się tą kartą:",
                continuation: TransformIntoChosen.Instance
            )
        });

        // Było i nie ma - SW:5, Kal:1
        // Po zagraniu: wybierz kartę na Stole, zmień jej instrukcję na "Usuń ze stołu w następnej turze"
        registry.Register(new Card
        {
            Id = "bylo_i_nie_ma",
            Name = "Było i nie ma",
            Flavor = Flavor.Umami,
            WillpowerCost = 5,
            Calories = 1,
            FlavorText = "Byłeś. Jesteś. Zaraz cię nie będzie.",
            Instruction = "Po zagraniu tej karty wybierz dowolną kartę na Stole. Zmieniasz instrukcję karty na: Usuń tę kartę ze stołu w następnej turze.",
            OnPlay = new ChooseCardFromZone(
                ZoneType.Table,
                "Wybierz kartę na Stole do usunięcia w następnej turze:",
                continuation: new SetTableCounterTo(1)
            )
        });

        // Dieta cud - SW:7, Kal:2
        // Połóż na Stole na 3 tury. Dopóki leży, dobierane karty mają -3 Kaloryczność.
        // Po zejściu +4 Tłuszcz.
        registry.Register(new Card
        {
            Id = "dieta_cud",
            Name = "Dieta cud",
            Flavor = Flavor.Umami,
            WillpowerCost = 7,
            Calories = 2,
            FlavorText = "Jak każda dieta, działa chwilę.",
            Instruction = "Połóż tę kartę na Stole na 3 tury. Dopóki karta leży na Stole wszystkie dobierane do Ręki mają Kaloryczność zmniejszoną trwale o 3 punkty. Po zejściu karty ze Stołu dodaj 4 Tłuszczu.",
            OnPlay = new PlaceOnTable(3),
            OnEnterTable = new AddModifier(ModifierType.ReduceCaloriesOnDraw, 3),
            OnLeaveTable = new Sequence(
                RemoveModifiersFromSource.Instance,
                new GainFat(4)
            )
        });
    }
}
