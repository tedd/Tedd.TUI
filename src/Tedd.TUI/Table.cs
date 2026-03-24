using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Tedd.TUI;

public class TableColumn
{
    public string? Header { get; set; }
    public GridLength Width { get; set; } = GridLength.Star;

    // Sorting
    public Func<object, object, int>? SortComparer { get; set; }
    public Func<TableRow, object>? SortKeySelector { get; set; }

    // Internal usage for layout
    internal int ActualWidth { get; set; }
}

public class TableRow : UIElement
{
    private List<UIElement> _cells = new List<UIElement>();
    public IList<UIElement> Cells => _cells;
    public object? Tag { get; set; } // For data binding or identification

    public void AddCell(UIElement cell)
    {
        _cells.Add(cell);
        cell.Parent = this;
    }

    public void AddCell(string text)
    {
        AddCell(new TextBlock { Text = text, Foreground = ConsoleColor.White });
    }

    public override int VisualChildrenCount => _cells.Count;

    public override UIElement GetVisualChild(int index)
    {
        if (index < 0 || index >= _cells.Count) throw new ArgumentOutOfRangeException(nameof(index));
        return _cells[index];
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        int maxHeight = 0;
        foreach (var cell in _cells)
        {
            cell.Measure(new Size(int.MaxValue, availableSize.Height));
            maxHeight = Math.Max(maxHeight, cell.DesiredSize.Height);
        }

        int totalWidth = 0;
        var table = FindAncestor<Table>();
        if (table != null && table.Columns.Count > 0)
        {
             foreach (var col in table.Columns) totalWidth += col.ActualWidth;
             if (table.ShowVerticalLines) totalWidth += table.Columns.Count - 1;
        }

        return new Size(totalWidth, maxHeight > 0 ? maxHeight : 1);
    }

    protected override void ArrangeOverride(Size finalSize)
    {
        var table = FindAncestor<Table>();
        if (table != null)
        {
            int x = 0;
            for (int i = 0; i < _cells.Count && i < table.Columns.Count; i++)
            {
                int w = table.Columns[i].ActualWidth;
                var cell = _cells[i];
                cell.Arrange(new Rect(x, 0, w, finalSize.Height));
                x += w;
                // Add spacing
                x += (i < table.Columns.Count - 1) ? 1 : 0;
            }
        }
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;

        // Render cells
        foreach (var cell in _cells)
        {
            cell.Render(buffer, x, y);
        }

        // Render vertical lines between cells
        var table = FindAncestor<Table>();
        if (table != null && table.ShowVerticalLines)
        {
            int cx = 0;
            // Use Light Vertical Line usually
            char vChar = '\u2502';

            for (int i = 0; i < table.Columns.Count - 1; i++)
            {
                cx += table.Columns[i].ActualWidth;
                buffer.DrawVLine(x + cx, y, RenderSize.Height, vChar, ConsoleColor.Gray, ConsoleColor.Black);
                cx++;
            }
        }
    }
}

public class Table : UIElement
{
    public List<TableColumn> Columns { get; } = new List<TableColumn>();

    private ObservableCollection<TableRow> _rows;
    public IList<TableRow> Rows => _rows;

    // Track if visible rows need to be rebuilt.
    // We assume rows are dirty initially.
    private bool _rowsDirty = true;

    private readonly ScrollViewer _scrollViewer;
    private readonly StackPanel _rowStack;

    public bool ShowHeader { get; set; } = true;
    public ConsoleColor HeaderForeground { get; set; } = ConsoleColor.Yellow;
    public ConsoleColor HeaderBackground { get; set; } = ConsoleColor.DarkGray;

    // Style Properties
    public bool ShowBorder { get; set; } = false;
    public bool ShowVerticalLines { get; set; } = true;

