using SFML.System;

namespace Caladabra.Desktop.Core;

public sealed class ScaleManager
{
    public const float BaseWidth = 1920f;
    public const float BaseHeight = 1080f;

    public float Scale { get; private set; } = 1.0f;
    public uint CurrentWidth { get; private set; } = 1920;
    public uint CurrentHeight { get; private set; } = 1080;

    public float OffsetX => (CurrentWidth - BaseWidth * Scale) / 2;
    public float OffsetY => (CurrentHeight - BaseHeight * Scale) / 2;

    public void UpdateScale(uint width, uint height)
    {
        CurrentWidth = width;
        CurrentHeight = height;
        Scale = Math.Min(width / BaseWidth, height / BaseHeight);
    }

    public float S(float value) => value * Scale;

    public uint S(uint value) => (uint)(value * Scale);

    public int S(int value) => (int)(value * Scale);

    public Vector2f S(Vector2f v) => new(v.X * Scale, v.Y * Scale);

    public Vector2f S(float x, float y) => new(x * Scale, y * Scale);

    public Vector2f Position(float x, float y) => new(OffsetX + x * Scale, OffsetY + y * Scale);
}
