using System;
using System.Text;

namespace Tedd.TUI;

public class TextBox : UIElement
{
    public TextBox()
    {
        Focusable = true;
    }
    private int _cursorPos = 0;
    private bool _isUserInput = false;

    // Selection anchor: the fixed end of the selection; the caret is the moving end.
    // -1 means no selection. When >= 0 the selection spans [min(anchor, caret), max).
    private int _selectionAnchor = -1;
    private bool _isMouseSelecting = false;

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register("Text", typeof(string), typeof(TextBox), string.Empty);

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly DependencyProperty FocusedForegroundProperty =
        DependencyProperty.Register("FocusedForeground", typeof(TuiColor), typeof(TextBox), TuiColor.Yellow);

    /// <summary>Text color while the text box has keyboard focus.</summary>
    public TuiColor FocusedForeground
    {
        get => (TuiColor)GetValue(FocusedForegroundProperty);
        set => SetValue(FocusedForegroundProperty, value);
    }

    public static readonly DependencyProperty FocusedBackgroundProperty =
        DependencyProperty.Register("FocusedBackground", typeof(TuiColor), typeof(TextBox), TuiColor.DarkBlue);

    /// <summary>Background color while the text box has keyboard focus.</summary>
    public TuiColor FocusedBackground
    {
        get => (TuiColor)GetValue(FocusedBackgroundProperty);
        set => SetValue(FocusedBackgroundProperty, value);
    }

    public static readonly DependencyProperty SelectionForegroundProperty =
        DependencyProperty.Register("SelectionForeground", typeof(TuiColor), typeof(TextBox), TuiColor.Black);

    /// <summary>Text color of the selected range.</summary>
    public TuiColor SelectionForeground
    {
        get => (TuiColor)GetValue(SelectionForegroundProperty);
        set => SetValue(SelectionForegroundProperty, value);
    }

    public static readonly DependencyProperty SelectionBackgroundProperty =
        DependencyProperty.Register("SelectionBackground", typeof(TuiColor), typeof(TextBox), TuiColor.Cyan);

    /// <summary>Background color of the selected range.</summary>
    public TuiColor SelectionBackground
    {
        get => (TuiColor)GetValue(SelectionBackgroundProperty);
        set => SetValue(SelectionBackgroundProperty, value);
    }

    public static readonly DependencyProperty CaretForegroundProperty =
        DependencyProperty.Register("CaretForeground", typeof(TuiColor), typeof(TextBox), TuiColor.Black);

    /// <summary>Foreground of the cell under the caret.</summary>
    public TuiColor CaretForeground
    {
        get => (TuiColor)GetValue(CaretForegroundProperty);
        set => SetValue(CaretForegroundProperty, value);
    }

    public static readonly DependencyProperty CaretBackgroundProperty =
        DependencyProperty.Register("CaretBackground", typeof(TuiColor), typeof(TextBox), TuiColor.Gray);

    /// <summary>Background of the cell under the caret.</summary>
    public TuiColor CaretBackground
    {
        get => (TuiColor)GetValue(CaretBackgroundProperty);
        set => SetValue(CaretBackgroundProperty, value);
    }

    public static readonly DependencyProperty IsPasswordProperty =
        DependencyProperty.Register("IsPassword", typeof(bool), typeof(TextBox), false);

    public bool IsPassword
    {
        get => (bool)GetValue(IsPasswordProperty);
        set => SetValue(IsPasswordProperty, value);
    }

    public static readonly DependencyProperty PasswordCharProperty =
        DependencyProperty.Register("PasswordChar", typeof(char), typeof(TextBox), '*');

    public char PasswordChar
    {
        get => (char)GetValue(PasswordCharProperty);
        set => SetValue(PasswordCharProperty, value);
    }

    public int CaretIndex
    {
        get => _cursorPos;
        set
        {
            _cursorPos = Math.Clamp(value, 0, (Text ?? "").Length);
            _selectionAnchor = -1;
            Invalidate();
        }
    }

    public bool HasSelection => _selectionAnchor >= 0 && _selectionAnchor != _cursorPos;

    public int SelectionStart => HasSelection ? Math.Min(_selectionAnchor, _cursorPos) : _cursorPos;

    public int SelectionLength => HasSelection ? Math.Abs(_cursorPos - _selectionAnchor) : 0;

    public string SelectedText
    {
        get
        {
            if (!HasSelection) return string.Empty;
            return (Text ?? "").Substring(SelectionStart, SelectionLength);
        }
    }