    public bool ShowHorizontalLines
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                _rowsDirty = true;
                Invalidate();
            }
        }
    } = false;

    public BoxStyle BorderStyle { get; set; } = BoxStyle.Heavy; // Default to Heavy per user request

    // Selection
    public int SelectedIndex
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                SelectionChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }
    } = -1;
    public event EventHandler SelectionChanged;

    public Table()
    {
        _rows = new ObservableCollection<TableRow>();
        _rows.CollectionChanged += OnRowsCollectionChanged;

        Focusable = true;
        _rowStack = new StackPanel { Orientation = Orientation.Vertical };
        _scrollViewer = new ScrollViewer
        {
            Content = _rowStack,
            VerticalScrollBarVisibility = true,
            HorizontalScrollBarVisibility = true
        };
        _scrollViewer.Parent = this;
    }

    private void OnRowsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _rowsDirty = true;
        Invalidate();
    }

    public void AddRow(TableRow row)
    {
        _rows.Add(row);
    }

    public void AddRow(params ReadOnlySpan<object> values)
    {
        var row = new TableRow();
        foreach (var val in values)
        {
            if (val is UIElement uie) row.AddCell(uie);
            else row.AddCell(val?.ToString() ?? "");
        }
        AddRow(row);
    }

    public override int VisualChildrenCount => 1;
    public override UIElement GetVisualChild(int index)
    {
        if (index == 0) return _scrollViewer;
        throw new ArgumentOutOfRangeException(nameof(index));
    }

    public override UIElement FindName(string name)
    {
        if (Name == name) return this;
        // Search rows (logical children)
        foreach (var row in Rows)
        {
            var found = row.FindName(name);
            if (found != null) return found;
        }
        // Also check scrollviewer (which contains visible rows, so redundant but safe)
        // Actually, visible rows are subset of Rows, so checking Rows is sufficient.
        // But if ScrollViewer has other children? No, just stackpanel of rows.

        return null;
    }

    protected override void OnDataContextChanged(object newValue)
    {
        base.OnDataContextChanged(newValue);
        _scrollViewer.DataContext = newValue;
    }

    private void UpdateVisibleRows()
    {
        if (!_rowsDirty) return;

        _rowStack.Children.Clear();

        int startIdx = 0;
        int count = _rows.Count;

        if (IsInternalPaging)
        {
            startIdx = CurrentPage * PageSize;
            count = Math.Min(PageSize, _rows.Count - startIdx);
        }
        else if (PageSize > 0)
        {
            count = Math.Min(PageSize, _rows.Count);
        }

        for (int i = 0; i < count; i++)
        {
            int realIdx = startIdx + i;
            if (realIdx < _rows.Count)
            {
                _rowStack.AddChild(_rows[realIdx]);

                if (ShowHorizontalLines && i < count - 1)
                {
                    var sep = new TableSeparator();
                    _rowStack.AddChild(sep);
                }
            }
        }

        _rowsDirty = false;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var col in Columns) col.ActualWidth = 0;

        if (ShowHeader)
        {
            for (int i = 0; i < Columns.Count; i++)
            {
                var col = Columns[i];
                if (col.Width.GridUnitType == GridUnitType.Auto)
                {
                    col.ActualWidth = Math.Max(col.ActualWidth, col.Header?.Length ?? 0);
                }
            }
        }

        UpdateVisibleRows();

        int padding = ShowBorder ? 1 : 0;
        int availableWidthForCols = Math.Max(0, availableSize.Width - 2 * padding);
        if (_scrollViewer.VerticalScrollBarVisibility) availableWidthForCols--;

        foreach (var child in _rowStack.Children)
        {
            if (child is TableRow row)
            {
                child.Measure(new Size(availableWidthForCols, availableSize.Height));

                for (int j = 0; j < Columns.Count && j < row.Cells.Count; j++)
                {
                    var col = Columns[j];
                    if (col.Width.GridUnitType == GridUnitType.Auto)
                    {
                        col.ActualWidth = Math.Max(col.ActualWidth, row.Cells[j].DesiredSize.Width);
                    }
                }
            }
        }

        int usedWidth = 0;
        double totalStars = 0;
        foreach (var col in Columns)
        {
            if (col.Width.GridUnitType == GridUnitType.Pixel)
            {
                col.ActualWidth = (int)col.Width.Value;
            }
            usedWidth += col.ActualWidth;
            if (col.Width.GridUnitType == GridUnitType.Star)
            {
                totalStars += col.Width.Value;
            }
        }

        int separators = Math.Max(0, Columns.Count - 1);
        usedWidth += separators;

        int remainingWidth = Math.Max(0, availableWidthForCols - usedWidth);

        if (totalStars > 0 && remainingWidth > 0)
        {
            foreach (var col in Columns)
            {
                if (col.Width.GridUnitType == GridUnitType.Star)
                {
                    double share = col.Width.Value / totalStars;
                    int added = (int)(remainingWidth * share);
                    col.ActualWidth += added;
                }
            }
        }

        int headerBlockHeight = ShowHeader ? 2 : 0;
        int footerHeight = PageSize > 0 ? 1 : 0;
        int verticalPadding = ShowBorder ? 2 : 0;

        int bodyHeight = Math.Max(0, availableSize.Height - headerBlockHeight - footerHeight - verticalPadding);
        int svWidth = Math.Max(0, availableSize.Width - 2 * padding);

        _scrollViewer.Measure(new Size(svWidth, bodyHeight));

        return new Size(availableSize.Width, verticalPadding + headerBlockHeight + _scrollViewer.DesiredSize.Height + footerHeight);
    }

    protected override void ArrangeOverride(Size finalSize)
    {
        int padding = ShowBorder ? 1 : 0;
        int headerBlockHeight = ShowHeader ? 2 : 0;
        int verticalPadding = ShowBorder ? 2 : 0;
        int footerHeight = PageSize > 0 ? 1 : 0;
        int bodyHeight = Math.Max(0, finalSize.Height - headerBlockHeight - footerHeight - verticalPadding);
        int svWidth = Math.Max(0, finalSize.Width - 2 * padding);

        _scrollViewer.Arrange(new Rect(padding, padding + headerBlockHeight, svWidth, bodyHeight));
    }

    private struct TableBoxChars
    {
        public char TL, TR, BL, BR, H, V;
        public char TDown, TUp;
        public char TLeft, TRight;
        public char HeaderCross;
        public char BodySepTLeft, BodySepTRight;
        public char HeaderSepH;
        public char HeaderInnerV;

        public static TableBoxChars Get(BoxStyle style)
        {
            TableBoxChars c = new TableBoxChars();
            var b = BoxDrawingChars.Get(style);
            c.TL = b.TopLeft; c.TR = b.TopRight; c.BL = b.BottomLeft; c.BR = b.BottomRight;
            c.H = b.Horizontal; c.V = b.Vertical;
            c.HeaderInnerV = b.Vertical;
            c.HeaderSepH = b.Horizontal;

            switch (style)
            {
                case BoxStyle.Heavy:
                    c.TDown = '\u2533';
                    c.TUp = '\u2537';   // ┷ (Heavy Horz, Light Up)
                    c.TLeft = '\u2523';
                    c.TRight = '\u252B';
                    c.HeaderCross = '\u254B';
                    c.BodySepTLeft = '\u2520';
                    c.BodySepTRight = '\u2528';
                    break;
                case BoxStyle.Double:
                    c.TDown = '\u2566';
                    c.TUp = '\u2569';   // ╧ (Double Horz, Single Up)
                    c.TLeft = '\u2560';
                    c.TRight = '\u2563';
                    c.HeaderCross = '\u256C';
                    c.BodySepTLeft = '\u255F'; // ╟ (Double Vert, Single Right)
                    c.BodySepTRight = '\u2562'; // ╢ (Double Vert, Single Left)
                    break;
                default:
                    c.TDown = '\u252C';
                    c.TUp = '\u2534';
                    c.TLeft = '\u251C';
                    c.TRight = '\u2524';
                    c.HeaderCross = '\u253C';
                    c.BodySepTLeft = '\u251C';
                    c.BodySepTRight = '\u2524';
                    break;
            }
            return c;
        }
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;
        int w = RenderSize.Width;
        int h = RenderSize.Height;

        TableBoxChars chars = TableBoxChars.Get(BorderStyle);

        if (w <= 0 || h <= 0) return;

        // 1. Draw Outer Border
        if (ShowBorder)
        {
            buffer.SetPixel(x, y, chars.TL, HeaderForeground, HeaderBackground);
            if (w > 1) buffer.SetPixel(x + w - 1, y, chars.TR, HeaderForeground, HeaderBackground);
            if (h > 1) buffer.SetPixel(x, y + h - 1, chars.BL, HeaderForeground, HeaderBackground);
            if (w > 1 && h > 1) buffer.SetPixel(x + w - 1, y + h - 1, chars.BR, HeaderForeground, HeaderBackground);

            if (w > 2)
            {
                buffer.DrawHLine(x + 1, y, w - 2, chars.H, HeaderForeground, HeaderBackground);
                if (h > 1) buffer.DrawHLine(x + 1, y + h - 1, w - 2, chars.H, HeaderForeground, HeaderBackground);
            }

            if (h > 2)
            {
                buffer.DrawVLine(x, y + 1, h - 2, chars.V, HeaderForeground, HeaderBackground);
                if (w > 1) buffer.DrawVLine(x + w - 1, y + 1, h - 2, chars.V, HeaderForeground, HeaderBackground);
            }

            if (ShowVerticalLines && w > 2)
            {
                int cx = 1;
                for (int i = 0; i < Columns.Count - 1; i++)
                {
                    cx += Columns[i].ActualWidth;
                    if (cx < w - 1)
                        buffer.SetPixel(x + cx, y, chars.TDown, HeaderForeground, HeaderBackground);
                    cx++;
                }
            }
        }

        // 2. Draw Header
        if (ShowHeader)
        {
            int headerY = y + (ShowBorder ? 1 : 0);
            int startX = x + (ShowBorder ? 1 : 0);

            int colX = startX;
            for (int i = 0; i < Columns.Count; i++)
            {
                var col = Columns[i];
                int drawWidth = col.ActualWidth;
                int maxColX = x + w - (ShowBorder ? 1 : 0);
                if (colX + drawWidth > maxColX) drawWidth = maxColX - colX;

                if (drawWidth > 0)
                {
                    var span = (col.Header ?? "").AsSpan();
                    if (span.Length > drawWidth) span = span.Slice(0, drawWidth);

                    buffer.DrawHLine(colX, headerY, drawWidth, ' ', HeaderForeground, HeaderBackground);
                    buffer.DrawString(colX, headerY, span, HeaderForeground, HeaderBackground);
                }

                colX += col.ActualWidth;

                if (i < Columns.Count - 1)
                {
                    if (ShowVerticalLines)
                        buffer.SetPixel(colX, headerY, chars.HeaderInnerV, HeaderForeground, HeaderBackground);
                    colX++;
                }
            }

            // Fill remaining header background
            if (colX < x + w - (ShowBorder ? 1 : 0))
            {
                int endX = x + w - (ShowBorder ? 1 : 0);
                buffer.DrawHLine(colX, headerY, endX - colX, ' ', HeaderForeground, HeaderBackground);
            }

            int sepY = headerY + 1;

            if (ShowBorder && sepY < y + h - 1)
            {
                buffer.SetPixel(x, sepY, chars.TLeft, HeaderForeground, HeaderBackground);
                if (w > 1) buffer.SetPixel(x + w - 1, sepY, chars.TRight, HeaderForeground, HeaderBackground);
            }

            if (sepY < y + h)
            {
                int lineX = startX;
                for (int i = 0; i < Columns.Count; i++)
                {
                    int cw = Columns[i].ActualWidth;
                    int maxLineX = x + w - (ShowBorder ? 1 : 0);
                    int drawWidth = cw;
                    if (lineX + drawWidth > maxLineX) drawWidth = maxLineX - lineX;

                    if (drawWidth > 0)
                        buffer.DrawHLine(lineX, sepY, drawWidth, chars.HeaderSepH, HeaderForeground, HeaderBackground);

                    lineX += cw;

                    if (i < Columns.Count - 1 && lineX < maxLineX)
                    {
                        if (ShowVerticalLines)
                            buffer.SetPixel(lineX, sepY, chars.HeaderCross, HeaderForeground, HeaderBackground);
                        lineX++;
                    }
                }

                // Fill remaining separator line
                if (lineX < x + w - (ShowBorder ? 1 : 0))
                {
                    int endX = x + w - (ShowBorder ? 1 : 0);
                    buffer.DrawHLine(lineX, sepY, endX - lineX, chars.HeaderSepH, HeaderForeground, HeaderBackground);
                }
            }
        }

        // 3. Render Body
        _scrollViewer.Render(buffer, x, y);

        // 4. Render Border Junctions
        int vOffset = _scrollViewer.VerticalOffset;
        int bodyScreenY = y + (ShowBorder ? 1 : 0) + (ShowHeader ? 2 : 0);

        int currentY = 0;
        foreach (var child in _rowStack.Children)
        {
            int hChild = child.RenderSize.Height;
            int screenY = bodyScreenY + currentY - vOffset;

            if (child is TableSeparator && ShowHorizontalLines && ShowBorder)
            {
                if (screenY >= bodyScreenY && screenY < bodyScreenY + _scrollViewer.RenderSize.Height)
                {
                    buffer.SetPixel(x, screenY, chars.BodySepTLeft, HeaderForeground, HeaderBackground);
                    if (w > 1) buffer.SetPixel(x + w - 1, screenY, chars.BodySepTRight, HeaderForeground, HeaderBackground);
                }
            }

            currentY += hChild;
            if (currentY - vOffset > h) break;
        }

        // 5. Draw Bottom Border Junctions
        if (ShowBorder && ShowVerticalLines && h > 1 && w > 2)
        {
            int cx = 1;
            int by = y + h - 1;
            for (int i = 0; i < Columns.Count - 1; i++)
            {
                cx += Columns[i].ActualWidth;
                if (cx < w - 1)
                    buffer.SetPixel(x + cx, by, chars.TUp, HeaderForeground, HeaderBackground);
                cx++;
            }
        }

        if (PageSize > 0)
        {
            RenderPagination(buffer, offsetX, offsetY);
        }
    }

    public override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == ConsoleKey.UpArrow)
        {
            int limit = EffectiveTotalRows - 1;
            if (SelectedIndex > 0)
            {
                SelectedIndex--;
                EnsureVisible(SelectedIndex);
            }
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.DownArrow)
        {
            int limit = EffectiveTotalRows - 1;
            if (SelectedIndex < limit)
            {
                SelectedIndex++;
                EnsureVisible(SelectedIndex);
            }
            e.Handled = true;
        }
    }

    private void EnsureVisible(int index)
    {
        if (PageSize > 0)
        {
            int page = index / PageSize;
            if (page != CurrentPage)
            {
                CurrentPage = page;
            }
            Invalidate();
        }
    }

    public TableColumn? SortedColumn { get; private set; }
    public bool IsSortDescending { get; private set; }

    public void Sort(TableColumn column)
    {
        if (SortedColumn == column)
        {
            IsSortDescending = !IsSortDescending;
        }
        else
        {
            SortedColumn = column;
            IsSortDescending = false;
        }

        int colIndex = Columns.IndexOf(column);
        if (colIndex < 0) return;

        // Sort is not available on ObservableCollection, so we sort a list and refill
        // We unsubscribe to avoid triggering updates for every item add
        _rows.CollectionChanged -= OnRowsCollectionChanged;

        var list = new List<TableRow>(_rows);
        list.Sort((a, b) =>
        {
            if (a == b) return 0;

            int result = 0;
            if (column.SortComparer != null)
            {
                object? valA = column.SortKeySelector != null ? column.SortKeySelector(a) : GetCellValue(a, colIndex);
                object? valB = column.SortKeySelector != null ? column.SortKeySelector(b) : GetCellValue(b, colIndex);
                result = column.SortComparer(valA!, valB!);
            }
            else if (column.SortKeySelector != null)
            {
                var keyA = column.SortKeySelector(a);
                var keyB = column.SortKeySelector(b);
                if (keyA is IComparable cA) result = cA.CompareTo(keyB);
                else result = (keyA?.ToString() ?? "").CompareTo(keyB?.ToString() ?? "");
            }
            else
            {
                var textA = GetCellText(a, colIndex);
                var textB = GetCellText(b, colIndex);
                result = string.Compare(textA, textB, StringComparison.CurrentCultureIgnoreCase);
            }

            return IsSortDescending ? -result : result;
        });

        _rows.Clear();
        foreach (var item in list) _rows.Add(item);

        _rows.CollectionChanged += OnRowsCollectionChanged;

        _rowsDirty = true;
        Invalidate();
    }

    private object? GetCellValue(TableRow row, int colIndex)
    {
        if (colIndex >= row.Cells.Count) return null;
        var cell = row.Cells[colIndex];
        if (cell is TextBlock tb) return tb.Text;
        return cell;
    }

    private string GetCellText(TableRow row, int colIndex)
    {
        var val = GetCellValue(row, colIndex);
        return val?.ToString() ?? "";
    }

    public override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        int borderOffset = ShowBorder ? 1 : 0;
        int headerHeight = ShowHeader ? 2 : 0;

        if (ShowHeader && e.Y >= borderOffset && e.Y < borderOffset + 1)
        {
            int x = borderOffset;
            for (int i = 0; i < Columns.Count; i++)
            {
                var col = Columns[i];
                if (e.X >= x && e.X < x + col.ActualWidth)
                {
                    Sort(col);
                    break;
                }
                x += col.ActualWidth + 1;
            }
            e.Handled = true;
            return;
        }

        if (PageSize > 0 && e.Y == RenderSize.Height - 1)
        {
            HandlePaginationClick(e.X);
            e.Handled = true;
            return;
        }

        int bodyStartY = borderOffset + headerHeight;
        int y = e.Y - bodyStartY + _scrollViewer.VerticalOffset;

        if (y >= 0)
        {
            int currentY = 0;
            foreach (var child in _rowStack.Children)
            {
                int h = child.RenderSize.Height > 0 ? child.RenderSize.Height : 1;
                if (y >= currentY && y < currentY + h)
                {
                    if (child is TableRow row)
                    {
                        int rowIdx = _rows.IndexOf(row);
                        if (rowIdx >= 0)
                        {
                            SelectedIndex = rowIdx;
                            SelectionChanged?.Invoke(this, EventArgs.Empty);
                        }
                    }
                    break;
                }
                currentY += h;
            }
        }
        e.Handled = true;
    }

    public int PageSize
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                _rowsDirty = true;
                Invalidate();
            }
        }
    } = 0;

    public int CurrentPage
    {
        get;
        set
        {
            if (value < 0) value = 0;
            int max = TotalPages > 0 ? TotalPages - 1 : 0;
            if (value > max) value = max;

            if (field != value)
            {
                field = value;
                PageChanged?.Invoke(this, EventArgs.Empty);
                _rowsDirty = true;
                Invalidate();
            }
        }
    } = 0;

    public int TotalRows
    {
        get;
        set
        {
            if (field != value)
            {
                field = value;
                _rowsDirty = true;
                Invalidate();
            }
        }
    } = -1;

    public event EventHandler? PageChanged;

    private bool IsInternalPaging => PageSize > 0 && (TotalRows < 0 || TotalRows <= _rows.Count) && _rows.Count > PageSize;
    private int EffectiveTotalRows => TotalRows >= 0 ? TotalRows : _rows.Count;
    public int TotalPages
    {
        get
        {
            if (PageSize <= 0) return 1;
            int total = EffectiveTotalRows;
            if (total == 0) return 0;
            return (int)Math.Ceiling((double)total / PageSize);
        }
    }

    private void HandlePaginationClick(int localX)
    {
        int w = RenderSize.Width;
        int totalPages = TotalPages;
        if (totalPages <= 1) return;

        Span<char> buffer = stackalloc char[256];
        int len = GetPaginationString(buffer, w, totalPages, CurrentPage);
        var text = buffer.Slice(0, len);

        int startX = (w - len) / 2;
        if (localX < startX || localX >= startX + len) return;

        int charIdx = localX - startX;

        // Check for <
        int lessThanIdx = text.IndexOf('<');
        if (lessThanIdx >= 0 && lessThanIdx == charIdx)
        {
            if (CurrentPage > 0) CurrentPage--;
            return;
        }
        // Check for >
        int greaterThanIdx = text.LastIndexOf('>');
        if (greaterThanIdx >= 0 && greaterThanIdx == charIdx)
        {
            if (CurrentPage < totalPages - 1) CurrentPage++;
            return;
        }

        if (charIdx < 0 || charIdx >= len || text[charIdx] == ' ') return;

        // Find token boundaries around charIdx
        int tokenStart = text.Slice(0, charIdx).LastIndexOf(' ');
        if (tokenStart == -1) tokenStart = 0; else tokenStart++;

        int tokenEnd = text.Slice(charIdx).IndexOf(' ');
        if (tokenEnd == -1) tokenEnd = len; else tokenEnd += charIdx;

        if (tokenStart >= 0 && tokenEnd > tokenStart && tokenEnd <= len)
        {
            var token = text.Slice(tokenStart, tokenEnd - tokenStart);
            if (token.Length > 2 && token[0] == '[' && token[token.Length - 1] == ']')
            {
                token = token.Slice(1, token.Length - 2);
            }

            if (int.TryParse(token, out int pNum))
            {
                CurrentPage = pNum - 1;
            }
        }
    }

    internal static int GetPaginationString(Span<char> destination, int availableWidth, int totalPages, int currentPage)
    {
        int cp = currentPage + 1;

        // Calculate status string length: "< {cp} of {totalPages} >"
        // "< " (2) + digits(cp) + " of " (4) + digits(totalPages) + " >" (2)
        int statusLen = 8 + GetDigitCount(cp) + GetDigitCount(totalPages);

        if (statusLen > availableWidth)
        {
            // "< >"
            if (destination.Length < 3) return 0;
            destination[0] = '<'; destination[1] = ' '; destination[2] = '>';
            return 3;
        }

        // detailed check
        if (availableWidth > 30)
        {
            // Try generate detailed string
            int pos = 0;

            // Ensure destination is big enough?
            // We assume caller passes 256 which is enough for logic.
            // But we should be safe.
            if (destination.Length < 256) return 0; // Should not happen with stackalloc 256

            destination[pos++] = '<';

            // Page 1
            AppendPage(destination, ref pos, 1, cp);

            int start = Math.Max(2, cp - 2);
            int end = Math.Min(totalPages - 1, cp + 2);

            if (start > 2) AppendDots(destination, ref pos);

            for (int i = start; i <= end; i++)
            {
                AppendPage(destination, ref pos, i, cp);
            }

            if (end < totalPages - 1) AppendDots(destination, ref pos);

            if (totalPages > 1) AppendPage(destination, ref pos, totalPages, cp);

            destination[pos++] = ' ';
            destination[pos++] = '>';

            if (pos <= availableWidth)
            {
                return pos;
            }
        }

        // Fallback to status string
        return CreateStatusString(destination, cp, totalPages);
    }

    private static void AppendPage(Span<char> span, ref int pos, int p, int cp)
    {
        if (p == cp)
        {
            // " [{p}]"
            span[pos++] = ' '; span[pos++] = '[';
            p.TryFormat(span.Slice(pos), out int chars);
            pos += chars;
            span[pos++] = ']';
        }
        else
        {
            // " {p}"
            span[pos++] = ' ';
            p.TryFormat(span.Slice(pos), out int chars);
            pos += chars;
        }
    }

    private static void AppendDots(Span<char> span, ref int pos)
    {
        " ...".CopyTo(span.Slice(pos));
        pos += 4;
    }

    private static int CreateStatusString(Span<char> span, int cp, int totalPages)
    {
        // "< {cp} of {totalPages} >"
        span[0] = '<'; span[1] = ' ';
        int pos = 2;
        cp.TryFormat(span.Slice(pos), out int written);
        pos += written;
        " of ".CopyTo(span.Slice(pos));
        pos += 4;
        totalPages.TryFormat(span.Slice(pos), out written);
        pos += written;
        span[pos++] = ' '; span[pos++] = '>';
        return pos;
    }

    private static int GetDigitCount(int n)
    {
        if (n < 10) return 1;
        if (n < 100) return 2;
        if (n < 1000) return 3;
        if (n < 10000) return 4;
        if (n < 100000) return 5;
        if (n < 1000000) return 6;
        if (n < 10000000) return 7;
        if (n < 100000000) return 8;
        if (n < 1000000000) return 9;
        return 10;
    }

    private void RenderPagination(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        if (PageSize <= 0) return;
        int totalPages = TotalPages;
        if (totalPages <= 1) return;

        int w = RenderSize.Width;
        int y = RenderSize.Height - 1;

        buffer.DrawHLine(RenderSize.X + offsetX, RenderSize.Y + offsetY + y, w, ' ', ConsoleColor.Gray, ConsoleColor.Black);

        Span<char> textBuffer = stackalloc char[256];
        int len = GetPaginationString(textBuffer, w, totalPages, CurrentPage);
        var text = textBuffer.Slice(0, len);

        int startX = (w - len) / 2;
        int absX = RenderSize.X + offsetX + startX;
        int absY = RenderSize.Y + offsetY + y;

        buffer.DrawString(absX, absY, text, ConsoleColor.Gray, ConsoleColor.Black);
    }
}

