using System;
using System.Collections.Generic;
using System.Linq;
using Tedd.TUI;
using Xunit;
using Xunit.Abstractions;

namespace Tedd.TUI.Tests;

public class TableCoverageTests
{
    private readonly ITestOutputHelper _output;

    public TableCoverageTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // Helper to call internal static method
    private string GetPaginationString(int width, int totalPages, int currentPage)
    {
        Span<char> span = stackalloc char[256];
        int len = Table.GetPaginationString(span, width, totalPages, currentPage);
        return span.Slice(0, len).ToString();
    }

    [Theory]
    // Case 1: cp=0 (Page 1). Result: "< [1] 2 3 ... 10 >"
    [InlineData(10, 0, 50, "< [1] 2 3 ... 10 >")]
    // Case 2: cp=4 (Page 5). Range: [3..7] -> 3 4 5 6 7.
    [InlineData(10, 4, 50, "< 1 ... 3 4 [5] 6 7 ... 10 >")]
    // Case 3: cp=9 (Page 10).
    [InlineData(10, 9, 50, "< 1 ... 8 9 [10] >")]
    [InlineData(5, 2, 50, "< 1 2 [3] 4 5 >")]
    public void Table_GetPaginationString_Detailed_Edges(int totalPages, int currentPage, int width, string expected)
    {
        string result = GetPaginationString(width, totalPages, currentPage);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(10, 5, 20, "< 6 of 10 >")] // Fits in status string but not detailed
    [InlineData(100, 50, 15, "< 51 of 100 >")] // Fits
    public void Table_GetPaginationString_Fallback(int totalPages, int currentPage, int width, string expected)
    {
        string result = GetPaginationString(width, totalPages, currentPage);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(10, 5, 5, "< >")] // Very small width
    [InlineData(100, 50, 8, "< >")] // Width 8 not enough for "< 51 of 100 >" (13 chars)
    public void Table_GetPaginationString_Tiny(int totalPages, int currentPage, int width, string expected)
    {
        string result = GetPaginationString(width, totalPages, currentPage);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Table_OnMouseDown_Header_Sorts()
    {
        var table = new Table();
        table.ShowHeader = true;
        table.ShowBorder = true;

        var col1 = new TableColumn { Header = "ID", Width = new GridLength(5, GridUnitType.Pixel) };
        var col2 = new TableColumn { Header = "Name", Width = new GridLength(10, GridUnitType.Pixel) };
        table.Columns.Add(col1);
        table.Columns.Add(col2);

        table.AddRow("2", "Bob");
        table.AddRow("1", "Alice");

        table.Measure(new Size(20, 10));
        table.Arrange(new Rect(0, 0, 20, 10));

        var args = new MouseEventArgs { X = 2, Y = 1, Handled = false };
        table.OnMouseDown(args);

        Assert.True(args.Handled);
        Assert.Equal(col1, table.SortedColumn);
        Assert.False(table.IsSortDescending);
        Assert.Equal("1", ((TextBlock)table.Rows[0].Cells[0]).Text);

        args.Handled = false;
        table.OnMouseDown(args);
        Assert.True(table.IsSortDescending);
        Assert.Equal("2", ((TextBlock)table.Rows[0].Cells[0]).Text);

        args = new MouseEventArgs { X = 8, Y = 1, Handled = false };
        table.OnMouseDown(args);

        Assert.Equal(col2, table.SortedColumn);
        Assert.False(table.IsSortDescending);
        Assert.Equal("Alice", ((TextBlock)table.Rows[0].Cells[1]).Text);
    }

    [Fact]
    public void Table_OnMouseDown_Pagination_PrevNext()
    {
        var table = new Table();
        table.PageSize = 2;
        for (int i = 0; i < 10; i++) table.AddRow(i.ToString());

        int width = 50;
        table.Measure(new Size(width, 10));
        table.Arrange(new Rect(0, 0, width, 10));

        Assert.Equal(0, table.CurrentPage);

        // Calculate position of '>'
        string pagStr = GetPaginationString(width, table.TotalPages, table.CurrentPage);
        int startX = (width - pagStr.Length) / 2;
        int greaterIndex = pagStr.IndexOf('>');
        int clickX = startX + greaterIndex;

        var args = new MouseEventArgs { X = clickX, Y = 9, Handled = false };
        table.OnMouseDown(args);

        Assert.True(args.Handled);
        Assert.Equal(1, table.CurrentPage);

        // Calculate position of '<' for new page
        pagStr = GetPaginationString(width, table.TotalPages, table.CurrentPage);
        startX = (width - pagStr.Length) / 2;
        int lessIndex = pagStr.IndexOf('<');
        clickX = startX + lessIndex;

        args = new MouseEventArgs { X = clickX, Y = 9, Handled = false };
        table.OnMouseDown(args);

        Assert.Equal(0, table.CurrentPage);
    }

    [Fact]
    public void Table_OnMouseDown_Pagination_PageClick()
    {
        var table = new Table();
        table.PageSize = 1;
        for (int i = 0; i < 5; i++) table.AddRow(i.ToString());

        int width = 50;
        table.Measure(new Size(width, 10));
        table.Arrange(new Rect(0, 0, width, 10));

        // Current Page 0 (1). We want to click '3'.
        string s = GetPaginationString(width, table.TotalPages, table.CurrentPage);
        int startX = (width - s.Length) / 2;
        int idx3 = s.IndexOf('3');
        int clickX = startX + idx3;

        var args = new MouseEventArgs { X = clickX, Y = 9, Handled = false };
        table.OnMouseDown(args);

        Assert.Equal(2, table.CurrentPage); // Page 3 is index 2
    }

    [Fact]
    public void Table_OnKeyDown_Navigation()
    {
        var table = new Table();
        table.AddRow("Row1");
        table.AddRow("Row2");
        table.AddRow("Row3");

        table.SelectedIndex = 0;

        // Key Down
        var args = new KeyEventArgs { Key = ConsoleKey.DownArrow, Handled = false };
        table.OnKeyDown(args);

        Assert.True(args.Handled);
        Assert.Equal(1, table.SelectedIndex);

        // Key Up
        args = new KeyEventArgs { Key = ConsoleKey.UpArrow, Handled = false };
        table.OnKeyDown(args);

        Assert.Equal(0, table.SelectedIndex);

        // Boundary check (Up at 0)
        table.OnKeyDown(args);
        Assert.Equal(0, table.SelectedIndex);
    }

    [Fact]
    public void Table_OnKeyDown_Navigation_PageJump()
    {
        var table = new Table();
        table.PageSize = 2;
        table.AddRow("1");
        table.AddRow("2");
        table.AddRow("3");
        table.AddRow("4");

        table.SelectedIndex = 1;
        table.CurrentPage = 0;

        var args = new KeyEventArgs { Key = ConsoleKey.DownArrow, Handled = false };
        table.OnKeyDown(args);

        Assert.Equal(2, table.SelectedIndex);
        Assert.Equal(1, table.CurrentPage);

        args = new KeyEventArgs { Key = ConsoleKey.UpArrow, Handled = false };
        table.OnKeyDown(args);

        Assert.Equal(1, table.SelectedIndex);
        Assert.Equal(0, table.CurrentPage);
    }

    [Fact]
    public void Table_Sort_CustomComparer()
    {
        var table = new Table();
        var col = new TableColumn { Header = "Num" };
        col.SortComparer = (a, b) =>
        {
            int.TryParse(a?.ToString(), out int ia);
            int.TryParse(b?.ToString(), out int ib);
            return ia.CompareTo(ib);
        };
        table.Columns.Add(col);

        table.AddRow("10");
        table.AddRow("2");
        table.AddRow("1");

        table.Sort(col);

        Assert.Equal("1", ((TextBlock)table.Rows[0].Cells[0]).Text);
        Assert.Equal("2", ((TextBlock)table.Rows[1].Cells[0]).Text);
        Assert.Equal("10", ((TextBlock)table.Rows[2].Cells[0]).Text);
    }

    [Fact]
    public void Table_Sort_KeySelector()
    {
        var table = new Table();
        var col = new TableColumn { Header = "Len" };
        col.SortKeySelector = (row) => ((TextBlock)row.Cells[0]).Text.Length;
        table.Columns.Add(col);

        table.AddRow("Apple");
        table.AddRow("Banana");
        table.AddRow("Kiwi");

        table.Sort(col);

        Assert.Equal("Kiwi", ((TextBlock)table.Rows[0].Cells[0]).Text);
        Assert.Equal("Apple", ((TextBlock)table.Rows[1].Cells[0]).Text);
        Assert.Equal("Banana", ((TextBlock)table.Rows[2].Cells[0]).Text);
    }

    [Fact]
    public void Table_Sort_Nulls()
    {
        var table = new Table();
        var col = new TableColumn { Header = "Val" };
        table.Columns.Add(col);

        table.AddRow("B");
        var nullRow = new TableRow();
        nullRow.AddCell(new TextBlock { Text = null });
        table.AddRow(nullRow);
        table.AddRow("A");

        table.Sort(col);

        var t0 = ((TextBlock)table.Rows[0].Cells[0]).Text;
        var t1 = ((TextBlock)table.Rows[1].Cells[0]).Text;
        var t2 = ((TextBlock)table.Rows[2].Cells[0]).Text;

        Assert.True(string.IsNullOrEmpty(t0));
        Assert.Equal("A", t1);
        Assert.Equal("B", t2);
    }

    [Fact]
    public void Table_Empty_Render()
    {
        var table = new Table();
        var buffer = new VirtualBuffer(10, 10);

        table.Measure(new Size(10, 10));
        table.Arrange(new Rect(0, 0, 10, 10));
        table.Render(buffer);

        Assert.Equal(' ', buffer.GetPixel(0, 0).Character);
    }

    [Fact]
    public void Table_NoColumns_AddRow()
    {
        var table = new Table();
        table.AddRow("Test");

        table.Measure(new Size(10, 10));
    }

    [Fact]
    public void Table_PageSize_Zero()
    {
        var table = new Table();
        for (int i = 0; i < 10; i++) table.AddRow(i.ToString());

        table.PageSize = 0;
        Assert.Equal(1, table.TotalPages);

        table.Measure(new Size(100, 100));
        var stack = (StackPanel)((ScrollViewer)table.GetVisualChild(0)).Content!;

        // Should show all rows, limited by available height?
        // No, Measure will use availableSize.Height.
        // With PageSize=0, UpdateVisibleRows adds ALL rows up to _rows.Count.
        // But layout measures them.
        Assert.Equal(10, stack.Children.Count);
    }

    [Fact]
    public void Table_PageSize_Large()
    {
        var table = new Table();
        table.AddRow("1");
        table.PageSize = 100;

        Assert.Equal(1, table.TotalPages);
        table.Measure(new Size(100, 100));

        var stack = (StackPanel)((ScrollViewer)table.GetVisualChild(0)).Content!;
        Assert.Single(stack.Children);
    }

    [Fact]
    public void Table_Separator_Rendering()
    {
        var table = new Table { ShowHorizontalLines = true, ShowVerticalLines = true, ShowBorder = true, BorderStyle = BoxStyle.Single };

        // Two columns to verify cross character
        table.Columns.Add(new TableColumn { Width = new GridLength(3, GridUnitType.Pixel) });
        table.Columns.Add(new TableColumn { Width = new GridLength(3, GridUnitType.Pixel) });

        // Two rows to verify separator between them
        table.AddRow("A", "B");
        table.AddRow("C", "D");

        // Header(2) + Row1(1) + Sep(1) + Row2(1) + Border(2) = 7 height needed.
        // Width: Border(1) + Col1(3) + VLine(1) + Col2(3) + Border(1) = 9.

        table.Measure(new Size(20, 20));
        table.Arrange(new Rect(0, 0, 9, 7));

        var buffer = new VirtualBuffer(9, 7);
        table.Render(buffer, 0, 0);

        // Expected Layout:
        // 0: ┌───────┐
        // 1: │   │   │ (Header)
        // 2: ├───┼───┤ (Header Sep)
        // 3: │A  │B  │ (Row 1)
        // 4: ├───┼───┤ (Separator) <-- Target
        // 5: │C  │D  │ (Row 2)
        // 6: └───────┘

        // Row 4 should be separator
        // X=0: Left Junction (u2520 ┠) if ShowBorder && ShowHorizontalLines
        // X=1..3: Horz Line (u2500 ─)
        // X=4: Cross (u253C ┼) if ShowVerticalLines
        // X=5..7: Horz Line
        // X=8: Right Junction (u2528 ┨)

        // Verify Row 4
        Assert.Equal('\u251C', buffer.GetPixel(0, 4).Character); // Left Junction (Single)
        Assert.Equal('\u2500', buffer.GetPixel(1, 4).Character);
        Assert.Equal('\u253C', buffer.GetPixel(4, 4).Character); // Cross (Single)
        Assert.Equal('\u2500', buffer.GetPixel(5, 4).Character);
        Assert.Equal('\u2524', buffer.GetPixel(8, 4).Character); // Right Junction (Single)
    }
}
