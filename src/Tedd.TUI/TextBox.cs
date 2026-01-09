using System;

namespace Tedd.TUI;

public class TextBox : UIElement
{
    private int _cursorPos = 0;

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
        var bg = IsFocused ? ConsoleColor.DarkBlue : ConsoleColor.Black;

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

    public override void OnGotFocus()
    {
        base.OnGotFocus();
        _cursorPos = Text?.Length ?? 0;
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        // MouseEventArgs has local coordinates
        int clickIndex = e.X;
        if (clickIndex < 0) clickIndex = 0;

        string text = Text ?? "";
        if (clickIndex > text.Length) clickIndex = text.Length;

        _cursorPos = clickIndex;
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
                Text = text.Remove(_cursorPos - 1, 1);
                _cursorPos--;
            }
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.Delete)
        {
             if (_cursorPos < text.Length)
             {
                 Text = text.Remove(_cursorPos, 1);
             }
             e.Handled = true;
        }
        else if (!char.IsControl(e.KeyChar))
        {
            Text = text.Insert(_cursorPos, e.KeyChar.ToString());
            _cursorPos++;
            e.Handled = true;
        }
    }
}
