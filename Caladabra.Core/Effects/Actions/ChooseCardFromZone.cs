using Caladabra.Core.Cards;
using Caladabra.Core.State;
using Caladabra.Core.Zones;

namespace Caladabra.Core.Effects.Actions;

/// <summary>
/// Wymaga od gracza wyboru karty z określonej strefy.
/// Po wyborze ustawia context.ChosenCard.
/// </summary>
public sealed class ChooseCardFromZone : IEffect
{
    private readonly ZoneType _zone;
    private readonly Flavor? _flavorFilter;
    private readonly string _prompt;
    private readonly IEffect? _continuation;

    public ChooseCardFromZone(ZoneType zone, string prompt, Flavor? flavorFilter = null, IEffect? continuation = null)
    {
        _zone = zone;
        _prompt = prompt;
        _flavorFilter = flavorFilter;
        _continuation = continuation;
    }

    public EffectResult Execute(EffectContext context)
    {
        // Jeśli już mamy wybór - kontynuuj
        if (context.ChosenIndices != null && context.ChosenIndices.Length > 0)
        {
            var zone = context.State.GetZone(_zone);
            var cards = GetFilteredCards(zone);

            if (context.ChosenIndices[0] >= 0 && context.ChosenIndices[0] < cards.Count)
            {
                context.ChosenCard = cards[context.ChosenIndices[0]];
            }

            if (_continuation != null)
            {
                return _continuation.Execute(context);
            }

            return EffectResult.Done();
        }

        // Buduj opcje wyboru
        var sourceZone = context.State.GetZone(_zone);
        var availableCards = GetFilteredCards(sourceZone);

        if (availableCards.Count == 0)
        {
            // Brak kart do wyboru - pomijamy
            return EffectResult.Done();
        }

        var options = availableCards.Select((card, index) => new ChoiceOption
        {
            Index = index,
            Card = card,
            DisplayText = card.ToString()
        }).ToList();

        var choice = new PendingChoice
        {
            Type = GetChoiceType(),
            Prompt = _prompt,
            Options = options,
            FlavorFilter = _flavorFilter,
            Continuation = this,
            SourceCard = context.SourceCard
        };

        return EffectResult.NeedsChoice(choice, this);
    }

    private List<Card> GetFilteredCards(IZone zone)
    {
        var cards = zone.Cards.ToList();

        if (_flavorFilter.HasValue)
        {
            cards = cards.Where(c => c.Flavor == _flavorFilter.Value).ToList();
        }

        return cards;
    }

    private ChoiceType GetChoiceType() => _zone switch
    {
        ZoneType.Hand => ChoiceType.SelectFromHand,
        ZoneType.Table => ChoiceType.SelectFromTable,
        ZoneType.Stomach => ChoiceType.SelectFromStomach,
        ZoneType.Toilet => ChoiceType.SelectFromToilet,
        ZoneType.Pantry => ChoiceType.SelectFromPantry,
        ZoneType.CardList when _flavorFilter.HasValue => ChoiceType.SelectFromCardListFiltered,
        ZoneType.CardList => ChoiceType.SelectFromCardList,
        _ => ChoiceType.SelectFromHand
    };
}
