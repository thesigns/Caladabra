namespace Caladabra.Core.Effects.Actions;

/// <summary>
/// Marker informujący silnik, że gracz nie dobiera karty po tej akcji.
/// Używane przez np. "Wilczy głód".
/// </summary>
public sealed class SkipDraw : IEffect
{
    public static readonly SkipDraw Instance = new();

    private SkipDraw() { }

    public EffectResult Execute(EffectContext context)
    {
        // Ustaw flagę - silnik sprawdzi to przed dobieraniem karty
        context.ShouldSkipDraw = true;
        return EffectResult.Done();
    }
}
