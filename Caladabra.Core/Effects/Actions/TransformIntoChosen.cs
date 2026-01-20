namespace Caladabra.Core.Effects.Actions;

/// <summary>
/// Zamienia kartę źródłową na wybraną kartę (Kwantowa próżnia).
/// </summary>
public sealed class TransformIntoChosen : IEffect
{
    public static readonly TransformIntoChosen Instance = new();

    private TransformIntoChosen() { }

    public EffectResult Execute(EffectContext context)
    {
        if (context.ChosenCard == null || context.SourceCard == null)
            return EffectResult.Done();

        // Znajdź kartę źródłową w ręce po ID (referencja może być nieaktualna po załadowaniu z JSON)
        var hand = context.State.Hand;
        var index = hand.Cards.ToList().FindIndex(c => c.Id == context.SourceCard.Id);
        if (index < 0) return EffectResult.Done();

        // Zamień na sklonowaną wybraną kartę
        hand.ReplaceAt(index, context.ChosenCard.Clone());
        return EffectResult.Done();
    }
}
