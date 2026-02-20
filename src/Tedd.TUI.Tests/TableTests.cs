using System;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;
using System.Collections.Generic;
using System.Linq;

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
}
