using System;
using System.Collections.Generic;
using System.Linq;

namespace Tedd.TUI;

public enum GridUnitType
{
    Auto,
    Pixel,
    Star
}

public struct GridLength
{
    public double Value;
    public GridUnitType GridUnitType;

    public GridLength(double value, GridUnitType type)
    {
        Value = value;
        GridUnitType = type;
    }

    public static GridLength Auto => new GridLength(1, GridUnitType.Auto);
    public static GridLength Star => new GridLength(1, GridUnitType.Star);
    public static GridLength Pixel(int value) => new GridLength(value, GridUnitType.Pixel);
}

public class TableColumn
{
    public string Header { get; set; }
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

    // TableRow doesn't really layout itself independently; it relies on the Table to tell it column widths.
    // However, for Measure, we might simple measure children.
    
    public override int VisualChildrenCount => _cells.Count;

    public override UIElement GetVisualChild(int index)
    {
        if (index < 0 || index >= _cells.Count) throw new ArgumentOutOfRangeException(nameof(index));
        return _cells[index];
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // TableRow height is max of children heights? 
        // Width will be dictated by Table.
        // We measure children with infinite width to see what they want, or wait for Table?
        // The Table needs to measure children to resolve Auto columns.
        
        int maxHeight = 0;
        foreach (var cell in _cells)
        {
             cell.Measure(new Size(int.MaxValue, availableSize.Height)); // Allow cells to desire whatever width
             maxHeight = Math.Max(maxHeight, cell.DesiredSize.Height);
        }
        return new Size(0, maxHeight > 0 ? maxHeight : 1); 
    }

    protected override void ArrangeOverride(Size finalSize)
    {
        // Handled by Render or by Table arranging us? 
        // Table will Arrange the Row.
        // Row must Arrange its Cells.
        // But Row doesn't know Column widths unless we pass them or store them.
        // We'll let Table handle the precise Arrange of cells OR Table passes info to Row.
        // Let's have Table arrange the Row, and Row arrange Cells based on Table's calculated columns.
        // But Row is a UIElement, it won't have reference to Table easily unless we cast Parent.
        
        var table = FindAncestor<Table>();
        if (table != null)
        {
            int x = 0;
            // Spacing logic? Table.Measure/Arrange might need to pass spacing or we assume?
            // Table.ArrangeOverride uses `totalW += c.ActualWidth + 1`.
            // So we should assume spacing.
            
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

        foreach (var cell in _cells)
        {
            cell.Render(buffer, x, y);
        }
    }
}

public class Table : UIElement
{
    public List<TableColumn> Columns { get; } = new List<TableColumn>();
    
    private List<TableRow> _rows = new List<TableRow>();
    public IList<TableRow> Rows => _rows;

    private readonly ScrollViewer _scrollViewer;
    private readonly StackPanel _rowStack;

    public bool ShowHeader { get; set; } = true;
    public ConsoleColor HeaderForeground { get; set; } = ConsoleColor.Yellow;
    public ConsoleColor HeaderBackground { get; set; } = ConsoleColor.DarkGray;
    
    // Selection
    private int _selectedIndex = -1;
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (_selectedIndex != value)
            {
                _selectedIndex = value;
                SelectionChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }
    }
    public event EventHandler SelectionChanged;

