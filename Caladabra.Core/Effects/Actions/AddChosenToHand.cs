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
        if (context.ChosenCard == null)
        {
            return EffectResult.Done();
        }

        var card = context.ChosenCard;

        // Usuń kartę ze źródłowej strefy jeśli określono
        if (_fromZone.HasValue)
        {
            switch (_fromZone.Value)
            {
                case ZoneType.Toilet:
                    context.State.Toilet.Remove(card);
                    break;
                case ZoneType.Stomach:
                    // Żołądek to kolejka - nie ma bezpośredniego Remove
                    break;
                case ZoneType.Table:
                    context.State.Table.Remove(card);
                    break;
            }
        }

        // Dodaj do ręki
        context.State.Hand.Add(card);
        context.Emit(new CardDrawnEvent(card)); // Używamy CardDrawnEvent bo karta trafia do ręki

        return EffectResult.Done();
    }
}
