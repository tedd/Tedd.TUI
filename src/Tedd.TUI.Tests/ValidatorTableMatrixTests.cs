using System;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class ValidatorTableMatrixTests
{
    [Theory]
    [InlineData(BoxStyle.Single, '\u250C', '\u2510', '\u2514', '\u2518', '\u2500', '\u2502', '\u252C', '\u2534', '\u251C', '\u2524', '\u253C')]
    [InlineData(BoxStyle.Double, '\u2554', '\u2557', '\u255A', '\u255D', '\u2550', '\u2551', '\u2566', '\u2569', '\u2560', '\u2563', '\u256C')]
    [InlineData(BoxStyle.Heavy, '\u250F', '\u2513', '\u2517', '\u251B', '\u2501', '\u2503', '\u2533', '\u2537', '\u2523', '\u252B', '\u254B')]
    public void CoordinatePreciseCharacterAssertion_TableBoxStyles(BoxStyle style, char tl, char tr, char bl, char br, char h, char v, char tDown, char tUp, char tLeft, char tRight, char cross)
    {
        var table = new Table
        {
            BorderStyle = style,
            ShowBorder = true,
            ShowHeader = true,
            ShowHorizontalLines = true,
            ShowVerticalLines = true
        };

        table.Columns.Add(new TableColumn { Header = "A", Width = new GridLength(4, GridUnitType.Pixel) });
        table.Columns.Add(new TableColumn { Header = "B", Width = new GridLength(4, GridUnitType.Pixel) });
        table.AddRow("1", "2");
        table.AddRow("3", "4");

        // Layout dimensions:
        // Width = Border(1) + Col1(4) + VLine(1) + Col2(4) + Border(1) = 11
        // Height = BorderTop(1) + Header(1) + HeaderSep(1) + Row1(1) + BodySep(1) + Row2(1) + BorderBottom(1) = 7

        table.Measure(new Size(11, 7));
        table.Arrange(new Rect(0, 0, 11, 7));

        var buffer = new VirtualBuffer(11, 7);
        table.Render(buffer, 0, 0);

        // Verify Outer Corners
        Assert.Equal(tl, buffer.GetPixel(0, 0).Character);
        Assert.Equal(tr, buffer.GetPixel(10, 0).Character);
        Assert.Equal(bl, buffer.GetPixel(0, 6).Character);
        Assert.Equal(br, buffer.GetPixel(10, 6).Character);

        // Verify Outer Edges
        Assert.Equal(h, buffer.GetPixel(2, 0).Character);
        Assert.Equal(h, buffer.GetPixel(2, 6).Character);
        Assert.Equal(v, buffer.GetPixel(0, 1).Character);
        Assert.Equal(v, buffer.GetPixel(10, 1).Character);

        // Verify Top/Bottom Junctions (TDown / TUp)
        Assert.Equal(tDown, buffer.GetPixel(5, 0).Character);
        Assert.Equal(tUp, buffer.GetPixel(5, 6).Character);

        // Verify Header Separator Junctions
        Assert.Equal(tLeft, buffer.GetPixel(0, 2).Character);
        Assert.Equal(tRight, buffer.GetPixel(10, 2).Character);
        Assert.Equal(cross, buffer.GetPixel(5, 2).Character);

        // Verify Body Separator Junctions (using specific single-intersection chars for inner)
        char bodySepTLeft = style == BoxStyle.Double ? '\u255F' : (style == BoxStyle.Heavy ? '\u2520' : '\u251C');
        char bodySepTRight = style == BoxStyle.Double ? '\u2562' : (style == BoxStyle.Heavy ? '\u2528' : '\u2524');
        Assert.Equal(bodySepTLeft, buffer.GetPixel(0, 4).Character);
        Assert.Equal(bodySepTRight, buffer.GetPixel(10, 4).Character);
        Assert.Equal('\u253C', buffer.GetPixel(5, 4).Character); // Inner cross is always standard Light cross \u253C per TableSeparator impl
    }

    [Fact]
    public void HierarchicalCompositionValidation_DynamicStateMutation()
    {
        var rootGrid = new Grid();
        rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var table = new Table
        {
            BorderStyle = BoxStyle.Single,
            ShowBorder = true,
            ShowHeader = true,
            ShowHorizontalLines = true,
            ShowVerticalLines = true
        };
        table.Columns.Add(new TableColumn { Header = "A", Width = new GridLength(1, GridUnitType.Star) });
        table.Columns.Add(new TableColumn { Header = "B", Width = new GridLength(1, GridUnitType.Star) });
        table.AddRow("R1", "R2");
        table.AddRow("R3", "R4");

        Grid.SetColumn(table, 0);
        Grid.SetRow(table, 0);
        rootGrid.Children.Add(table);

        var rightPanel = new Canvas();
        Grid.SetColumn(rightPanel, 1);
        rootGrid.Children.Add(rightPanel);

        // Measure & Arrange at 20x10. Table gets 10x10.
        rootGrid.Measure(new Size(20, 10));
        rootGrid.Arrange(new Rect(0, 0, 20, 10));

        var buffer = new VirtualBuffer(20, 10);
        rootGrid.Render(buffer, 0, 0);

        // Verify Table boundaries within 10x10 area
        // Top-Left corner at (0,0)
        Assert.Equal('\u250C', buffer.GetPixel(0, 0).Character);
        // Top-Right corner at (9,0)
        Assert.Equal('\u2510', buffer.GetPixel(9, 0).Character);
        // Bottom-Left corner at (0,9)
        Assert.Equal('\u2514', buffer.GetPixel(0, 9).Character);
        // Bottom-Right corner at (9,9)
        Assert.Equal('\u2518', buffer.GetPixel(9, 9).Character);

        // Mutate State: Resize to 30x15
        rootGrid.Measure(new Size(30, 15));
        rootGrid.Arrange(new Rect(0, 0, 30, 15));

        var resizedBuffer = new VirtualBuffer(30, 15);
        rootGrid.Render(resizedBuffer, 0, 0);

        // Table should now occupy 15x15 area
        Assert.Equal('\u250C', resizedBuffer.GetPixel(0, 0).Character);
        Assert.Equal('\u2510', resizedBuffer.GetPixel(14, 0).Character);
        Assert.Equal('\u2514', resizedBuffer.GetPixel(0, 14).Character);
        Assert.Equal('\u2518', resizedBuffer.GetPixel(14, 14).Character);

        // Verify content within resized bounds
        Assert.Equal('A', resizedBuffer.GetPixel(1, 1).Character);
    }

    [Fact]
    public void BoundaryAndEdgeVerification_ZeroAndExtremeClipping()
    {
        var table = new Table
        {
            BorderStyle = BoxStyle.Heavy,
            ShowBorder = true,
            ShowHeader = true,
            ShowHorizontalLines = true,
            ShowVerticalLines = true
        };
        table.Columns.Add(new TableColumn { Header = "A" });
        table.Columns.Add(new TableColumn { Header = "B" });
        table.AddRow("Data1", "Data2");

        // 0x0 validation
        table.Measure(new Size(0, 0));
        table.Arrange(new Rect(0, 0, 0, 0));
        var buffer0 = new VirtualBuffer(5, 5);
        table.Render(buffer0, 0, 0);
        // Table renders a top-left corner even when bounds are 0x0 without crashing
        Assert.Equal('\u250F', buffer0.GetPixel(0, 0).Character);

        // Extreme clipping: 2x2 validation (not enough room for full borders/headers)
        table.Measure(new Size(2, 2));
        table.Arrange(new Rect(0, 0, 2, 2));
        var buffer1 = new VirtualBuffer(5, 5);
        table.Render(buffer1, 0, 0);

        // Assert partial rendering. Since table tries to draw vertical separators, at X=1 it might draw a top-down T-junction (┳) instead of a top-right corner (┓) due to lack of space
        Assert.Equal('\u250F', buffer1.GetPixel(0, 0).Character); // ┏
        Assert.Equal('\u2533', buffer1.GetPixel(1, 0).Character); // ┳
    }
}
