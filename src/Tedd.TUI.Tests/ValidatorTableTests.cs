using System;

using Tedd.TUI;

using Xunit;

namespace Tedd.TUI.Tests;

public class ValidatorTableTests
{
    [Fact]
    public void Table_CoordinatePreciseCharacterAssertion_HeavyStyle()
    {
        var table = new Table
        {
            ShowBorder = true,
            ShowHeader = true,
            ShowVerticalLines = true,
            ShowHorizontalLines = true,
            BorderStyle = BoxStyle.Heavy,
            Width = 12,
            Height = 8
        };

        var col1 = new TableColumn { Header = "ID", Width = new GridLength(3, GridUnitType.Pixel) };
        var col2 = new TableColumn { Header = "Name", Width = new GridLength(4, GridUnitType.Pixel) };
        table.Columns.Add(col1);
        table.Columns.Add(col2);

        table.AddRow("1", "Bob");
        table.AddRow("2", "Alice");

        // Force disable scrollbars for exact rendering tests without layout anomalies
        // table.ApplyTemplate();
        var sv = table.GetVisualChild(0) as ScrollViewer;
        if (sv != null)
        {
            sv.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        }

        table.Measure(new Size(12, 8));
        table.Arrange(new Rect(0, 0, 12, 8));

        var buffer = new VirtualBuffer(12, 8);
        table.Render(buffer, 0, 0);

        // Heavy Top-Left
        Assert.Equal('\u250F', buffer.GetPixel(0, 0).Character);
        // Heavy Top-Right
        Assert.Equal('\u2513', buffer.GetPixel(11, 0).Character);
        // Heavy Bottom-Left
        Assert.Equal('\u2517', buffer.GetPixel(0, 7).Character);
        // Heavy Bottom-Right
        Assert.Equal('\u251B', buffer.GetPixel(11, 7).Character);

        // Header Junctions
        // Heavy Left T-Junction for Header Separator
        Assert.Equal('\u2523', buffer.GetPixel(0, 2).Character);
        // Heavy Right T-Junction for Header Separator
        Assert.Equal('\u252B', buffer.GetPixel(11, 2).Character);

        // Header inner cross junction (between headers)
        // At X = 1 (Border) + 3 (Col1) = 4
        Assert.Equal('\u254B', buffer.GetPixel(4, 2).Character);

        // Heavy Top T-Junction (TDown) at X=4, Y=0
        Assert.Equal('\u2533', buffer.GetPixel(4, 0).Character);

        // Heavy Bottom T-Junction (TUp) at X=4, Y=7; BoxStyle.Heavy uses U+253B for TUp junctions
        Assert.Equal('\u253B', buffer.GetPixel(4, 7).Character);

        // Header Separator Horizontal Line (Heavy) at X=1..3, Y=2
        Assert.Equal('\u2501', buffer.GetPixel(2, 2).Character);

        // Header vertical line at X=4, Y=1 (Inner Vertical)
        // Note: For heavy border, inner vertical for header might be heavy or light depending on implementation.
        // The implementation uses b.Vertical, which for heavy is \u2503
        Assert.Equal('\u2503', buffer.GetPixel(4, 1).Character);
    }

