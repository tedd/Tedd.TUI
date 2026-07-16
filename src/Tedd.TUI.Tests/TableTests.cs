using System;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;
using System.Collections.Generic;
using System.Linq;
using Tedd.TUI.Tests.TestInfrastructure;

namespace Tedd.TUI.Tests;

public class TableTests
{
    private readonly ITestOutputHelper _output;

    public TableTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void Table_AddRow_Updates_VisualTree()
    {
        var table = new Table();
        table.ShowHorizontalLines = false;

        // Initial state
        table.Measure(new Size(100, 100));
        var stack = GetRowStack(table);
        Assert.Empty(stack.Children);

        // Add row
        table.AddRow("Col1", "Col2");
        table.Measure(new Size(100, 100)); // Layout pass

        Assert.Single(stack.Children);
    }

    [Fact]
    public void Table_ShowHorizontalLines_Adds_Separators()
    {
        var table = new Table();
        table.ShowHorizontalLines = false;
        table.AddRow("Row1");
        table.AddRow("Row2");

        table.Measure(new Size(100, 100));
        var stack = GetRowStack(table);
        Assert.Equal(2, stack.Children.Count); // 2 rows, no separators

        // Enable lines
        table.ShowHorizontalLines = true;
        table.Measure(new Size(100, 100));

        // 2 rows + 1 separator = 3 children
        Assert.Equal(3, stack.Children.Count);
    }

    [Fact]
    public void Table_PageSize_Limits_VisibleRows()
    {
        var table = new Table();
        table.ShowHorizontalLines = false;
        for (int i = 0; i < 10; i++) table.AddRow($"Row {i}");

        table.PageSize = 5;
        table.Measure(new Size(100, 100));

        var stack = GetRowStack(table);
        Assert.Equal(5, stack.Children.Count);
    }

    [Fact]
    public void Table_Layout_Performance_Benchmark()
    {
        // Setup
        var table = new Table();
        table.ShowHorizontalLines = true; // Force separator creation for maximum impact
        table.Columns.Add(new TableColumn { Header = "Col1" });
        table.Columns.Add(new TableColumn { Header = "Col2" });

        int rowCount = 1000;
        for (int i = 0; i < rowCount; i++)
        {
            table.AddRow("Row" + i, "Value" + i);
        }

        // Measure
        var sw = Stopwatch.StartNew();
        int iterations = 100;

        for (int i = 0; i < iterations; i++)
        {
            table.Measure(new Size(80, 25));
        }

        sw.Stop();

        _output.WriteLine($"Time for {iterations} layout passes with {rowCount} rows: {sw.ElapsedMilliseconds} ms");

        // Assert that it's "fast enough" or just use this for manual comparison
        Assert.True(sw.ElapsedMilliseconds > 0);
    }

    [Fact]
    public void Table_Rows_CollectionChanged_Updates_VisualTree()
    {
        var table = new Table();
        table.ShowHorizontalLines = false;
        table.Measure(new Size(100, 100));

        // Add via Rows property (IList)
        table.Rows.Add(new TableRow());
        table.Measure(new Size(100, 100));

        var stack = GetRowStack(table);
        Assert.Single(stack.Children);
    }

    [Fact]
    public void Table_Layout_Consistency()
    {
        var table = new Table();
        // Add a column with Auto width
        table.Columns.Add(new TableColumn { Header = "Header", Width = GridLength.Auto });

        // Add row with content wider than header
        table.AddRow("Wide Content Row");

        // First Measure
        table.Measure(new Size(100, 100));
        var colWidth = GetActualWidth(table.Columns[0]);
        Assert.True(colWidth >= "Wide Content Row".Length);

        // Second Measure (should be consistent, not reset to Header width)
        table.Measure(new Size(100, 100));
        Assert.Equal(colWidth, GetActualWidth(table.Columns[0]));
    }