    public void Select(int start, int length)
    {
        string text = Text ?? "";
        start = Math.Clamp(start, 0, text.Length);
        length = Math.Clamp(length, 0, text.Length - start);
        _selectionAnchor = length == 0 ? -1 : start;
        _cursorPos = start + length;
        Invalidate();
    }

    public void SelectAll() => Select(0, (Text ?? "").Length);

    public void ClearSelection()
    {
        _selectionAnchor = -1;
        Invalidate();
    }

    /// <summary>Copies the selection to <see cref="Clipboard"/>. No-op for password boxes.</summary>
    public void Copy()
    {
        if (IsPassword) return;
        var selected = SelectedText;
        if (selected.Length == 0) return;
        Clipboard.SetText(selected);
    }

    /// <summary>Copies the selection and removes it from the text. No-op for password boxes.</summary>
    public void Cut()
    {
        if (IsPassword) return;
        var selected = SelectedText;
        if (selected.Length == 0) return;
        Clipboard.SetText(selected);
        DeleteSelection();
    }

    /// <summary>Inserts the clipboard text at the caret, replacing any selection.</summary>
    public void Paste()
    {
        var paste = SanitizeForSingleLine(Clipboard.GetText());
        if (paste.Length == 0) return;

        DeleteSelection();
        string text = Text ?? "";
        _isUserInput = true;
        Text = text.Insert(_cursorPos, paste);
        _isUserInput = false;
        _cursorPos += paste.Length;
        Invalidate();
    }

    private bool DeleteSelection()
    {
        if (!HasSelection) return false;
        int start = SelectionStart;
        int length = SelectionLength;
        _isUserInput = true;
        Text = (Text ?? "").Remove(start, length);
        _isUserInput = false;
        _cursorPos = start;
        _selectionAnchor = -1;
        Invalidate();
        return true;
    }

    // A single-line control cannot represent line breaks or other control characters;
    // pasted newlines/tabs become spaces so multi-line clipboard content stays readable.
    private static string SanitizeForSingleLine(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        var sb = new StringBuilder(value.Length);
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (c == '\r')
            {
                if (i + 1 < value.Length && value[i + 1] == '\n') i++;
                sb.Append(' ');
            }
            else if (c == '\n' || c == '\t')
            {
                sb.Append(' ');
            }
            else if (!char.IsControl(c))
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    protected override void OnPropertyChanged(DependencyProperty dp)
    {
        base.OnPropertyChanged(dp);

        if (dp == TextProperty && !_isUserInput)
        {
            // Move cursor to end when text is set programmatically
            var text = Text ?? "";
            _cursorPos = text.Length;
            _selectionAnchor = -1;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // Default width
        return new Size(Width > 0 ? Width : 20, 1);
    }

    // First visible character index given the current caret; must match Render's
    // horizontal scrolling so mouse coordinates map to the characters the user sees.
    private int GetScrollStart(int w)
    {
        if (w <= 0) return 0;
        return _cursorPos >= w ? _cursorPos - w + 1 : 0;
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;
        int w = RenderSize.Width;

        var fg = IsFocused ? FocusedForeground : Foreground;
        // Unfocused with no Background set: keep transparent behavior by adopting the
        // existing buffer background under the control.
        var bg = IsFocused ? FocusedBackground : (Background ?? buffer.GetPixel(x, y).Background);

        string text = Text ?? "";
        string display = IsPassword ? new string(PasswordChar, text.Length) : text;

        // Simple scrolling if cursor would be past the visible width.
        // Apply even when text exactly fills the width so the trailing caret stays visible.
        int start = GetScrollStart(w);

        int selStart = -1, selEnd = -1;
        if (IsFocused && HasSelection)
        {
            selStart = SelectionStart;
            selEnd = selStart + SelectionLength; // exclusive
        }

        // Draw text area
        for (int i = 0; i < w; i++)
        {
            char c = ' ';
            int textIdx = start + i;
            if (textIdx < display.Length) c = display[textIdx];

            var cellBg = bg;
            var cellFg = fg;

            // Selection highlight
            if (textIdx >= selStart && textIdx < selEnd)
            {
                cellBg = SelectionBackground;
                cellFg = SelectionForeground;
            }

            // Cursor (takes priority over selection)
            if (IsFocused && textIdx == _cursorPos)
            {
                cellBg = CaretBackground;
                cellFg = CaretForeground;
            }

            buffer.SetPixel(x + i, y, c, cellFg, cellBg);
        }

        // Draw cursor at end if needed
        if (IsFocused && _cursorPos == display.Length && display.Length - start < w)
        {
            buffer.SetPixel(x + (display.Length - start), y, ' ', CaretForeground, CaretBackground);
        }
    }

    // Maps a mouse X coordinate (local to this control) to a text index, accounting
    // for horizontal scrolling. Coordinates outside the control (possible under mouse
    // capture) clamp to the nearest end.
    private int TextIndexFromMouse(int localX)
    {
        string text = Text ?? "";
        int target = GetScrollStart(RenderSize.Width) + localX;
        return Math.Clamp(target, 0, text.Length);
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();

        // Move caret to click position and anchor a new (empty) selection there;
        // dragging extends it. e.X is already local to this control.
        int target = TextIndexFromMouse(e.X);
        _cursorPos = target;
        _selectionAnchor = target;
        _isMouseSelecting = true;

        // Capture the mouse so the drag keeps selecting even when the pointer leaves
        // the control's bounds (same pattern as Thumb).
        (GetRoot() as TuiWindow)?.CaptureMouse(this);

        Invalidate();
        e.Handled = true;
    }

    public override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_isMouseSelecting) return;

        // With a window attached, only track while we hold the capture.
        if (GetRoot() is TuiWindow root && root.CapturedElement != this) return;

        int target = TextIndexFromMouse(e.X);
        if (target != _cursorPos)
        {
            _cursorPos = target;
            Invalidate();
        }
        e.Handled = true;
    }

