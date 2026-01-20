namespace Caladabra.Core.Effects.Actions;

/// <summary>
/// Ustawia licznik tur wybranej karty na stole na określoną wartość.
/// </summary>
public sealed class SetTableCounterTo : IEffect
{
    private readonly int _turns;

    public SetTableCounterTo(int turns)
    {
        _turns = turns;
    }

    public EffectResult Execute(EffectContext context)
    {
        if (context.ChosenCard == null)
        {
            return EffectResult.Done();
        }

        var card = context.ChosenCard;

        // Znajdź wpis na stole
        var entry = context.State.Table.Entries
            .FirstOrDefault(e => e.Card == card);

        if (entry != null)
        {
            entry.TurnsRemaining = _turns;
        }

        return EffectResult.Done();
    }
}
