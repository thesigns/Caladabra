using Caladabra.Core.Cards;
using Caladabra.Core.Events;
using Caladabra.Core.Zones;

namespace Caladabra.Core.Effects.Actions;

/// <summary>
/// Dodaje wybraną kartę (context.ChosenCard) do ręki gracza.
/// Usuwa kartę ze źródłowej strefy jeśli trzeba.
/// </summary>
public sealed class AddChosenToHand : IEffect
{
    private readonly ZoneType? _fromZone;

    /// <summary>
    /// Tworzy efekt dodający wybraną kartę do ręki.
    /// </summary>
    /// <param name="fromZone">Strefa źródłowa do usunięcia karty (null = nie usuwaj, np. dla CardList)</param>
    public AddChosenToHand(ZoneType? fromZone = null)
    {
        _fromZone = fromZone;
    }

    /// <summary>
    /// Singleton dla przypadku gdy nie trzeba usuwać ze strefy (np. CardList).
    /// </summary>
    public static readonly AddChosenToHand Instance = new();

    /// <summary>
    /// Instancja usuwająca kartę z Kibelka.
    /// </summary>
    public static readonly AddChosenToHand FromToilet = new(ZoneType.Toilet);

    public EffectResult Execute(EffectContext context)
    {
        Card? card;

        // Dla stref wymagających usunięcia, użyj indeksu (po JSON restore referencje nie działają)
        if (_fromZone.HasValue && context.ChosenIndices != null && context.ChosenIndices.Length > 0)
        {
            var index = context.ChosenIndices[0];

            switch (_fromZone.Value)
            {
                case ZoneType.Toilet:
                    card = context.State.Toilet.RemoveAt(index);
                    break;
                case ZoneType.Table:
                    var entries = context.State.Table.Entries.ToList();
                    if (index >= 0 && index < entries.Count)
                    {
                        card = entries[index].Card;
                        context.State.Table.Remove(card);
                        // Wykonaj OnLeaveTable
                        if (card.OnLeaveTable != null)
                        {
                            card.OnLeaveTable.Execute(context);
                        }
                    }
                    else
                    {
                        return EffectResult.Done();
                    }
                    break;
                default:
                    card = context.ChosenCard;
                    break;
            }
        }
        else
        {
            // Dla CardList - używamy klona z ChosenCard
            card = context.ChosenCard?.Clone();
        }

        if (card == null)
            return EffectResult.Done();

        // Dodaj do ręki
        context.State.Hand.Add(card);
        context.Emit(new CardDrawnEvent(card));

        return EffectResult.Done();
    }
}
