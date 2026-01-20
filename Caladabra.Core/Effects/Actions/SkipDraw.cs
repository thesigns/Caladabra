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
        // To jest tylko marker - silnik sprawdza czy efekt zawiera SkipDraw
        return EffectResult.Done();
    }
}
