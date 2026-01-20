namespace Caladabra.Core.Effects.Actions;

/// <summary>
/// Efekt który nic nie robi. Przydatny jako placeholder.
/// </summary>
public sealed class NoEffect : IEffect
{
    public static readonly NoEffect Instance = new();

    private NoEffect() { }

    public EffectResult Execute(EffectContext context)
    {
        return EffectResult.Done();
    }
}