// Intent: Prevent CPU spin when rendering separators in unbounded layout containers (like ScrollViewer)
// Why:
// - ScrollViewer gives infinite Horizontal DesiredSize, causing the separator to iterate to int.MaxValue if unchecked.
// Constraints/Invariants:
// - Separator width must be bounded by the actual Table width or current viewport width.
// Failure modes:
// - App freezes/spins at 100% CPU when rendering long tables if width isn't clamped.
// Verification:
// - Verify no CPU spikes when scrolling and resizing app windows containing tables.
internal class TableSeparator : UIElement
{
    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(0, 1);
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        var table = FindAncestor<Table>();
        if (table == null) return;

        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;

        // Ensure that width is capped by what the Table actually measures,
        // rather than the infinite horizontal scroll space given by ScrollViewer.
        int width = Math.Min(RenderSize.Width, table.RenderSize.Width);

        char hChar = '\u2500';
        char crossChar = '\u253C';

        buffer.DrawHLine(x, y, width, hChar, ConsoleColor.Gray, ConsoleColor.Black);

        if (table.ShowVerticalLines)
        {
            int cx = 0;
            for (int i = 0; i < table.Columns.Count - 1; i++)
            {
                cx += table.Columns[i].ActualWidth;
                buffer.SetPixel(x + cx, y, crossChar, ConsoleColor.Gray, ConsoleColor.Black);
                cx++;
            }
        }
    }
}
