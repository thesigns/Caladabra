using Caladabra.Core.Cards;
using Caladabra.Desktop.Core;
using SFML.Graphics;
using SFML.System;

namespace Caladabra.Desktop.Rendering;

/// <summary>
/// Renders a card with a progressive "eating" visual effect using mask textures.
/// The card appears to be bitten/eaten as progress increases.
/// </summary>
public sealed class CardEatingEffect : IDisposable
{
    private readonly AssetManager _assets;
    private readonly Texture[] _maskTextures;
    private RenderTexture? _compositeTexture;
    private uint _currentTextureWidth;
    private uint _currentTextureHeight;

    /// <summary>
    /// Eating progress from 0.0 (not started) to 1.0 (fully eaten).
    /// </summary>
    public float Progress { get; set; }

    /// <summary>
    /// Whether the eating effect is currently active.
    /// </summary>
    public bool IsActive { get; set; }

    public CardEatingEffect(AssetManager assets)
    {
        _assets = assets;

        // Load all available mask textures
        var maskCount = assets.EatingMaskCount;
        _maskTextures = new Texture[maskCount];
        for (int i = 0; i < maskCount; i++)
        {
            _maskTextures[i] = assets.GetEatingMask(i);
        }
    }

    /// <summary>
    /// Draws a card with the eating effect applied based on current Progress.
    /// </summary>
    public void DrawEatingCard(
        RenderWindow window,
        Card card,
        Vector2f position,
        CardDisplayMode mode,
        float scale,
        CardRenderer renderer,
        Color? tint = null)
    {
        if (_maskTextures.Length == 0)
        {
            // No masks available - fall back to normal rendering
            renderer.Draw(window, card, position, mode, scale, tint);
            return;
        }

        var cardSize = renderer.GetCardSize(scale);
        var width = (uint)cardSize.X;
        var height = (uint)cardSize.Y;

        // Ensure RenderTexture exists and is correct size
        EnsureRenderTexture(width, height);

        // 1. Clear and render card to texture at origin (0,0)
        _compositeTexture!.Clear(Color.Transparent);
        renderer.Draw(_compositeTexture, card, new Vector2f(0, 0), mode, scale, tint);

        // 2. Determine which mask to apply based on progress
        int maskIndex = GetMaskIndex(Progress);

        if (maskIndex >= 0)
        {
            // 3. Apply mask - mask alpha controls what remains visible
            // Mask should have: alpha=255 where visible, alpha=0 where eaten
            // Both card and mask are rendered normally here, VertexArray flips both at the end
            var maskTexture = _maskTextures[maskIndex];
            var maskSprite = new Sprite(maskTexture)
            {
                Scale = new Vector2f(
                    cardSize.X / maskTexture.Size.X,
                    cardSize.Y / maskTexture.Size.Y)
            };

            // BlendMode that multiplies destination by source alpha
            // dst.rgb = dst.rgb * src.a, dst.a = dst.a * src.a
            var maskBlend = new BlendMode(
                BlendMode.Factor.Zero,          // srcColorFactor - ignore mask color
                BlendMode.Factor.SrcAlpha,      // dstColorFactor - multiply by mask alpha
                BlendMode.Equation.Add,
                BlendMode.Factor.Zero,          // srcAlphaFactor
                BlendMode.Factor.SrcAlpha,      // dstAlphaFactor - multiply by mask alpha
                BlendMode.Equation.Add
            );

            _compositeTexture.Draw(maskSprite, new RenderStates(maskBlend));
        }

        _compositeTexture.Display();

        // 4. Draw the composited result to the window
        // In SFML.Net 3, RenderTexture does NOT have flipped Y - just draw normally
        var resultSprite = new Sprite(_compositeTexture.Texture)
        {
            Position = position
        };
        window.Draw(resultSprite);
    }

    /// <summary>
    /// Maps progress (0.0-1.0) to mask index (-1 = no mask, 0-N = mask index).
    /// </summary>
    private int GetMaskIndex(float progress)
    {
        if (_maskTextures.Length == 0)
            return -1;

        // Divide progress into equal segments
        // First segment (0 to 1/N+1) = no mask
        // Remaining segments map to masks 0, 1, 2, ...
        float segmentSize = 1.0f / (_maskTextures.Length + 1);

        if (progress < segmentSize)
            return -1;  // No mask yet

        int index = (int)((progress - segmentSize) / segmentSize);
        return Math.Min(index, _maskTextures.Length - 1);
    }

    private void EnsureRenderTexture(uint width, uint height)
    {
        if (_compositeTexture == null ||
            _currentTextureWidth != width ||
            _currentTextureHeight != height)
        {
            _compositeTexture?.Dispose();
            _compositeTexture = new RenderTexture(new Vector2u(width, height));
            _compositeTexture.Smooth = true;
            _currentTextureWidth = width;
            _currentTextureHeight = height;
        }
    }

    public void Dispose()
    {
        _compositeTexture?.Dispose();
        _compositeTexture = null;
    }
}
