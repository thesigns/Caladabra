using SFML.Graphics;
using SFML.System;
using Caladabra.Core.Cards;
using Caladabra.Desktop.Rendering;

namespace Caladabra.Desktop.Animation;

/// <summary>
/// Animacja przelotu karty z punktu A do punktu B z opcjonalnym easingiem.
/// </summary>
public sealed class CardMoveAnimation : IAnimation
{
    private readonly CardRenderer _cardRenderer;
    private readonly Card _card;

    private readonly Vector2f _startPosition;
    private readonly Vector2f _endPosition;
    private readonly float _startScale;
    private readonly float _endScale;
    private readonly CardDisplayMode _startMode;
    private readonly CardDisplayMode _endMode;

    private readonly float _duration;
    private readonly Easing.EasingFunction _easingFunction;

    private float _elapsed;

    // Bieżący stan animacji
    private Vector2f _currentPosition;
    private float _currentScale;
    private CardDisplayMode _currentMode;

    public bool IsComplete => _elapsed >= _duration;

    /// <summary>Karta będąca animowana.</summary>
    public Card Card => _card;

    /// <summary>Callback wywoływany po zakończeniu animacji.</summary>
    public Action? OnComplete { get; set; }

    public CardMoveAnimation(
        CardRenderer cardRenderer,
        Card card,
        Vector2f startPosition,
        Vector2f endPosition,
        float duration = 0.3f,
        Easing.EasingFunction? easing = null,
        float startScale = 1.0f,
        float endScale = 1.0f,
        CardDisplayMode startMode = CardDisplayMode.Small,
        CardDisplayMode endMode = CardDisplayMode.Small)
    {
        _cardRenderer = cardRenderer;
        _card = card;
        _startPosition = startPosition;
        _endPosition = endPosition;
        _startScale = startScale;
        _endScale = endScale;
        _startMode = startMode;
        _endMode = endMode;
        _duration = duration;
        _easingFunction = easing ?? Easing.EaseInOutCubic;

        _elapsed = 0f;
        _currentPosition = startPosition;
        _currentScale = startScale;
        _currentMode = startMode;
    }

    public void Update(float deltaTime)
    {
        if (IsComplete) return;

        _elapsed += deltaTime;
        float t = Math.Clamp(_elapsed / _duration, 0f, 1f);
        float easedT = _easingFunction(t);

        // Interpoluj pozycję
        _currentPosition = new Vector2f(
            Lerp(_startPosition.X, _endPosition.X, easedT),
            Lerp(_startPosition.Y, _endPosition.Y, easedT)
        );

        // Interpoluj skalę
        _currentScale = Lerp(_startScale, _endScale, easedT);

        // Zmień tryb wyświetlania w połowie animacji
        _currentMode = t < 0.5f ? _startMode : _endMode;

        if (IsComplete)
        {
            OnComplete?.Invoke();
        }
    }

    public void Render(RenderWindow window)
    {
        _cardRenderer.Draw(
            window,
            _card,
            _currentPosition,
            _currentMode,
            _currentScale
        );
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
