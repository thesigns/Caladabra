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
        // Użyj indeksu zamiast referencji (po JSON restore referencje nie działają)
        if (context.ChosenIndices == null || context.ChosenIndices.Length == 0)
            return EffectResult.Done();

        var index = context.ChosenIndices[0];
        var entries = context.State.Table.Entries.ToList();

        if (index < 0 || index >= entries.Count)
            return EffectResult.Done();

        entries[index].TurnsRemaining = _turns;

        return EffectResult.Done();
    }
}
