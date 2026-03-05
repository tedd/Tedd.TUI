using System;
using System.Collections.Generic;

namespace Tedd.TUI;

public class TextEditor : UIElement
{
    public TextEditor()
    {
        Focusable = true;
    }

    private int _cursorCol = 0;
    private int _cursorRow = 0;
    private int _scrollX = 0;
    private int _scrollY = 0;
    private bool _isUserInput = false;
    private List<string> _lines = new List<string> { "" };

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register("Text", typeof(string), typeof(TextEditor), string.Empty);

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    protected override void OnPropertyChanged(DependencyProperty dp)
    {
        base.OnPropertyChanged(dp);

        if (dp == TextProperty && !_isUserInput)
        {
            var text = Text ?? "";

            // Optimization: Replace string.Split with span-based line enumeration to eliminate array allocations O(1) allocation instead of O(n)
            _lines = new List<string>();
            var span = text.AsSpan();
            foreach (var line in span.EnumerateLines())
            {
                _lines.Add(line.ToString());
            }

            // EnumerateLines swallows trailing newlines, so we must add an empty line explicitly
            // if the text ends with \n or \r\n to match string.Split(..., StringSplitOptions.None)
            if (span.Length > 0 && (span[^1] == '\n' || span[^1] == '\r'))
            {
                _lines.Add("");
            }

            if (_lines.Count == 0) _lines.Add("");

            _cursorRow = _lines.Count - 1;
            _cursorCol = _lines[_cursorRow].Length;
            AdjustScroll();
        }
    }

    private void UpdateTextFromLines()
    {
        _isUserInput = true;
        Text = string.Join(Environment.NewLine, _lines);
        _isUserInput = false;
        Invalidate();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(Width > 0 ? Width : 40, Height > 0 ? Height : 10);
    }

    protected override void ArrangeOverride(Size finalSize)
    {
        base.ArrangeOverride(finalSize);
        AdjustScroll();
    }

    private void AdjustScroll()
    {
        int w = RenderSize.Width;
        int h = RenderSize.Height;

        if (w <= 0 || h <= 0) return;

        if (_cursorRow < _scrollY)
        {
            _scrollY = _cursorRow;
        }
        else if (_cursorRow >= _scrollY + h)
        {
            _scrollY = _cursorRow - h + 1;
        }

        if (_cursorCol < _scrollX)
        {
            _scrollX = _cursorCol;
        }
        else if (_cursorCol >= _scrollX + w)
        {
            _scrollX = _cursorCol - w + 1;
        }
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;
        int w = RenderSize.Width;
        int h = RenderSize.Height;

        if (w <= 0 || h <= 0) return;

        var fg = IsFocused ? ConsoleColor.Yellow : ConsoleColor.White;
        var bg = IsFocused ? ConsoleColor.DarkBlue : (Background ?? buffer.GetPixel(x, y).Background);

        for (int row = 0; row < h; row++)
        {
            int lineIdx = _scrollY + row;
            string lineText = lineIdx < _lines.Count ? _lines[lineIdx] : "";

            for (int col = 0; col < w; col++)
            {
                int charIdx = _scrollX + col;
                char c = ' ';
                if (charIdx < lineText.Length) c = lineText[charIdx];

                var cellFg = fg;
                var cellBg = bg;

                if (IsFocused && lineIdx == _cursorRow && charIdx == _cursorCol)
                {
                    cellFg = ConsoleColor.Black;
                    cellBg = ConsoleColor.Gray;
                }

                buffer.SetPixel(x + col, y + row, c, cellFg, cellBg);
            }

            if (IsFocused && lineIdx == _cursorRow && _cursorCol == lineText.Length && _cursorCol - _scrollX < w)
            {
                buffer.SetPixel(x + (_cursorCol - _scrollX), y + row, ' ', ConsoleColor.Black, ConsoleColor.Gray);
            }
        }
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();

        int localX = e.X;
        int localY = e.Y;

        int targetRow = _scrollY + localY;
        if (targetRow < 0) targetRow = 0;
        if (targetRow >= _lines.Count) targetRow = _lines.Count - 1;

        _cursorRow = targetRow;

        int targetCol = _scrollX + localX;
        if (targetCol < 0) targetCol = 0;
        if (targetCol > _lines[_cursorRow].Length) targetCol = _lines[_cursorRow].Length;

        _cursorCol = targetCol;

        AdjustScroll();
        Invalidate();
        e.Handled = true;
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == ConsoleKey.LeftArrow)
        {
            if (_cursorCol > 0)
            {
                _cursorCol--;
            }
            else if (_cursorRow > 0)
            {
                _cursorRow--;
                _cursorCol = _lines[_cursorRow].Length;
            }
            AdjustScroll();
            Invalidate();
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.RightArrow)
        {
            if (_cursorCol < _lines[_cursorRow].Length)
            {
                _cursorCol++;
            }
            else if (_cursorRow < _lines.Count - 1)
            {
                _cursorRow++;
                _cursorCol = 0;
            }
            AdjustScroll();
            Invalidate();
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.UpArrow)
        {
            if (_cursorRow > 0)
            {
                _cursorRow--;
                if (_cursorCol > _lines[_cursorRow].Length)
                {
                    _cursorCol = _lines[_cursorRow].Length;
                }
            }
            AdjustScroll();
            Invalidate();
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.DownArrow)
        {
            if (_cursorRow < _lines.Count - 1)
            {
                _cursorRow++;
                if (_cursorCol > _lines[_cursorRow].Length)
                {
                    _cursorCol = _lines[_cursorRow].Length;
                }
            }
            AdjustScroll();
            Invalidate();
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.Home)
        {
            _cursorCol = 0;
            AdjustScroll();
            Invalidate();
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.End)
        {
            _cursorCol = _lines[_cursorRow].Length;
            AdjustScroll();
            Invalidate();
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.Enter)
        {
            string currentLine = _lines[_cursorRow];
            string remain = currentLine.Substring(_cursorCol);
            _lines[_cursorRow] = currentLine.Substring(0, _cursorCol);
            _lines.Insert(_cursorRow + 1, remain);
            _cursorRow++;
            _cursorCol = 0;
            UpdateTextFromLines();
            AdjustScroll();
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.Backspace)
        {
            if (_cursorCol > 0)
            {
                _lines[_cursorRow] = _lines[_cursorRow].Remove(_cursorCol - 1, 1);
                _cursorCol--;
                UpdateTextFromLines();
            }
            else if (_cursorRow > 0)
            {
                string currentLine = _lines[_cursorRow];
                _lines.RemoveAt(_cursorRow);
                _cursorRow--;
                _cursorCol = _lines[_cursorRow].Length;
                _lines[_cursorRow] += currentLine;
                UpdateTextFromLines();
            }
            AdjustScroll();
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.Delete)
        {
            if (_cursorCol < _lines[_cursorRow].Length)
            {
                _lines[_cursorRow] = _lines[_cursorRow].Remove(_cursorCol, 1);
                UpdateTextFromLines();
            }
            else if (_cursorRow < _lines.Count - 1)
            {
                string nextLine = _lines[_cursorRow + 1];
                _lines.RemoveAt(_cursorRow + 1);
                _lines[_cursorRow] += nextLine;
                UpdateTextFromLines();
            }
            AdjustScroll();
            e.Handled = true;
        }
        else if (!char.IsControl(e.KeyChar))
        {
            _lines[_cursorRow] = _lines[_cursorRow].Insert(_cursorCol, e.KeyChar.ToString());
            _cursorCol++;
            UpdateTextFromLines();
            AdjustScroll();
            e.Handled = true;
        }
    }
}
