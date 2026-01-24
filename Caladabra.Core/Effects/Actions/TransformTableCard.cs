namespace Caladabra.Core.Effects.Actions;

/// <summary>
/// Transformuje wybraną kartę na stole - zmienia jej instrukcję i blokuje oryginalne efekty.
/// Używane przez kartę "Było i nie ma".
/// </summary>
public sealed class TransformTableCard : IEffect
{
    private readonly string _newInstruction;
    private readonly int _turnsRemaining;

    public TransformTableCard(string newInstruction, int turnsRemaining)
    {
        _newInstruction = newInstruction;
        _turnsRemaining = turnsRemaining;
    }

    public EffectResult Execute(EffectContext context)
    {
        if (context.ChosenIndices == null || context.ChosenIndices.Length == 0)
            return EffectResult.Done();

        var index = context.ChosenIndices[0];
        var entry = context.State.Table.GetEntryAt(index);

        if (entry == null)
            return EffectResult.Done();

        // Usuń modyfikatory tej karty (np. "Dieta cud" przestaje działać)
        context.State.ActiveModifiers.RemoveAll(m => m.SourceCard.Id == entry.Card.Id);

        // Transformuj kartę
        // turnsRemaining=0 oznacza "usuń przy następnym tick" (bo tick dla bieżącej tury już się odbył)
        entry.IsTransformed = true;
        entry.OverriddenInstruction = _newInstruction;
        entry.TurnsRemaining = _turnsRemaining;

        return EffectResult.Done();
    }
}