    public override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (!_isMouseSelecting) return;

        _isMouseSelecting = false;
        if (GetRoot() is TuiWindow root && root.CapturedElement == this)
        {
            root.ReleaseMouseCapture();
        }

        // A click without a drag leaves no selection behind.
        if (_selectionAnchor == _cursorPos) _selectionAnchor = -1;

        Invalidate();
        e.Handled = true;
    }

    public override void OnLostFocus()
    {
        base.OnLostFocus();
        if (_isMouseSelecting)
        {
            _isMouseSelecting = false;
            if (GetRoot() is TuiWindow root && root.CapturedElement == this)
            {
                root.ReleaseMouseCapture();
            }
        }
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        string text = Text ?? "";
        bool ctrl = (e.Modifiers & ConsoleModifiers.Control) != 0;
        bool shift = (e.Modifiers & ConsoleModifiers.Shift) != 0;

        if (ctrl && e.Key == ConsoleKey.C || ctrl && e.Key == ConsoleKey.Insert)
        {
            Copy();
            e.Handled = true;
        }
        else if (ctrl && e.Key == ConsoleKey.X || shift && !ctrl && e.Key == ConsoleKey.Delete)
        {
            Cut();
            e.Handled = true;
        }
        else if (ctrl && e.Key == ConsoleKey.V || shift && !ctrl && e.Key == ConsoleKey.Insert)
        {
            Paste();
            e.Handled = true;
        }
        else if (ctrl && e.Key == ConsoleKey.A)
        {
            SelectAll();
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.LeftArrow)
        {
            if (shift)
            {
                if (_selectionAnchor < 0) _selectionAnchor = _cursorPos;
                if (_cursorPos > 0) _cursorPos--;
            }
            else if (HasSelection)
            {
                _cursorPos = SelectionStart;
                _selectionAnchor = -1;
            }
            else
            {
                if (_cursorPos > 0) _cursorPos--;
                _selectionAnchor = -1;
            }
            Invalidate();
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.RightArrow)
        {
            if (shift)
            {
                if (_selectionAnchor < 0) _selectionAnchor = _cursorPos;
                if (_cursorPos < text.Length) _cursorPos++;
            }
            else if (HasSelection)
            {
                _cursorPos = SelectionStart + SelectionLength;
                _selectionAnchor = -1;
            }
            else
            {
                if (_cursorPos < text.Length) _cursorPos++;
                _selectionAnchor = -1;
            }
            Invalidate();
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.Backspace)
        {
            if (!DeleteSelection() && _cursorPos > 0 && text.Length > 0)
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
            if (!DeleteSelection() && _cursorPos < text.Length)
            {
                _isUserInput = true;
                Text = text.Remove(_cursorPos, 1);
                _isUserInput = false;
            }
            e.Handled = true;
        }
        else if (!ctrl && !char.IsControl(e.KeyChar))
        {
            DeleteSelection();
            text = Text ?? "";
            _isUserInput = true;
            Text = text.Insert(_cursorPos, e.KeyChar.ToString());
            _isUserInput = false;
            _cursorPos++;
            e.Handled = true;
        }
    }
}
