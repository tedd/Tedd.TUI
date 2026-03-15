using System;

namespace Tedd.TUI.Archive;

public class TextBoxLegacy : UIElement
{
    public TextBoxLegacy()
    {
        Focusable = true;
    }
    private int _cursorPos = 0;
    private bool _isUserInput = false;

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(TextBoxLegacy), string.Empty);

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly DependencyProperty IsPasswordProperty =
        DependencyProperty.Register(nameof(IsPassword), typeof(bool), typeof(TextBoxLegacy), false);

    public bool IsPassword
    {
        get => (bool)GetValue(IsPasswordProperty);
        set => SetValue(IsPasswordProperty, value);
    }

    public static readonly DependencyProperty PasswordCharProperty =
        DependencyProperty.Register(nameof(PasswordChar), typeof(char), typeof(TextBoxLegacy), '*');

    public char PasswordChar
    {
        get => (char)GetValue(PasswordCharProperty);
        set => SetValue(PasswordCharProperty, value);
    }

    protected override void OnPropertyChanged(DependencyProperty dp)
    {
        base.OnPropertyChanged(dp);

        if (dp == TextProperty && !_isUserInput)
        {
            var text = Text ?? "";
            _cursorPos = text.Length;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(Width > 0 ? Width : 20, 1);
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;
        int w = RenderSize.Width;

        var fg = IsFocused ? ConsoleColor.Yellow : ConsoleColor.White;
        var effectiveBg = IsFocused ? ConsoleColor.DarkBlue : (Background ?? buffer.GetPixel(x, y).Background);
        var bg = effectiveBg;

        string text = Text ?? "";
        string display = IsPassword ? new string(PasswordChar, text.Length) : text;

        int start = 0;
        if (display.Length > w)
        {
            if (_cursorPos >= w)
                start = _cursorPos - w + 1;
        }

        for (int i = 0; i < w; i++)
        {
            char c = ' ';
            int textIdx = start + i;
            if (textIdx < display.Length) c = display[textIdx];

            var cellBg = bg;
            var cellFg = fg;

            if (IsFocused && textIdx == _cursorPos)
            {
                cellBg = ConsoleColor.Gray;
                cellFg = ConsoleColor.Black;
            }

            buffer.SetPixel(x + i, y, c, cellFg, cellBg);
        }

        if (IsFocused && _cursorPos == display.Length && display.Length - start < w)
        {
            buffer.SetPixel(x + (display.Length - start), y, ' ', ConsoleColor.Black, ConsoleColor.Gray);
        }
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        e.Handled = true;
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        string text = Text ?? "";

        if (e.Key == ConsoleKey.LeftArrow)
        {
            if (_cursorPos > 0) _cursorPos--;
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.RightArrow)
        {
            if (_cursorPos < text.Length) _cursorPos++;
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.Backspace)
        {
            if (_cursorPos > 0 && text.Length > 0)
            {
                _isUserInput = true;
                Text = text.Remove(_cursorPos - 1, 1);
                _isUserInput = false;
                _cursorPos--;
            }
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.Delete)
        {
            if (_cursorPos < text.Length)
            {
                _isUserInput = true;
                Text = text.Remove(_cursorPos, 1);
                _isUserInput = false;
            }
            e.Handled = true;
        }
        else if (!char.IsControl(e.KeyChar))
        {
            _isUserInput = true;
            Text = text.Insert(_cursorPos, e.KeyChar.ToString());
            _isUserInput = false;
            _cursorPos++;
            e.Handled = true;
        }
    }
}