    private int GetActualWidth(TableColumn col)
    {
        var prop = typeof(TableColumn).GetProperty("ActualWidth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (int)prop!.GetValue(col)!;
    }

    private StackPanel GetRowStack(Table table)
    {
        var sv = (ScrollViewer)table.GetVisualChild(0);
        return (StackPanel)sv.Content!;
    }

    [Fact]
    public void Table_Sorting_Logic()
    {
        var table = new Table();
        table.Columns.Add(new TableColumn { Header = "Name" });
        table.Columns.Add(new TableColumn { Header = "Age" }); // String sort

        table.AddRow("Alice", "30");
        table.AddRow("Bob", "20");
        table.AddRow("Charlie", "25");

        // Sort by Name (Column 0) - Ascending
        table.Sort(table.Columns[0]);

        Assert.Equal("Alice", GetCellText(table.Rows[0], 0));
        Assert.Equal("Bob", GetCellText(table.Rows[1], 0));
        Assert.Equal("Charlie", GetCellText(table.Rows[2], 0));

        // Sort by Name (Column 0) - Descending
        table.Sort(table.Columns[0]);
        Assert.Equal("Charlie", GetCellText(table.Rows[0], 0));
        Assert.Equal("Bob", GetCellText(table.Rows[1], 0));
        Assert.Equal("Alice", GetCellText(table.Rows[2], 0));

        // Sort by Age (Column 1) - String sort "20" < "25" < "30"
        table.Sort(table.Columns[1]);
        Assert.Equal("Bob", GetCellText(table.Rows[0], 0)); // 20
        Assert.Equal("Charlie", GetCellText(table.Rows[1], 0)); // 25
        Assert.Equal("Alice", GetCellText(table.Rows[2], 0)); // 30
    }

    private string GetCellText(TableRow row, int colIndex)
    {
        var cell = (TextBlock)row.Cells[colIndex];
        return cell.Text;
    }

    [Fact]
    public void Table_Pagination_Navigation()
    {
        var table = new Table();
        table.PageSize = 2;
        table.AddRow("1");
        table.AddRow("2");
        table.AddRow("3");
        table.AddRow("4");
        table.AddRow("5");

        // Total 5 items, PageSize 2 -> 3 pages (2, 2, 1)
        table.Measure(new Size(100, 100)); // Layout to update internals

        Assert.Equal(0, table.CurrentPage);
        Assert.Equal(3, table.TotalPages);

        // Check visible rows (Page 1: "1", "2")
        var stack = GetRowStack(table);
        // stack children count depends on lines. Assuming ShowHorizontalLines=false by default in ctor?
        // No, defaults to false.
        Assert.Equal(2, stack.Children.Count);
        Assert.Equal("1", ((TextBlock)((TableRow)stack.Children[0]).Cells[0]).Text);

        // Next Page
        table.CurrentPage = 1;
        table.Measure(new Size(100, 100)); // Re-layout needed to update visible rows

        stack = GetRowStack(table);
        Assert.Equal(2, stack.Children.Count);
        Assert.Equal("3", ((TextBlock)((TableRow)stack.Children[0]).Cells[0]).Text);

        // Last Page
        table.CurrentPage = 2;
        table.Measure(new Size(100, 100));

        stack = GetRowStack(table);
        Assert.Single(stack.Children);
        Assert.Equal("5", ((TextBlock)((TableRow)stack.Children[0]).Cells[0]).Text);
    }

    [Theory]
    [InlineData(1, 10, 30, "< 2 of 10 >")] // Width <= 30 forces status string fallback

    // Case 1: Small width -> "< >"
    [InlineData(1, 10, 5, "< >")]

    // Case 2: Medium width -> Status string "< 2 of 10 >" (if detailed fails or width <= 30)
    [InlineData(1, 10, 20, "< 2 of 10 >")]

    public void Table_Pagination_String_Format(int pageIndex, int totalPages, int width, string expected)
    {
        // Table.GetPaginationString is internal static.
        // pageIndex is 0-based.

        char[] buffer = new char[256];
        Span<char> span = new Span<char>(buffer);

        int len = Table.GetPaginationString(span, width, totalPages, pageIndex);

        string result = span.Slice(0, len).ToString();
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Table_Pagination_String_Detailed()
    {
        // Width > 30 -> Detailed string
        int width = 50;
        int totalPages = 10;
        int pageIndex = 4; // Page 5

        // Expected: "< 1 ... 3 4 [5] 6 7 ... 10 >"
        // Let's rely on exact logic output verification

        char[] buffer = new char[256];
        Span<char> span = new Span<char>(buffer);

        int len = Table.GetPaginationString(span, width, totalPages, pageIndex);
        string result = span.Slice(0, len).ToString();

        Assert.Contains("[5]", result);
        Assert.StartsWith("< 1 ...", result);
        Assert.EndsWith("... 10 >", result);
    }

    [Fact]
    public void MouseClick_NestedTable_SortsColumnsAndSelectsScrolledRowOnly()
    {
        var table = new Table
        {
            Width = 18,
            Height = 7,
            ShowBorder = true,
            ShowHeader = true
        };
        table.Columns.Add(new TableColumn { Header = "Id", Width = new GridLength(4, GridUnitType.Pixel) });
        table.Columns.Add(new TableColumn { Header = "Name", Width = new GridLength(8, GridUnitType.Pixel) });
        table.AddRow("3", "Charlie");
        table.AddRow("1", "Alice");
        table.AddRow("5", "Echo");
        table.AddRow("2", "Bravo");
        table.AddRow("4", "Delta");

        var surfaceContent = new StackPanel();
        surfaceContent.AddChild(new TextBlock { Text = "records" });
        surfaceContent.AddChild(table);
        surfaceContent.AddChild(new TextBlock { Text = "footer surface" });
        var host = new ControlTestHost(new Border { Child = surfaceContent }, 22, 11);

        var nameHeaderClick = host.Click(table, 7, 1);

        Assert.True(nameHeaderClick.Down.Handled);
        Assert.Same(table.Columns[1], table.SortedColumn);
        Assert.Equal("Alice", GetCellText(table.Rows[0], 1));
        Assert.Equal(-1, table.SelectedIndex);

        host.Click(table, 2, 1);

        Assert.Same(table.Columns[0], table.SortedColumn);
        Assert.Equal("1", GetCellText(table.Rows[0], 0));
        Assert.Equal(-1, table.SelectedIndex);

        var bodyScrollViewer = (ScrollViewer)table.GetVisualChild(0);
        bodyScrollViewer.ScrollToVerticalOffset(2);
        var rowClick = host.Click(table, 2, 3);

        Assert.True(rowClick.Down.Handled);
        Assert.True(table.IsFocused);
        Assert.Equal(2, table.SelectedIndex);
        Assert.Equal("3", GetCellText(table.Rows[table.SelectedIndex], 0));

        host.Click(surfaceContent.GetVisualChild(2), 3, 0);
        Assert.Equal(2, table.SelectedIndex);
    }
}
