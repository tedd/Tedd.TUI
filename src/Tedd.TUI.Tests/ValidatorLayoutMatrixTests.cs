using System;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class ValidatorLayoutMatrixTests
{
    [Theory]
    [InlineData(BoxStyle.Single, '\u250C', '\u2510', '\u2514', '\u2518', '\u2500', '\u2502')]
    [InlineData(BoxStyle.Double, '\u2554', '\u2557', '\u255A', '\u255D', '\u2550', '\u2551')]
    [InlineData(BoxStyle.Heavy, '\u250F', '\u2513', '\u2517', '\u251B', '\u2501', '\u2503')]
    public void CoordinatePreciseCharacterAssertion_BoxStyles(BoxStyle style, char tl, char tr, char bl, char br, char h, char v)
    {
        var border = new Border
        {
            BoxStyle = style,
            Width = 10,
            Height = 10,
            VerticalScrollBarVisibility = false,
            HorizontalScrollBarVisibility = false
        };

        border.Measure(new Size(10, 10));
        border.Arrange(new Rect(0, 0, 10, 10));

        var buffer = new VirtualBuffer(10, 10);
        border.Render(buffer, 0, 0);

        // Verify corners
        Assert.Equal(tl, buffer.GetPixel(0, 0).Character);
        Assert.Equal(tr, buffer.GetPixel(9, 0).Character);
        Assert.Equal(bl, buffer.GetPixel(0, 9).Character);
        Assert.Equal(br, buffer.GetPixel(9, 9).Character);

        // Verify horizontal edges (sample middle points)
        Assert.Equal(h, buffer.GetPixel(5, 0).Character);
        Assert.Equal(h, buffer.GetPixel(5, 9).Character);

        // Verify vertical edges (sample middle points)
        Assert.Equal(v, buffer.GetPixel(0, 5).Character);
        Assert.Equal(v, buffer.GetPixel(9, 5).Character);
    }

    [Fact]
    public void HierarchicalCompositionValidation_LayoutMatrix()
    {
        var rootGrid = new Grid();
        rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var leftBorder = new Border { BoxStyle = BoxStyle.Single, VerticalScrollBarVisibility = false, HorizontalScrollBarVisibility = false };
        Grid.SetColumn(leftBorder, 0);

        var stackPanel = new StackPanel();
        var txt1 = new TextBlock { Text = "A" };
        var txt2 = new TextBlock { Text = "B" };
        stackPanel.Children.Add(txt1);
        stackPanel.Children.Add(txt2);
        leftBorder.Content = stackPanel;

        var rightBorder = new Border { BoxStyle = BoxStyle.Double, VerticalScrollBarVisibility = false, HorizontalScrollBarVisibility = false };
        Grid.SetColumn(rightBorder, 1);

        rootGrid.Children.Add(leftBorder);
        rootGrid.Children.Add(rightBorder);

        // Measure and arrange at 20x10
        rootGrid.Measure(new Size(20, 10));
        rootGrid.Arrange(new Rect(0, 0, 20, 10));

        var buffer = new VirtualBuffer(20, 10);
        rootGrid.Render(buffer, 0, 0);

        // Grid columns are 10 wide each.
        // Left border at (0,0) to (9,9)
        Assert.Equal('\u250C', buffer.GetPixel(0, 0).Character); // Left Top-Left Single
        Assert.Equal('\u2510', buffer.GetPixel(9, 0).Character); // Left Top-Right Single

        // Right border at (10,0) to (19,9)
        Assert.Equal('\u2554', buffer.GetPixel(10, 0).Character); // Right Top-Left Double
        Assert.Equal('\u2557', buffer.GetPixel(19, 0).Character); // Right Top-Right Double

        // Content of Left Border: TextBlock "A" at (1,1), "B" at (1,2)
        Assert.Equal('A', buffer.GetPixel(1, 1).Character);
        Assert.Equal('B', buffer.GetPixel(1, 2).Character);

        // Dynamic State Mutation: Resize to 30x10
        rootGrid.Measure(new Size(30, 10));
        rootGrid.Arrange(new Rect(0, 0, 30, 10));
        var resizedBuffer = new VirtualBuffer(30, 10);
        rootGrid.Render(resizedBuffer, 0, 0);

        // Columns are now 15 wide each.
        // Left border at (0,0) to (14,9)
        Assert.Equal('\u250C', resizedBuffer.GetPixel(0, 0).Character);
        Assert.Equal('\u2510', resizedBuffer.GetPixel(14, 0).Character);

        // Right border at (15,0) to (29,9)
        Assert.Equal('\u2554', resizedBuffer.GetPixel(15, 0).Character);
        Assert.Equal('\u2557', resizedBuffer.GetPixel(29, 0).Character);
    }

    [Fact]
    public void BoundaryAndEdgeVerification_ZeroSize_SingleSize()
    {
        var border = new Border { BoxStyle = BoxStyle.Single };

        // 0x0
        border.Measure(new Size(0, 0));
        border.Arrange(new Rect(0, 0, 0, 0));
        var buffer0 = new VirtualBuffer(10, 10);
        border.Render(buffer0, 0, 0);
        Assert.Equal(' ', buffer0.GetPixel(0, 0).Character); // Shouldn't render anything

        // 1x1
        border.Measure(new Size(1, 1));
        border.Arrange(new Rect(0, 0, 1, 1));
        var buffer1 = new VirtualBuffer(10, 10);
        border.Render(buffer1, 0, 0);
        Assert.Equal(' ', buffer1.GetPixel(0, 0).Character); // Border requires >= 2x2
    }

    [Fact]
    public void CoordinatePreciseCharacterAssertion_TableBorders()
    {
        var table = new Table
        {
            ShowBorder = true,
            ShowHeader = true,
            ShowVerticalLines = true,
            ShowHorizontalLines = true,
            BorderStyle = BoxStyle.Heavy
        };

        var col1 = new TableColumn { Header = "A", Width = new GridLength(3, GridUnitType.Pixel) };
        var col2 = new TableColumn { Header = "B", Width = new GridLength(3, GridUnitType.Pixel) };
        table.Columns.Add(col1);
        table.Columns.Add(col2);

        table.AddRow("1", "2");

        // Layout the table within a VirtualBuffer
        // Width = 1(border) + 3(col1) + 1(vline) + 3(col2) + 1(border) = 9
        // Height = 1(top border) + 1(header) + 1(header sep) + 1(row) + 1(bottom border) = 5
        table.Measure(new Size(9, 5));
        table.Arrange(new Rect(0, 0, 9, 5));

        // For heavy style Table BoxChars are:
        // TDown: '\u2533', TUp: '\u253B', TLeft: '\u2523', TRight: '\u252B', HeaderCross: '\u254B'
        // TL: '\u250F', TR: '\u2513', BL: '\u2517', BR: '\u251B'

        // Let's explicitly check TDown, TUp, TLeft, TRight, and HeaderCross characters as required by memory.

        var buffer = new VirtualBuffer(9, 5);

        // We need to apply template/ensure scrollbar is hidden if Table internally uses ScrollViewer.
        // Actually Table dynamically creates ScrollViewer. Let's make sure it doesn't show scrollbars.
        var sv = table.GetVisualChild(0) as ScrollViewer;
        if (sv != null)
        {
            sv.VerticalScrollBarVisibility = false;
            sv.HorizontalScrollBarVisibility = false;
        }

        // Re-measure and arrange to be safe
        table.Measure(new Size(9, 5));
        table.Arrange(new Rect(0, 0, 9, 5));
        table.Render(buffer, 0, 0);

        // Verify Outer Corners
        Assert.Equal('\u250F', buffer.GetPixel(0, 0).Character); // TopLeft Heavy
        Assert.Equal('\u2513', buffer.GetPixel(8, 0).Character); // TopRight Heavy
        Assert.Equal('\u2517', buffer.GetPixel(0, 4).Character); // BottomLeft Heavy
        Assert.Equal('\u251B', buffer.GetPixel(8, 4).Character); // BottomRight Heavy

        // Verify TDown (Top Border Junction)
        // Position: X=4 (1 border + 3 col1)
        Assert.Equal('\u2533', buffer.GetPixel(4, 0).Character);

        // Verify TUp (Bottom Border Junction)
        Assert.Equal('\u253B', buffer.GetPixel(4, 4).Character);

        // Verify TLeft (Left Border Junction at Header Separator)
        // Header is at Y=1, Header Sep is at Y=2
        Assert.Equal('\u2523', buffer.GetPixel(0, 2).Character);

        // Verify TRight (Right Border Junction at Header Separator)
        Assert.Equal('\u252B', buffer.GetPixel(8, 2).Character);

        // Verify HeaderCross (Cross at Header Separator)
        Assert.Equal('\u254B', buffer.GetPixel(4, 2).Character);
    }

    [Fact]
    public void HierarchicalCompositionValidation_TableMatrix()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10, GridUnitType.Pixel) });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10, GridUnitType.Pixel) });

        var table = new Table
        {
            ShowBorder = true,
            ShowHeader = true,
            ShowVerticalLines = true,
            BorderStyle = BoxStyle.Double
        };
        table.Columns.Add(new TableColumn { Header = "X", Width = new GridLength(5, GridUnitType.Pixel) });
        table.AddRow("Y");

        grid.Children.Add(table);

        grid.Measure(new Size(10, 10));
        grid.Arrange(new Rect(0, 0, 10, 10));

        var sv = table.GetVisualChild(0) as ScrollViewer;
        if (sv != null)
        {
            sv.VerticalScrollBarVisibility = false;
            sv.HorizontalScrollBarVisibility = false;
        }

        grid.Measure(new Size(10, 10));
        grid.Arrange(new Rect(0, 0, 10, 10));

        var buffer = new VirtualBuffer(10, 10);
        grid.Render(buffer, 0, 0);

        // For BoxStyle.Double: TL = \u2554, TR = \u2557
        Assert.Equal('\u2554', buffer.GetPixel(0, 0).Character); // Table is at 0,0
        // Width of table is 1 + 5 + 1 = 7.
        // The table actual width inside Grid depends on measure. Table measure returns 7 desired width.
        // Grid default HorizontalAlignment is Stretch? UIElement HorizontalAlignment default is Stretch.
        // Table Arrange is called with finalSize (10, 10). So table width is 10.
        // Let's assert at X=9
        Assert.Equal('\u2557', buffer.GetPixel(9, 0).Character);

        // Dynamic layout mutation: resize grid to 0x0
        grid.Measure(new Size(0, 0));
        grid.Arrange(new Rect(0, 0, 0, 0));

        var zeroBuffer = new VirtualBuffer(10, 10);
        grid.Render(zeroBuffer, 0, 0);

        Assert.Equal(' ', zeroBuffer.GetPixel(0, 0).Character); // Should not render anything
    }
}
