using System;
using System.Text;
using Tedd.TUI;

namespace Tedd.TUI.Markdown;

public class MarkdownView : UIElement
{
    private FlowDocument _document;
    private MarkdownParser _parser;
    private string? _baseDirectory;

    // --- Text selection state (content-local cell coordinates) ---
    // Anchor is where the drag began; caret is the current end. Both are (col, row)
    // relative to the document's top-left, which coincides with this view's local origin.
    private bool _isSelecting;
    private bool _hasSelection;
    private int _anchorCol, _anchorRow;
    private int _caretCol, _caretRow;

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register("Text", typeof(string), typeof(MarkdownView), string.Empty);

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public MarkdownTheme Theme
    {
        get => field ??= new MarkdownTheme();
        set
        {
            field = value;
            Refresh();
        }
    }

    /// <summary>
    /// Directory used to resolve relative image sources (e.g. <c>![alt](photo.png)</c>).
    /// Typically set to the directory of the markdown document. Forwarded to every
    /// <see cref="Image"/> created from this view.
    /// </summary>
    public string? BaseDirectory
    {
        get => _baseDirectory;
        set
        {
            if (_baseDirectory != value)
            {
                _baseDirectory = value;
                Refresh();
            }
        }
    }

    public MarkdownView()
    {
        // Focusable so the view can receive Ctrl+C to copy the current selection.
        Focusable = true;
        _document = new FlowDocument();
        _document.Parent = this;
    }

    protected override void OnPropertyChanged(DependencyProperty dp)
    {
        base.OnPropertyChanged(dp);
        if (dp == TextProperty)
        {
            Refresh();
        }
    }

    public void Refresh()
    {
        // A re-parse replaces the visual tree the selection was measured against.
        ClearSelection();

        if (string.IsNullOrEmpty(Text))
        {
            // Reset to empty document
            _document = new FlowDocument();
            _document.Parent = this;
            Invalidate();
            return;
        }

        // Create parser with current theme and base directory.
        _parser = new MarkdownParser(Theme) { BaseDirectory = _baseDirectory };

        // Parse and populate
        var doc = _parser.Parse(Text);

        // Replace document
        _document = doc;
        _document.Parent = this;

        Invalidate();
    }

    public override int VisualChildrenCount => _document != null ? 1 : 0;

    public override UIElement GetVisualChild(int index)
    {
        if (_document != null && index == 0) return _document;
        throw new ArgumentOutOfRangeException(nameof(index));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (_document == null) return new Size(0, 0);
        _document.Measure(availableSize);
        return _document.DesiredSize;
    }

    protected override void ArrangeOverride(Size finalSize)
    {
        if (_document != null)
        {
            _document.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
        }
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        if (_document == null) return;

        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;
        _document.Render(buffer, x, y);

        if (_hasSelection)
        {
            RenderSelectionHighlight(buffer, x, y);
        }
    }

    // === Text selection =====================================================

    public override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();

        // e.X/e.Y are local to this view, which is also the document's coordinate space.
        _anchorCol = _caretCol = Math.Max(0, e.X);
        _anchorRow = _caretRow = Math.Max(0, e.Y);
        _isSelecting = true;
        _hasSelection = false;

