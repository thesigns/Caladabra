using SFML.Graphics;
using SFML.System;
using Caladabra.Desktop.Core;

namespace Caladabra.Desktop.UI;

public sealed class Button
{
    private readonly Font _font;
    private readonly ScaleManager _scale;

    private string _text;
    private Vector2f _position;
    private Vector2f _size;
    private bool _isHovered;
    private bool _isPressed;
    private bool _isEnabled = true;

    public string Text
    {
        get => _text;
        set => _text = value;
    }

    public Vector2f Position
    {
        get => _position;
        set => _position = value;
    }

    public Vector2f Size
    {
        get => _size;
        set => _size = value;
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => _isEnabled = value;
    }

    public bool IsHovered => _isHovered;

    public Action? OnClick { get; set; }

    public Button(Font font, ScaleManager scale, string text, Vector2f position, Vector2f size)
    {
        _font = font;
        _scale = scale;
        _text = text;
        _position = position;
        _size = size;
    }

    public bool ContainsPoint(Vector2f point)
    {
        return point.X >= _position.X && point.X <= _position.X + _size.X &&
               point.Y >= _position.Y && point.Y <= _position.Y + _size.Y;
    }

    public void UpdateHover(Vector2f mousePosition)
    {
        _isHovered = _isEnabled && ContainsPoint(mousePosition);
    }

    public void HandlePress(Vector2f mousePosition)
    {
        if (_isEnabled && ContainsPoint(mousePosition))
        {
            _isPressed = true;
        }
    }

    public void HandleRelease(Vector2f mousePosition)
    {
        if (_isPressed && _isEnabled && ContainsPoint(mousePosition))
        {
            OnClick?.Invoke();
        }
        _isPressed = false;
    }

    public void Draw(RenderWindow window)
    {
        // Background
        var bgColor = GetBackgroundColor();
        var background = new RectangleShape(_size)
        {
            Position = _position,
            FillColor = bgColor,
            OutlineColor = GetBorderColor(),
            OutlineThickness = _scale.S(2f)
        };
        window.Draw(background);

        // Text
        var textColor = _isEnabled ? Theme.TextPrimary : Theme.TextMuted;
        var text = new Text(_font, _text, (uint)_scale.S(Theme.FontSizeMedium))
        {
            FillColor = textColor
        };

        // Center text in button
        var textBounds = text.GetLocalBounds();
        text.Position = new Vector2f(
            _position.X + (_size.X - textBounds.Size.X) / 2 - textBounds.Position.X,
            _position.Y + (_size.Y - textBounds.Size.Y) / 2 - textBounds.Position.Y
        );

        window.Draw(text);
    }

    private Color GetBackgroundColor()
    {
        if (!_isEnabled)
            return Theme.BackgroundMedium;
        if (_isPressed && _isHovered)
            return new Color(70, 110, 170);
        if (_isHovered)
            return Theme.AccentPrimary;
        return Theme.BackgroundLight;
    }

    private Color GetBorderColor()
    {
        if (!_isEnabled)
            return Theme.BackgroundLight;
        if (_isHovered)
            return new Color(130, 180, 255);
        return Theme.AccentPrimary;
    }
}