    [Fact]
    public void Table_HierarchicalCompositionAndDynamicStateMutation()
    {
        var rootGrid = new Grid();
        rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var table = new Table
        {
            ShowBorder = true,
            ShowHeader = true,
            ShowVerticalLines = true,
            BorderStyle = BoxStyle.Heavy
        };
        table.Columns.Add(new TableColumn { Header = "A", Width = new GridLength(1, GridUnitType.Star) });
        table.Columns.Add(new TableColumn { Header = "B", Width = new GridLength(1, GridUnitType.Star) });
        table.AddRow("1", "2");

        Grid.SetColumn(table, 0);

        var rightBorder = new Border { BoxStyle = BoxStyle.Double, VerticalScrollBarVisibility = ScrollBarVisibility.Disabled, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        Grid.SetColumn(rightBorder, 1);

        rootGrid.Children.Add(table);
        rootGrid.Children.Add(rightBorder);

        // Measure and arrange at 20x10
        rootGrid.Measure(new Size(20, 10));
        rootGrid.Arrange(new Rect(0, 0, 20, 10));

        var buffer = new VirtualBuffer(20, 10);
        rootGrid.Render(buffer, 0, 0);

        // Table is in left column (width 10). Border is heavy.
        Assert.Equal('\u250F', buffer.GetPixel(0, 0).Character); // Heavy Top-Left
        Assert.Equal('\u2513', buffer.GetPixel(9, 0).Character); // Heavy Top-Right

        // Right border is in right column (width 10, offset 10). Border is double.
        Assert.Equal('\u2554', buffer.GetPixel(10, 0).Character); // Double Top-Left
        Assert.Equal('\u2557', buffer.GetPixel(19, 0).Character); // Double Top-Right

        // Dynamic State Mutation: Resize to 30x10
        rootGrid.Measure(new Size(30, 10));
        rootGrid.Arrange(new Rect(0, 0, 30, 10));

        var resizedBuffer = new VirtualBuffer(30, 10);
        rootGrid.Render(resizedBuffer, 0, 0);

        // Columns are now 15 wide each.
        // Table at (0,0) to (14,9)
        Assert.Equal('\u250F', resizedBuffer.GetPixel(0, 0).Character);
        Assert.Equal('\u2513', resizedBuffer.GetPixel(14, 0).Character);

        // Right border at (15,0) to (29,9)
        Assert.Equal('\u2554', resizedBuffer.GetPixel(15, 0).Character);
        Assert.Equal('\u2557', resizedBuffer.GetPixel(29, 0).Character);
    }

    [Fact]
    public void Table_BoundaryAndEdgeVerification_ExtremeConstraints()
    {
        var table = new Table
        {
            ShowBorder = true,
            ShowHeader = true,
            ShowVerticalLines = true,
            ShowHorizontalLines = true,
            BorderStyle = BoxStyle.Heavy,
        };
        table.Columns.Add(new TableColumn { Header = "Col", Width = new GridLength(10, GridUnitType.Pixel) });
        table.AddRow("Test");

        // 0x0
        table.Measure(new Size(0, 0));
        table.Arrange(new Rect(0, 0, 0, 0));
        var buffer0 = new VirtualBuffer(10, 10);

        // At 0x0 Table will still attempt to draw border since ShowBorder is true,
        // but actual rendering might be skipped if we wrap in layout or width is 0.
        // Let's see if we can render.
        // Actually table rendering of borders does: buffer.SetPixel(x, y, chars.TL).
        // If x,y is within buffer it sets it even if size is 0.
        // Wait, if w=0, h=0 it draws TL at x,y and TR at x-1, y.
        // Let's clear the buffer after to not fail on garbage, or just not assert 0x0 explicitly for ' ',
        // but just verify it does not throw.
        table.Render(buffer0, 0, 0);
        // The fact it renders without crashing is the main edge case verification.

        // 1x1
        table.Measure(new Size(1, 1));
        table.Arrange(new Rect(0, 0, 1, 1));
        var buffer1 = new VirtualBuffer(10, 10);
        table.Render(buffer1, 0, 0);

        // Table with borders and headers needs at least 2x2. At 1x1, it shouldn't throw,
        // it may draw a partial border, but it should not fail.
        // We just ensure the rendering didn't throw and no extreme overflow happened.
        // We can check buffer is clean outside the 1x1 area
        // Actually, Table draws its header text if the header width accommodates it, or at least attempts it.
        // For a size of 1x1, it might just draw the border character or the first character of the header "C" at 0,0.
        // What we care about in extreme constraint testing is that it does NOT throw exceptions and gracefully clips.
        // We will assert nothing is outside the 1x1 render area.
        for (var y = 0; y < 10; y++)
        {
            for (var x = 0; x < 10; x++)
            {
                // (0,0) is the only cell inside the 1x1 render area when rendering at (0,0).
                if (x == 0 && y == 0)
                    continue;

                Assert.Equal(' ', buffer1.GetPixel(x, y).Character);
            }
        }
    }

    [Fact]
    public void Table_BoundaryAndEdgeVerification_ZeroSizeConstraint()
    {
        var table = new Table
        {
            ShowBorder = true,
            ShowHeader = true,
            BorderStyle = BoxStyle.Double
        };
        table.Columns.Add(new TableColumn { Header = "A" });
        table.AddRow("Test");

        table.Measure(new Size(0, 0));
        table.Arrange(new Rect(0, 0, 0, 0));

        var buffer = new VirtualBuffer(10, 10);
        // Ensure no exception is thrown when rendering an empty layout
        var ex = Record.Exception(() => table.Render(buffer, 0, 0));
        Assert.Null(ex);
    }

    [Fact]
    public void Table_CoordinatePreciseCharacterAssertion_HeavyTUpTDown()
    {
        var table = new Table
        {
            ShowBorder = true,
            ShowHeader = false,
            ShowVerticalLines = true,
            ShowHorizontalLines = true,
            BorderStyle = BoxStyle.Heavy,
            Width = 10,
            Height = 5
        };

        table.Columns.Add(new TableColumn { Width = new GridLength(4, GridUnitType.Pixel) });
        table.Columns.Add(new TableColumn { Width = new GridLength(4, GridUnitType.Pixel) });
        table.AddRow("A", "B");

        var sv = table.GetVisualChild(0) as ScrollViewer;
        if (sv != null)
        {
            sv.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        }

        table.Measure(new Size(10, 5));
        table.Arrange(new Rect(0, 0, 10, 5));

        var buffer = new VirtualBuffer(10, 5);
        table.Render(buffer, 0, 0);

        // Verify TDown (Heavy) at top junction
        Assert.Equal('\u2533', buffer.GetPixel(5, 0).Character);

        // Verify TUp (Heavy uses U+253B) at bottom junction
        Assert.Equal('\u253B', buffer.GetPixel(5, 4).Character);
    }

    [Fact]
    public void Table_CoordinatePreciseCharacterAssertion_SingleStyle()
    {
        var table = new Table
        {
            ShowBorder = true,
            ShowHeader = true,
            ShowVerticalLines = true,
            ShowHorizontalLines = true,
            BorderStyle = BoxStyle.Single,
            Width = 12,
            Height = 8
        };

        var col1 = new TableColumn { Header = "ID", Width = new GridLength(3, GridUnitType.Pixel) };
        var col2 = new TableColumn { Header = "Name", Width = new GridLength(4, GridUnitType.Pixel) };
        table.Columns.Add(col1);
        table.Columns.Add(col2);

        table.AddRow("1", "Bob");
        table.AddRow("2", "Alice");

        var sv = table.GetVisualChild(0) as ScrollViewer;
        if (sv != null)
        {
            sv.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        }

        table.Measure(new Size(12, 8));
        table.Arrange(new Rect(0, 0, 12, 8));

        var buffer = new VirtualBuffer(12, 8);
        table.Render(buffer, 0, 0);

        // Single Top-Left
        Assert.Equal('\u250C', buffer.GetPixel(0, 0).Character);
        // Single Top-Right
        Assert.Equal('\u2510', buffer.GetPixel(11, 0).Character);
        // Single Bottom-Left
        Assert.Equal('\u2514', buffer.GetPixel(0, 7).Character);
        // Single Bottom-Right
        Assert.Equal('\u2518', buffer.GetPixel(11, 7).Character);

        // Header Junctions
        // Single Left T-Junction for Header Separator
        Assert.Equal('\u251C', buffer.GetPixel(0, 2).Character);
        // Single Right T-Junction for Header Separator
        Assert.Equal('\u2524', buffer.GetPixel(11, 2).Character);

        // Header inner cross junction (between headers)
        // At X = 1 (Border) + 3 (Col1) = 4
        Assert.Equal('\u253C', buffer.GetPixel(4, 2).Character);

        // Single Top T-Junction (TDown) at X=4, Y=0
        Assert.Equal('\u252C', buffer.GetPixel(4, 0).Character);

        // Single Bottom T-Junction (TUp) at X=4, Y=7
        Assert.Equal('\u2534', buffer.GetPixel(4, 7).Character);
    }

    [Fact]
    public void Table_CoordinatePreciseCharacterAssertion_HeavyBodySeparation()
    {
        // Architectural Mandate: Nest in a hierarchy and trigger dynamic resize
        var parentGrid = new Grid();
        parentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        parentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var table = new Table
        {
            ShowBorder = true,
            ShowHeader = true,
            ShowVerticalLines = true,
            ShowHorizontalLines = true,
            BorderStyle = BoxStyle.Heavy
        };

        var col1 = new TableColumn { Header = "ID", Width = new GridLength(3, GridUnitType.Pixel) };
        var col2 = new TableColumn { Header = "Name", Width = new GridLength(4, GridUnitType.Pixel) };
        table.Columns.Add(col1);
        table.Columns.Add(col2);

        table.AddRow("1", "Bob");
        table.AddRow("2", "Alice");

        parentGrid.Children.Add(table);

        // Initial layout pass
        parentGrid.Measure(new Size(12, 8));
        parentGrid.Arrange(new Rect(0, 0, 12, 8));

        var sv = table.GetVisualChild(0) as ScrollViewer;
        if (sv != null)
        {
            sv.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        }

        var buffer = new VirtualBuffer(12, 8);
        parentGrid.Render(buffer, 0, 0);

        // Assert coordinates on initial constraint
        Assert.Equal('\u254B', buffer.GetPixel(4, 2).Character);
        Assert.Equal('\u2523', buffer.GetPixel(0, 4).Character);
        Assert.Equal('\u252B', buffer.GetPixel(11, 4).Character);
        Assert.Equal('\u254B', buffer.GetPixel(4, 4).Character);
        Assert.Equal('\u2501', buffer.GetPixel(2, 4).Character);

        // Dynamic Resize state mutation
        parentGrid.Measure(new Size(20, 10));
        parentGrid.Arrange(new Rect(0, 0, 20, 10));

        var bufferResize = new VirtualBuffer(20, 10);
        parentGrid.Render(bufferResize, 0, 0);

        // Table spans full star width, so width is 20 now.
        // Body Sep Right is now at 19. Cross remains at 4 because column widths are fixed pixels.
        Assert.Equal('\u254B', bufferResize.GetPixel(4, 2).Character); // Header cross junction
        Assert.Equal('\u2523', bufferResize.GetPixel(0, 4).Character); // Left junction
        Assert.Equal('\u252B', bufferResize.GetPixel(19, 4).Character); // Right junction moved to 19
        Assert.Equal('\u254B', bufferResize.GetPixel(4, 4).Character); // Cross remains at 4
        Assert.Equal('\u2501', bufferResize.GetPixel(2, 4).Character); // Horizontal remains line
    }

    [Fact]
    public void Table_CoordinatePreciseCharacterAssertion_DoubleStyle()
    {
        var table = new Table
        {
            ShowBorder = true,
            ShowHeader = true,
            ShowVerticalLines = true,
            ShowHorizontalLines = true,
            BorderStyle = BoxStyle.Double,
            Width = 12,
            Height = 8
        };

        var col1 = new TableColumn { Header = "ID", Width = new GridLength(3, GridUnitType.Pixel) };
        var col2 = new TableColumn { Header = "Name", Width = new GridLength(4, GridUnitType.Pixel) };
        table.Columns.Add(col1);
        table.Columns.Add(col2);

        table.AddRow("1", "Bob");
        table.AddRow("2", "Alice");

        // Force disable scrollbars for exact rendering tests without layout anomalies
        var sv = table.GetVisualChild(0) as ScrollViewer;
        if (sv != null)
        {
            sv.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        }

        table.Measure(new Size(12, 8));
        table.Arrange(new Rect(0, 0, 12, 8));

        var buffer = new VirtualBuffer(12, 8);
        table.Render(buffer, 0, 0);

        // Explicit requirements based on Trace Memory
        // TDown
        Assert.Equal('\u2566', buffer.GetPixel(4, 0).Character);
        // TUp
        Assert.Equal('\u2569', buffer.GetPixel(4, 7).Character);
        // TLeft (Header separator left junction)
        Assert.Equal('\u2560', buffer.GetPixel(0, 2).Character);
        // TRight (Header separator right junction)
        Assert.Equal('\u2563', buffer.GetPixel(11, 2).Character);
        // HeaderCross
        Assert.Equal('\u256C', buffer.GetPixel(4, 2).Character);

        // Render separator lines by taking body rows into account.
        // In order to render row separators, table needs horizontal lines enabled and at least 2 rows.
        // Table uses TableSeparator for body separation when ShowHorizontalLines = true.
        // The first row measures h=1. The separator measures h=1.
        // Header is at y=1, Header sep is at y=2. So row 0 is at y=3.
        // Separator is at y=4.
        Assert.Equal('\u255F', buffer.GetPixel(0, 4).Character); // BodySepTLeft
        Assert.Equal('\u2562', buffer.GetPixel(11, 4).Character); // BodySepTRight
    }

    [Fact]
    public void Table_HierarchicalCompositionAndDynamicStateMutation_NestedGrids()
    {
        var parentGrid = new Grid();
        parentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        parentGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var childGrid = new Grid();
        childGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        childGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var table = new Table
        {
            ShowBorder = true,
            ShowHeader = true,
            ShowVerticalLines = true,
            BorderStyle = BoxStyle.Heavy
        };
        table.Columns.Add(new TableColumn { Header = "1", Width = new GridLength(1, GridUnitType.Star) });
        table.AddRow("A");

        Grid.SetColumn(table, 0);
        childGrid.Children.Add(table);

        Grid.SetRow(childGrid, 0);
        parentGrid.Children.Add(childGrid);

        // Initial layout pass
        parentGrid.Measure(new Size(20, 20));
        parentGrid.Arrange(new Rect(0, 0, 20, 20));

        var buffer = new VirtualBuffer(20, 20);
        parentGrid.Render(buffer, 0, 0);

        // Verify table constraints within nested grids
        // Table should be at (0, 0) to (9, 9) because parent grid row is 10 high and child grid col is 10 wide
        Assert.Equal('\u250F', buffer.GetPixel(0, 0).Character); // TL
        Assert.Equal('\u2513', buffer.GetPixel(9, 0).Character); // TR

        // Dynamic State Mutation
        parentGrid.Measure(new Size(40, 30));
        parentGrid.Arrange(new Rect(0, 0, 40, 30));

        var newBuffer = new VirtualBuffer(40, 30);
        parentGrid.Render(newBuffer, 0, 0);

        // Table should now be at (0, 0) to (19, 14)
        Assert.Equal('\u250F', newBuffer.GetPixel(0, 0).Character); // TL
        Assert.Equal('\u2513', newBuffer.GetPixel(19, 0).Character); // TR
        Assert.Equal('\u2517', newBuffer.GetPixel(0, 14).Character); // BL
        Assert.Equal('\u251B', newBuffer.GetPixel(19, 14).Character); // BR
    }
}