    public Table()
    {
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

    public void AddRow(TableRow row)
    {
        _rows.Add(row);
        // row.Parent = this; // Row parent is now effectively managed by StackPanel? 
        // No, StackPanel.AddChild sets parent to StackPanel.
        // We defer adding to StackPanel until UpdateVisibleRows.
        Invalidate();
    }
    
    public void AddRow(params object[] values)
    {
        var row = new TableRow();
        foreach (var val in values)
        {
            if (val is UIElement uie) row.AddCell(uie);
            else row.AddCell(val?.ToString() ?? "");
        }
        AddRow(row);
    }

    public override int VisualChildrenCount => 1; // Only ScrollViewer is a direct visual child (Header is drawn manually)
    public override UIElement GetVisualChild(int index) 
    {
        if (index == 0) return _scrollViewer;
        throw new ArgumentOutOfRangeException(nameof(index));
    }

    protected override void OnDataContextChanged(object newValue)
    {
        base.OnDataContextChanged(newValue);
        _scrollViewer.DataContext = newValue;
    }

    private void UpdateVisibleRows()
    {
        // Rebuild _rowStack children based on current page / valid rows.
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
             // External paging or mixed.
             count = Math.Min(PageSize, _rows.Count);
        }

        for (int i = 0; i < count; i++)
        {
            int realIdx = startIdx + i;
            if (realIdx < _rows.Count)
            {
                _rowStack.AddChild(_rows[realIdx]);
            }
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // 1. Reset ActualWidths
        foreach (var col in Columns) col.ActualWidth = 0;

        // 2. Measure headers if Auto
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

        // 3. Scan ALL rows to determine Auto column widths.
        // We must scan _rows, not just visible ones, if we want stable columns?
        // Or scan only visible? Usually stable is better, but expensive.
        // Let's scan visible ones (in _rowStack) or scan data source?
        // To be correct with pagination creating different widths per page is jarring.
        // Let's scan a subset or current page rows.
        
        // Sync row stack first? Or Measure calls UpdateVisibleRows?
        // Let's update rows before measure.
        UpdateVisibleRows();

        foreach (var row in _rows) // Ideally only measured rows, but we need width from all?
        {
            // We can't Measure() rows that are not in visual tree (StackPanel) properly without attaching?
            // But _rows contains all.
            // Let's just measure rows that are going to be displayed for Auto sizing?
            // If we only measure visible, column widths jump between pages.
            // Compromise: Measure current page rows.
        }
        
        // For column auto-width, we rely on the rows we just put in _rowStack.
        foreach (var child in _rowStack.Children)
        {
            // We need to measure it to let it size its cells.
            // BUT TableRow.MeasureOverride currently assumes infinite width for cells.
            // We can call Measure on it.
            child.Measure(new Size(availableSize.Width, availableSize.Height)); // Height irrelevant for width calc

            if (child is TableRow row)
            {
                 // Logic from previous MeasureOverride implementation
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

        // 4. Resolve Star widths
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

        int scrollBarWidth = _scrollViewer.VerticalScrollBarVisibility ? 1 : 0;
        // The ScrollViewer will take available width.
        // We should subtract scrollbar width from available space for columns?
        // Yes, if ScrollBar is visible.
        
        int remainingWidth = Math.Max(0, availableSize.Width - usedWidth - scrollBarWidth);
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

        // 5. Measure ScrollViewer
        int headerHeight = ShowHeader ? 1 : 0;
        int footerHeight = PageSize > 0 ? 1 : 0;
        int bodyHeight = Math.Max(0, availableSize.Height - headerHeight - footerHeight);
        
        _scrollViewer.Measure(new Size(availableSize.Width, bodyHeight));

        return new Size(availableSize.Width, headerHeight + _scrollViewer.DesiredSize.Height + footerHeight);
    }

    protected override void ArrangeOverride(Size finalSize)
    {
        int headerHeight = ShowHeader ? 1 : 0;
        int footerHeight = PageSize > 0 ? 1 : 0;
        int bodyHeight = Math.Max(0, finalSize.Height - headerHeight - footerHeight);

        // Header is just drawn, no child to arrange.
        
        // Arrange ScrollViewer
        _scrollViewer.Arrange(new Rect(0, headerHeight, finalSize.Width, bodyHeight));
        
        // Footer is just drawn.
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
         int x = RenderSize.X + offsetX;
         int y = RenderSize.Y + offsetY;

         // Draw Header
         if (ShowHeader)
         {
            int colX = x;
            for (int i = 0; i < Columns.Count; i++)
            {
                var col = Columns[i];
                string h = col.Header ?? "";
                if (h.Length > col.ActualWidth) h = h.Substring(0, col.ActualWidth);
                
                // Draw background
                for(int dx=0; dx<col.ActualWidth; dx++)
                    buffer.SetPixel(colX + dx, y, ' ', HeaderForeground, HeaderBackground);
                
                // Draw text
                for(int dx=0; dx<h.Length; dx++)
                    buffer.SetPixel(colX + dx, y, h[dx], HeaderForeground, HeaderBackground);
                
                colX += col.ActualWidth;
                
                // Separator
                if (i < Columns.Count - 1)
                {
                   buffer.SetPixel(colX, y, '│', ConsoleColor.Gray, ConsoleColor.Black);
                   colX++;
                }
            }
         }

         // Draw Body (ScrollViewer)
         // ScrollViewer Render will handle children.
         // We just passed Arrange, so it knows where to draw.
         // ScrollViewer is a child, so we call its render.
         _scrollViewer.Render(buffer, x, y);

         // Draw Footer
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
            if (SelectedIndex > 0)
            {
                SelectedIndex--;
                EnsureVisible(SelectedIndex);
            }
            e.Handled = true;
        }
        else if (e.Key == ConsoleKey.DownArrow)
        {
            // Max index? 
            int max = IsInternalPaging ? _rows.Count : (TotalRows > 0 ? TotalRows : _rows.Count); 
            // If external paging, we might select index > _rows.Count?
            // No, SelectedIndex usually refers to the loaded rows? 
            // OR global index? 
            // If we support lazy loading, SelectedIndex should probably be global.
            // But _rows only has local. 
            // Let's assume SelectedIndex is GLOBAL.
            // If External Paging, and we have 5 rows, but TotalRows=100.
            // Rows has 5 items.
            // Index 0..4 map to Page 0.
            // Index 5..9 map to Page 1? But Rows only has 5 items.
            // This implies Rows must be swapped.
            // So indices are always 0..PageSize-1 relative to view, or absolute?
            // "Add 20 items... fetch new page" implies global abstraction.
            // But implementation-wise, Table usually binds to what it has.
            // Let's stick to: SelectedIndex is GLOBAL.
            // If internal paging: easy.
            // If external paging: we need to map global index to local row?
            // Too complex for typical TUI table. 
            // Let's match typical patterns: SelectedIndex is index into the DATA SOURCE (global).
            // If we are on Page 1 (items 10-19), SelectedIndex 11 corresponds to Row 1 (locally).
            
            // If External, and we only have 10 rows loaded.
            // User presses Down on item 9. SelectedIndex becomes 10.
            // EnsureVisible(10) -> Checks current page. 10 is on Page 1 (0-9 is Page 0).
            // So CurrentPage becomes 1.
            // PageChanged fires.
            // User Handler loads Page 1 rows (indices 10-19? or 0-9?).
            // Usually valid implementations: 
            // A) Rows are replaced. Index 10 doesn't exist in Rows.
            //    So we must map SelectedIndex 10 to Row 0?
            //    That's confusing.
            //    Better: SelectedIndex is always GLOBAL.
            //    When rendering/mapping, we subtract Page * PageSize?
            
            // Let's assume GLOBAL SelectedIndex.
            
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
            // Paging Mode
            int page = index / PageSize;
            if (page != CurrentPage)
            {
                CurrentPage = page;
                // PageChanged implicitly handled, triggers UpdateVisibleRows
            }
            // If already on page, do we need to scroll ScrollViewer?
            // Usually not if PageSize fits in Table height. But if rows are tall?
            // Yes, user wants scrollbar if overflows.
            
            // Find row in _rowStack?
            // The row is likely in _rowStack if on current page.
            if (index >= 0 && index < _rows.Count)
            {
                // We need to calculate position of this row inside _rowStack.
                // ScrollViewer works with vertical offset.
                // We need to find the Y position of the selected row relative to _rowStack.
                
                // Since _rows are added to StackPanel, we can iterate StackPanel children?
                // Or calculate from heights? Row heights are dynamic.
                // Best is to find the child control corresponding to _rows[index].
                
                var row = _rows[index];
                if (row.Parent == _rowStack) // Check if attached
                {
                   // To ensure visible, we need to know its position.
                   // StackPanel arranges children. If Arrange has run, render size/pos is known?
                   // No, RenderSize.Y is relative to StackPanel.
                   
                   // But Arrange only runs during Layout pass.
                   // If we just gathered visible rows, layout hasn't run yet.
                   // So we might need to UpdateLayout?
                   
                   // However, simplified approach:
                   // Just rely on user scrolling or minimal auto-scroll if possible.
                   // For now, let's just ensure Page.
                }
            }
            Invalidate();
        }
        else
        {
            // Scrolling Mode (classic / no pagination) via ScrollViewer?
            // If No Paging, all rows are in _rowStack.
            
            // Update ScrollViewer offset?
            // This requires measuring/positioning. The ScrollViewer logic handles scrolling via ScrollBar interactively.
            // Programmatic EnsureVisible would require finding the target Y.
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

        // Perform Sort
        // We need to know which generic type to compare if no Comparer is given.
        // We can use a default strategy: Try key selector, or try parsing text from cell.
        
        int colIndex = Columns.IndexOf(column);
        if (colIndex < 0) return;

        _rows.Sort((a, b) =>
        {
            if (a == b) return 0;
            
            int result = 0;
            
            // 1. Explicit Comparer
            if (column.SortComparer != null)
            {
                // We need values.
                // If SortKeySelector is present, use it.
                object? valA = column.SortKeySelector != null ? column.SortKeySelector(a) : GetCellValue(a, colIndex);
                object? valB = column.SortKeySelector != null ? column.SortKeySelector(b) : GetCellValue(b, colIndex);
                result = column.SortComparer(valA!, valB!);
            }
            // 2. Explicit Key Selector (comparable)
            else if (column.SortKeySelector != null)
            {
                var keyA = column.SortKeySelector(a);
                var keyB = column.SortKeySelector(b);
                if (keyA is IComparable cA) result = cA.CompareTo(keyB);
                else result = (keyA?.ToString() ?? "").CompareTo(keyB?.ToString() ?? "");
            }
            // 3. Default: Text of cell
            else
            {
                var textA = GetCellText(a, colIndex);
                var textB = GetCellText(b, colIndex);
                
                // Try numeric? User request says "Get some good samples that are not alphabetic".
                // We could try parsing as double if both look like numbers?
                // Or just string compare for default. 
                // Let's stick to string compare for default to be safe, unless user sets a custom sorter.
                // But user asked for numeric. 
                // Let's rely on user setting SortKeySelector or Comparer for numeric, or we can add a smart default.
                // Let's do simple string compare here.
                result = string.Compare(textA, textB, StringComparison.CurrentCultureIgnoreCase);
            }

            return IsSortDescending ? -result : result;
        });

        Invalidate();
    }

    private object? GetCellValue(TableRow row, int colIndex)
    {
        if (colIndex >= row.Cells.Count) return null;
        var cell = row.Cells[colIndex];
        // If cell has a Tag, maybe use that?
        // Or TextBlock text.
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
        // Header Hit Test
        if (ShowHeader && e.Y == 0)
        {
            // Find column
            int x = 0;
            for(int i=0; i<Columns.Count; i++)
            {
                var col = Columns[i];
                if (e.X >= x && e.X < x + col.ActualWidth)
                {
                    Sort(col);
                    break;
                }
                x += col.ActualWidth;
                x += (i < Columns.Count - 1) ? 1 : 0; // Separator
            }
            e.Handled = true;
            return;
        }

        // Pagination Hit Test
        if (PageSize > 0 && e.Y == RenderSize.Height - 1)
        {
            // Handle pagination clicks
            HandlePaginationClick(e.X);
            e.Handled = true;
            return;
        }

        // Hit test for row
        int y = e.Y;
        if (ShowHeader) y--;
        
        if (y >= 0)
        {
            // If paging is enabled, we need to consider that the rendered rows
            // might be offset if we are doing internal paging?
            // Actually, Render just renders what's in _rows (sliced or not).
            // But we need to know which row was clicked.
            
            // If internal paging: rendered rows are _rows[CurrentPage*PageSize ...].
            // If external paging: rendered rows are _rows[0 ...].
            
            // If Scrolling (PageSize=0): rendered rows are _rows[_scrollOffset ...].

            int startIdx = 0;
            int count = _rows.Count;
            bool useScrolling = PageSize <= 0;

            if (IsInternalPaging)
            {
                startIdx = CurrentPage * PageSize;
                count = Math.Min(PageSize, _rows.Count - startIdx);
                
                int currentY = 0;
                for (int i = 0; i < count; i++)
                {
                    int realIdx = startIdx + i;
                    if (realIdx >= _rows.Count) break;

                    var row = _rows[realIdx];
                    int rh = row.RenderSize.Height > 0 ? row.RenderSize.Height : 1;
                    
                    if (y >= currentY && y < currentY + rh)
                    {
                        SelectedIndex = realIdx;
                        SelectionChanged?.Invoke(this, EventArgs.Empty);
                        break;
                    }
                    currentY += rh;
                }
            }
            else if (useScrolling)
            {
                // ScrollViewer based scrolling (pixel/line based)
                int scrollY = _scrollViewer.VerticalOffset;
                int targetY = y + scrollY;

                int currentY = 0;
                for (int i = 0; i < _rows.Count; i++)
                {
                    var row = _rows[i];
                    int rh = row.RenderSize.Height > 0 ? row.RenderSize.Height : 1;

                    if (targetY >= currentY && targetY < currentY + rh)
                    {
                        SelectedIndex = i;
                        SelectionChanged?.Invoke(this, EventArgs.Empty);
                        break;
                    }
                    currentY += rh;
                }
            }
        }
        e.Handled = true;
    }

    // Pagination
    private int _pageSize = 0;
    public int PageSize
    {
        get => _pageSize;
        set
        {
            if (_pageSize != value)
            {
                _pageSize = value;
                Invalidate();
            }
        }
    }

    private int _currentPage = 0;
    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            if (value < 0) value = 0;
            // Cap at MaxPages? Need TotalRows for that.
            int max = TotalPages > 0 ? TotalPages - 1 : 0;
            if (value > max) value = max;

            if (_currentPage != value)
            {
                _currentPage = value;
                PageChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }
    }

    private int _totalRows = -1;
    public int TotalRows
    {
        get => _totalRows;
        set
        {
            if (_totalRows != value)
            {
                _totalRows = value;
                Invalidate();
            }
        }
    }

    public event EventHandler? PageChanged;

    private bool IsInternalPaging => PageSize > 0 && (_totalRows < 0 || _totalRows <= _rows.Count) && _rows.Count > PageSize;
    private int EffectiveTotalRows => _totalRows >= 0 ? _totalRows : _rows.Count;
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
        // Must match RenderPagination logic to find targets
        // Simple logic: we just need to know where the buttons are.
        // This is tricky because RenderPagination is dynamic.
        // We can either recalculate or store regions. 
        // Let's recalculate simply.
        
        int w = RenderSize.Width;
        int totalPages = TotalPages;
        if (totalPages <= 1) return;

        // Same logic as Render
        string text = GetPaginationString(w, totalPages);
        // " < 1 2 3 > "
        // We center this string.
        int startX = (w - text.Length) / 2;
        if (localX < startX || localX >= startX + text.Length) return;

        int charIdx = localX - startX;
        // Parse what we clicked.
        // This is heuristic parsing. 
        
        // Check for Prev/Next arrows
        if (text.Contains("<") && text.IndexOf('<') == charIdx)
        {
            if (CurrentPage > 0) CurrentPage--;
            return;
        }
        if (text.Contains(">") && text.LastIndexOf('>') == charIdx)
        {
            if (CurrentPage < totalPages - 1) CurrentPage++;
            return;
        }

        // Check for numbers
        // We need to map the string back to page numbers.
        // Format: "< 1 2 3 ... 10 >"
        // We can tokenize the string by spaces and find which token we clicked.
        
        if (charIdx < 0 || charIdx >= text.Length || text[charIdx] == ' ') return;

        // Find the token at charIdx
        // Scan back to space
        int tokenStart = text.LastIndexOf(' ', charIdx);
        if (tokenStart == -1) tokenStart = 0; else tokenStart++;
        
        int tokenEnd = text.IndexOf(' ', charIdx);
        if (tokenEnd == -1) tokenEnd = text.Length;

        if (tokenStart >= 0 && tokenEnd > tokenStart && tokenEnd <= text.Length)
        {
            string token = text.Substring(tokenStart, tokenEnd - tokenStart);
            if (int.TryParse(token, out int pNum))
            {
                CurrentPage = pNum - 1;
            }
        }
    }

    private string GetPaginationString(int availableWidth, int totalPages)
    {
        // Strategies:
        // 1. Minimal: "< >" (Length 3)
        // 2. Current: "< 1 >" 
        // 3. Status: "< 1 of 10 >"
        // 4. Expanded: "< 1 2 3 4 5 >"
        
        int cp = CurrentPage + 1;
        
        // Try Full/Expanded first
        // "< 1 ... 4 5 [6] 7 8 ... 10 >"
        // Let's build a smart list.
        // Always include 1, Total.
        // Include Current, +/- surroundings.
        
        // If we have plenty of space, show all?
        // Let's try to generate the "Standard" string: "< 1 2 3 ... N >"
        
        var parts = new List<string>();
        parts.Add("<");
        
        // Determine range to show
        // We want to fit in availableWidth.
        // Let's assume we want as many as possible centered on Current.
        
        // Ideal: 1, 2, ..., CP-1, CP, CP+1, ..., Total
        // If too many, collapse with ...
        
        // Simple logic for "More space":
        // 1 .. Total
        // If Total < 10, show all?
        
        // Let's build a candidate list of page numbers to show.
        // Always show 1, Total.
        // Always show Current.
        // Fill gaps if small, else ...
        
        // Let's try building " < CP of Total > "
        string statusStr = $"< {cp} of {totalPages} >";
        if (statusStr.Length <= availableWidth)
        {
            // Can we do better? 
            // "< 1 2 3 4 5 >"
            // Let's try to fit numbers.
            // Estimate 4 chars per number " N ".
            int maxNums = (availableWidth - 4) / 4; // -4 for "< >" and padding
            
            if (maxNums >= totalPages)
            {
                // Show all
                string s = "<";
                for(int i=1; i<=totalPages; i++)
                {
                    if (i == cp) s += $" [{i}]"; 
                    else s += $" {i}";
                }
                s += " >";
                if (s.Length <= availableWidth) return s;
            }
            
            // Show partial
            // Center around CP.
            // always 1 ... [CP] ... Total
            // Let's just return the status string if implementation of complex ellipsizing is too risky/long for now.
            // User asked: "Moe space, add n for page number, then "< n of n >", then "< 1 2 3 ... 12 >""
            
            // If we have lots of space:
            if (availableWidth > 30) // Arbitrary threshold for "lots"
            {
                 // Try complex string
                 // Logic: 1 .. range .. Total
                 // range = CP-2 to CP+2
                 List<int> pages = new List<int>();
                 pages.Add(1);
                 
                 int start = Math.Max(2, cp - 2);
                 int end = Math.Min(totalPages - 1, cp + 2);
                 
                 if (start > 2) pages.Add(-1); // ...
                 for(int i=start; i<=end; i++) pages.Add(i);
                 if (end < totalPages - 1) pages.Add(-1); // ...
                 
                 if (totalPages > 1) pages.Add(totalPages);
                 
                 string s = "<";
                 foreach(var p in pages)
                 {
                     if (p == -1) s += " ...";
                     else if (p == cp) s += $" [{p}]";
                     else s += $" {p}";
                 }
                 s += " >";
                 
                 if (s.Length <= availableWidth) return s;
            }
            
            return statusStr;
        }
        
        // Smallest
        return "< >";
    }

    private void RenderPagination(VirtualBuffer buffer, int offsetX, int offsetY)
    {
       if (PageSize <= 0) return;
       int totalPages = TotalPages;
       if (totalPages <= 1) return; // Hide if single page? Or always show if PageSize set? Usually hide if 1 page.
       
       int w = RenderSize.Width;
       int y = RenderSize.Height - 1; // Last line
       
       // Clear line?
       for(int i=0; i<w; i++) buffer.SetPixel(RenderSize.X + offsetX + i, RenderSize.Y + offsetY + y, ' ', ConsoleColor.Gray, ConsoleColor.Black);
       
       string text = GetPaginationString(w, totalPages);
       
       int startX = (w - text.Length) / 2;
       int absX = RenderSize.X + offsetX + startX;
       int absY = RenderSize.Y + offsetY + y;
       
       for(int i=0; i<text.Length; i++)
       {
           char c = text[i];
           // Highlight current page number?
           // The GetPaginationString puts brackets [N] for current.
           // We can just render string.
           buffer.SetPixel(absX + i, absY, c, ConsoleColor.Gray, ConsoleColor.Black);
       }
    }
    
    // Override Render to call RenderPagination
    // We need to modify Render method in previous block?
    // The replace tool works on chunks. I need to replace Render as well or append to it.
    // Wait, the previous block I am replacing is OnMouseDown. 
    // I need to patch Render as well.
    // The replace tool accepts [StartLine, EndLine].
    // I can rewrite Render method too if I include it in the range?
    // Render is lines 303-405.
    // OnMouseDown is 539-593.
    // EnsureVisible is 430-454.
    // I need to change OnKeyDown too (for pagination aware navigation).
    // And Measure/Arrange (to reserve space).
    
    // This looks like I should Replace almost the whole file or large chunks.
    // Let's do it in chunks.
    
    // First chunk: Measure/Arrange/Render.
    
    // Wait, I am currently replacing OnMouseDown (end of file).
    // I should cancel this and do it properly from top down or big chunks.
}
