using SFML.Graphics;

namespace Caladabra.Desktop.Animation;

/// <summary>
/// Interfejs bazowy dla wszystkich animacji.
/// </summary>
public interface IAnimation
{
    /// <summary>Czy animacja się zakończyła.</summary>
    bool IsComplete { get; }

    /// <summary>Aktualizuje stan animacji.</summary>
    /// <param name="deltaTime">Czas od ostatniej klatki w sekundach.</param>
    void Update(float deltaTime);

    /// <summary>Renderuje animację.</summary>
    void Render(RenderWindow window);
}
