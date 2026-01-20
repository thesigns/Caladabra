namespace Caladabra.Core.Effects.Actions;

/// <summary>
/// Usuwa wszystkie modyfikatory utworzone przez kartę źródłową.
/// Używane gdy karta opuszcza stół.
/// </summary>
public sealed class RemoveModifiersFromSource : IEffect
{
    public static readonly RemoveModifiersFromSource Instance = new();

    private RemoveModifiersFromSource() { }

    public EffectResult Execute(EffectContext context)
    {
        context.State.ActiveModifiers.RemoveAll(m => m.SourceCard == context.SourceCard);
        return EffectResult.Done();
    }
}