        (GetRoot() as TuiWindow)?.CaptureMouse(this);
        Invalidate();
        e.Handled = true;
    }

    public override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!_isSelecting) return;
        if (GetRoot() is TuiWindow root && root.CapturedElement != this) return;

        int col = Math.Max(0, e.X);
        int row = Math.Max(0, e.Y);
        if (col != _caretCol || row != _caretRow)
        {
            _caretCol = col;
            _caretRow = row;
            // Any drag away from the anchor cell constitutes a selection.
            _hasSelection = _caretCol != _anchorCol || _caretRow != _anchorRow;
            Invalidate();
        }
        e.Handled = true;
    }

    public override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (!_isSelecting) return;

        _isSelecting = false;
        if (GetRoot() is TuiWindow root && root.CapturedElement == this)
        {
            root.ReleaseMouseCapture();
        }

        // A click without a drag leaves nothing selected.
        _hasSelection = _caretCol != _anchorCol || _caretRow != _anchorRow;
        Invalidate();
        e.Handled = true;
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == ConsoleKey.C && (e.Modifiers & ConsoleModifiers.Control) != 0)
        {
            CopySelection();
            e.Handled = true;
        }
    }

    /// <summary>Clears the active selection (if any).</summary>
    public void ClearSelection()
    {
        if (!_hasSelection && !_isSelecting) return;
        _hasSelection = false;
        _isSelecting = false;
        Invalidate();
    }

    /// <summary>The currently selected text, or the empty string when nothing is selected.</summary>
    public string SelectedText => _hasSelection ? BuildSelectedText() : string.Empty;

    /// <summary>Copies the current selection to the <see cref="Clipboard"/>.</summary>
    public void CopySelection()
    {
        if (!_hasSelection) return;
        string text = BuildSelectedText();
        if (!string.IsNullOrEmpty(text))
        {
            Clipboard.SetText(text);
        }
    }

    // Normalizes anchor/caret into (top-left .. bottom-right) reading order and returns
    // an inclusive cell range: rows [r0..r1], with columns [c0 (on r0) .. c1 (on r1)].
    private void GetNormalizedSelection(out int r0, out int c0, out int r1, out int c1)
    {
        if (_anchorRow < _caretRow || (_anchorRow == _caretRow && _anchorCol <= _caretCol))
        {
            r0 = _anchorRow; c0 = _anchorCol; r1 = _caretRow; c1 = _caretCol;
        }
        else
        {
            r0 = _caretRow; c0 = _caretCol; r1 = _anchorRow; c1 = _anchorCol;
        }
    }

    private string BuildSelectedText()
    {
        var grid = BuildTextGrid(out int width, out int height);
        if (grid == null || height == 0) return string.Empty;

        GetNormalizedSelection(out int r0, out int c0, out int r1, out int c1);
        r0 = Math.Clamp(r0, 0, height - 1);
        r1 = Math.Clamp(r1, 0, height - 1);

        var sb = new StringBuilder();
        for (int row = r0; row <= r1; row++)
        {
            int startCol = (row == r0) ? c0 : 0;
            int endCol = (row == r1) ? c1 + 1 : width; // +1: end cell is inclusive
            startCol = Math.Clamp(startCol, 0, width);
            endCol = Math.Clamp(endCol, 0, width);

            var line = grid[row];
            var lineSb = new StringBuilder();
            for (int col = startCol; col < endCol; col++)
            {
                lineSb.Append(line[col] == '\0' ? ' ' : line[col]);
            }

            // Trim trailing padding so copied lines don't carry blank runs.
            int len = lineSb.Length;
            while (len > 0 && lineSb[len - 1] == ' ') len--;
            lineSb.Length = len;

            if (row != r0) sb.Append('\n');
            sb.Append(lineSb);
        }
        return sb.ToString();
    }

    private void RenderSelectionHighlight(VirtualBuffer buffer, int originX, int originY)
    {
        var grid = BuildTextGrid(out int width, out int height);
        if (grid == null || height == 0) return;

        GetNormalizedSelection(out int r0, out int c0, out int r1, out int c1);
        r0 = Math.Clamp(r0, 0, height - 1);
        r1 = Math.Clamp(r1, 0, height - 1);

        TuiColor selBg = Theme.SelectionBackground ?? TuiColor.DarkCyan;
        TuiColor? selFg = Theme.SelectionForeground;

        for (int row = r0; row <= r1; row++)
        {
            int startCol = (row == r0) ? c0 : 0;
            int endCol = (row == r1) ? c1 + 1 : width;

            // Only paint over actual content, not the blank tail of the line.
            int contentLen = ContentLength(grid[row]);
            startCol = Math.Clamp(startCol, 0, width);
            endCol = Math.Clamp(Math.Min(endCol, contentLen), 0, width);

            for (int col = startCol; col < endCol; col++)
            {
                int sx = originX + col;
                int sy = originY + row;
                var cell = buffer.GetPixel(sx, sy);
                buffer.SetPixel(sx, sy, cell.Character, selFg ?? cell.Foreground, selBg);
            }
        }
    }

    private static int ContentLength(char[] row)
    {
        int len = row.Length;
        while (len > 0 && (row[len - 1] == ' ' || row[len - 1] == '\0')) len--;
        return len;
    }

    /// <summary>
    /// Snapshots the rendered text of the document into a row-major grid of glyphs in
    /// content-local coordinates. Code blocks are skipped -- they carry their own copy
    /// button and scroll independently, so they are not part of the flowing selection.
    /// </summary>
    private char[][]? BuildTextGrid(out int width, out int height)
    {
        width = Math.Max(RenderSize.Width, _document?.RenderSize.Width ?? 0);
        height = _document?.RenderSize.Height ?? 0;
        if (_document == null || width <= 0 || height <= 0) return null;

        var grid = new char[height][];
        for (int i = 0; i < height; i++)
        {
            grid[i] = new char[width];
            Array.Fill(grid[i], ' ');
        }

        CollectText(_document, 0, 0, grid, width, height);
        return grid;
    }

    private static void CollectText(UIElement el, int ox, int oy, char[][] grid, int width, int height)
    {
        if (el == null) return;
        // Code blocks are excluded from prose selection.
        if (el is MarkdownCodeBlock) return;

        int x = ox + el.RenderSize.X;
        int y = oy + el.RenderSize.Y;

        string? text = el switch
        {
            TextBlock tb => tb.Text,
            Hyperlink hl => hl.Text,
            _ => null
        };

        if (!string.IsNullOrEmpty(text))
        {
            if (y >= 0 && y < height)
            {
                var rowChars = grid[y];
                for (int i = 0; i < text.Length; i++)
                {
                    int col = x + i;
                    if (col >= 0 && col < width) rowChars[col] = text[i];
                }
            }
            return;
        }

        int count = el.VisualChildrenCount;
        for (int i = 0; i < count; i++)
        {
            CollectText(el.GetVisualChild(i), x, y, grid, width, height);
        }
    }
}
