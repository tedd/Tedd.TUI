using System;

namespace Tedd.TUI;

public class TextBox : UIElement
{
    public TextBox()
    {
        Focusable = true;
    }
    private int _cursorPos = 0;
    private bool _isUserInput = false;

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register("Text", typeof(string), typeof(TextBox), string.Empty);

    public string Text
    {
        get { return (string)GetValue(TextProperty); }
        set { SetValue(TextProperty, value); }
    }

    public static readonly DependencyProperty IsPasswordProperty =
        DependencyProperty.Register("IsPassword", typeof(bool), typeof(TextBox), false);

    public bool IsPassword
    {
        get { return (bool)GetValue(IsPasswordProperty); }
        set { SetValue(IsPasswordProperty, value); }
    }

    protected override void OnPropertyChanged(DependencyProperty dp)
    {
        base.OnPropertyChanged(dp);

        if (dp == TextProperty && !_isUserInput)
        {
            // Move cursor to end when text is set programmatically
            var text = Text ?? "";
            _cursorPos = text.Length;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // Default width
        return new Size(Width > 0 ? Width : 20, 1);
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;
        int w = RenderSize.Width;

        var fg = IsFocused ? ConsoleColor.Yellow : ConsoleColor.White;
        // Default to provided Background, or transparent (existing buffer background) if null
        // However, existing behavior was hardcoded Black when not focused.
        // We want to support transparency if Background is null.
        var effectiveBg = IsFocused ? ConsoleColor.DarkBlue : (Background ?? buffer.GetPixel(x, y).Background);
        var bg = effectiveBg;

        string text = Text ?? "";
        string display = IsPassword ? new string('*', text.Length) : text;

        // Simple scrolling if text is longer than width
        int start = 0;
        if (display.Length > w)
        {
            // if focused, ensure cursor is visible
            // _cursorPos is absolute index in text
            if (_cursorPos >= w)
                start = _cursorPos - w + 1;
        }

        // Draw text area
        for (int i = 0; i < w; i++)
        {
            char c = ' ';
            int textIdx = start + i;
            if (textIdx < display.Length) c = display[textIdx];

            // Cursor
            var cellBg = bg;
            var cellFg = fg;

            if (IsFocused && textIdx == _cursorPos)
            {
                cellBg = ConsoleColor.Gray;
                cellFg = ConsoleColor.Black;
            }

            buffer.SetPixel(x + i, y, c, cellFg, cellBg);
        }

        // Draw cursor at end if needed
        if (IsFocused && _cursorPos == display.Length && display.Length - start < w)
        {
            buffer.SetPixel(x + (display.Length - start), y, ' ', ConsoleColor.Black, ConsoleColor.Gray);
        }
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        // Move cursor to click position?
        // int relativeX = e.X - RenderSize.X;
        // _cursorPos = Math.Min(Text.Length, relativeX);
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
