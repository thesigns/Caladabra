using SFML.Graphics;

namespace Caladabra.Desktop.Animation;

/// <summary>
/// Zarządza animacjami kart i blokuje interakcje podczas ich trwania.
/// </summary>
public sealed class AnimationManager
{
    private readonly List<IAnimation> _activeAnimations = new();
    private readonly HashSet<string> _animatingCardIds = new();

    /// <summary>Czy trwają jakiekolwiek animacje (blokada interakcji).</summary>
    public bool IsAnimating => _activeAnimations.Count > 0;

    /// <summary>Czy karta o danym ID jest aktualnie animowana.</summary>
    public bool IsCardAnimating(string cardId) => _animatingCardIds.Contains(cardId);

    /// <summary>Uruchamia animację natychmiast (równolegle z innymi).</summary>
    public void StartImmediate(IAnimation animation)
    {
        _activeAnimations.Add(animation);

        if (animation is CardMoveAnimation cardAnim)
        {
            _animatingCardIds.Add(cardAnim.Card.Id);
        }
    }

    /// <summary>Uruchamia grupę animacji równolegle.</summary>
    public void StartParallel(IEnumerable<IAnimation> animations)
    {
        foreach (var anim in animations)
        {
            StartImmediate(anim);
        }
    }

    /// <summary>Aktualizuje wszystkie aktywne animacje.</summary>
    public void Update(float deltaTime)
    {
        // Aktualizuj aktywne animacje
        for (int i = _activeAnimations.Count - 1; i >= 0; i--)
        {
            var anim = _activeAnimations[i];
            anim.Update(deltaTime);

            if (anim.IsComplete)
            {
                _activeAnimations.RemoveAt(i);

                if (anim is CardMoveAnimation cardAnim)
                {
                    _animatingCardIds.Remove(cardAnim.Card.Id);
                }
            }
        }
    }

    /// <summary>Renderuje wszystkie aktywne animacje (nad normalnym UI).</summary>
    public void Render(RenderWindow window)
    {
        foreach (var anim in _activeAnimations)
        {
            anim.Render(window);
        }
    }

    /// <summary>Anuluje wszystkie animacje.</summary>
    public void Clear()
    {
        _activeAnimations.Clear();
        _animatingCardIds.Clear();
    }
}
