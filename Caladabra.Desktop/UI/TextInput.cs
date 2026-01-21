using SFML.Graphics;
using SFML.System;
using SFML.Window;
using Caladabra.Desktop.Core;

namespace Caladabra.Desktop.UI;

public sealed class TextInput
{
    private readonly Font _font;
    private readonly ScaleManager _scale;

    private string _text = "";
    private bool _isFocused = true;
    private float _cursorBlinkTimer;
    private bool _cursorVisible = true;

    private const float CursorBlinkInterval = 0.5f;

    public string Text
    {
        get => _text;
        set => _text = value ?? "";
    }

    public string Placeholder { get; set; } = "";
    public int MaxLength { get; set; } = 20;
    public bool DigitsOnly { get; set; }
    public Vector2f Position { get; set; }
    public Vector2f Size { get; set; }
    public bool IsFocused
    {
        get => _isFocused;
        set => _isFocused = value;
    }

    public Action<string>? OnSubmit { get; set; }

    public TextInput(Font font, ScaleManager scale, Vector2f position, Vector2f size)
    {
        _font = font;
        _scale = scale;
        Position = position;
        Size = size;
    }

    public void HandleTextEntered(uint unicode)
    {
        if (!_isFocused) return;

        // Ignoruj znaki kontrolne (poza Backspace który jest obsługiwany w HandleKeyPressed)
        if (unicode < 32 || unicode == 127) return;

        // Filtruj tylko cyfry jeśli DigitsOnly
        if (DigitsOnly && (unicode < '0' || unicode > '9')) return;

        // Sprawdź limit długości
        if (_text.Length >= MaxLength) return;

        _text += (char)unicode;
        ResetCursorBlink();
    }

    public void HandleKeyPressed(Keyboard.Key key)
    {
        if (!_isFocused) return;

        switch (key)
        {
            case Keyboard.Key.Backspace:
                if (_text.Length > 0)
                {
                    _text = _text[..^1];
                    ResetCursorBlink();
                }
                break;

            case Keyboard.Key.Enter:
                OnSubmit?.Invoke(_text);
                break;
        }
    }

    public void Update(float deltaTime)
    {
        if (!_isFocused) return;

        _cursorBlinkTimer += deltaTime;
        if (_cursorBlinkTimer >= CursorBlinkInterval)
        {
            _cursorBlinkTimer = 0;
            _cursorVisible = !_cursorVisible;
        }
    }

    public void Draw(RenderWindow window)
    {
        // Tło
        var background = new RectangleShape(Size)
        {
            Position = Position,
            FillColor = Theme.BackgroundDark,
            OutlineColor = _isFocused ? Theme.AccentPrimary : Theme.BackgroundLight,
            OutlineThickness = _scale.S(2f)
        };
        window.Draw(background);

        // Tekst lub placeholder
        var displayText = string.IsNullOrEmpty(_text) ? Placeholder : _text;
        var textColor = string.IsNullOrEmpty(_text) ? Theme.TextMuted : Theme.TextPrimary;

        var text = new Text(_font, displayText, (uint)_scale.S(Theme.FontSizeMedium))
        {
            FillColor = textColor
        };

        // Wyśrodkuj tekst pionowo
        var textBounds = text.GetLocalBounds();
        float textX = Position.X + _scale.S(10f);
        float textY = Position.Y + (Size.Y - textBounds.Size.Y) / 2 - textBounds.Position.Y;
        text.Position = new Vector2f(textX, textY);

        window.Draw(text);

        // Kursor (tylko gdy focused i tekst nie jest placeholder)
        if (_isFocused && _cursorVisible && !string.IsNullOrEmpty(_text) || (_isFocused && _cursorVisible && string.IsNullOrEmpty(_text)))
        {
            // Pozycja kursora na końcu tekstu
            float cursorX;
            if (string.IsNullOrEmpty(_text))
            {
                cursorX = textX;
            }
            else
            {
                var actualText = new Text(_font, _text, (uint)_scale.S(Theme.FontSizeMedium));
                var actualBounds = actualText.GetLocalBounds();
                cursorX = textX + actualBounds.Size.X + _scale.S(2f);
            }

            var cursor = new RectangleShape(new Vector2f(_scale.S(2f), _scale.S(Theme.FontSizeMedium + 4)))
            {
                Position = new Vector2f(cursorX, textY),
                FillColor = Theme.TextPrimary
            };
            window.Draw(cursor);
        }
    }

    private void ResetCursorBlink()
    {
        _cursorBlinkTimer = 0;
        _cursorVisible = true;
    }
}
