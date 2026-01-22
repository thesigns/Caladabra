namespace Caladabra.Desktop.Animation;

/// <summary>
/// Funkcje easingu dla płynnych animacji.
/// </summary>
public static class Easing
{
    /// <summary>Delegat funkcji easingu.</summary>
    /// <param name="t">Postęp animacji (0.0 - 1.0)</param>
    /// <returns>Wartość z easingiem (0.0 - 1.0, może wychodzić poza zakres dla efektów bounce)</returns>
    public delegate float EasingFunction(float t);

    /// <summary>Liniowa interpolacja (brak easingu).</summary>
    public static float Linear(float t) => t;

    /// <summary>Łagodne wejście i wyjście (kwadratowa - szybsza).</summary>
    public static float EaseInOutQuad(float t) =>
        t < 0.5f
            ? 2f * t * t
            : 1f - MathF.Pow(-2f * t + 2f, 2f) / 2f;

    /// <summary>Łagodne wejście i wyjście (kubiczna - płynniejsza).</summary>
    public static float EaseInOutCubic(float t) =>
        t < 0.5f
            ? 4f * t * t * t
            : 1f - MathF.Pow(-2f * t + 2f, 3f) / 2f;

    /// <summary>Efekt "odbicia" na końcu - element wychodzi poza cel i wraca.</summary>
    public static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * MathF.Pow(t - 1f, 3f) + c1 * MathF.Pow(t - 1f, 2f);
    }

    /// <summary>Szybkie wyjście z opóźnieniem na końcu.</summary>
    public static float EaseOutQuad(float t) =>
        1f - (1f - t) * (1f - t);

    /// <summary>Szybkie wyjście (kubiczne).</summary>
    public static float EaseOutCubic(float t) =>
        1f - MathF.Pow(1f - t, 3f);
}
