using System;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class ValidatorGroupBoxMatrixTests
{
    [Theory]
    [InlineData(BoxStyle.Single, '\u250C', '\u2510', '\u2514', '\u2518', '\u2500', '\u2502')]
    [InlineData(BoxStyle.Double, '\u2554', '\u2557', '\u255A', '\u255D', '\u2550', '\u2551')]
    [InlineData(BoxStyle.Heavy, '\u250F', '\u2513', '\u2517', '\u251B', '\u2501', '\u2503')]
    public void CoordinatePreciseCharacterAssertion_BoxStyles(BoxStyle style, char tl, char tr, char bl, char br, char h, char v)
    {
        var groupBox = new GroupBox
        {
            BoxStyle = style,
            Width = 10,
            Height = 10
        };
        groupBox.ApplyTemplate(); // Ensure visual tree is built
        var border = (Border)groupBox.GetVisualChild(0);
        border.VerticalScrollBarVisibility = false;
        border.HorizontalScrollBarVisibility = false;

        groupBox.Measure(new Size(10, 10));
        groupBox.Arrange(new Rect(0, 0, 10, 10));

        var buffer = new VirtualBuffer(10, 10);
        groupBox.Render(buffer, 0, 0);

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

        var gbLeft = new GroupBox { BoxStyle = BoxStyle.Single };
        gbLeft.ApplyTemplate();
        var borderLeft = (Border)gbLeft.GetVisualChild(0);
        borderLeft.VerticalScrollBarVisibility = false;
        borderLeft.HorizontalScrollBarVisibility = false;
        Grid.SetColumn(gbLeft, 0);

        var stackPanel = new StackPanel();
        stackPanel.Children.Add(new TextBlock { Text = "A" });
        stackPanel.Children.Add(new TextBlock { Text = "B" });
        gbLeft.Content = stackPanel;

        var canvasRight = new Canvas();
        Grid.SetColumn(canvasRight, 1);

        var gbRight = new GroupBox { BoxStyle = BoxStyle.Double, Width = 10, Height = 10 };
        gbRight.ApplyTemplate();
        var borderRight = (Border)gbRight.GetVisualChild(0);
        borderRight.VerticalScrollBarVisibility = false;
        borderRight.HorizontalScrollBarVisibility = false;
        Canvas.SetLeft(gbRight, 2);
        Canvas.SetTop(gbRight, 2);
        canvasRight.Children.Add(gbRight);

        rootGrid.Children.Add(gbLeft);
        rootGrid.Children.Add(canvasRight);

        // Layout pass: Measure and arrange at 20x10.
        rootGrid.Measure(new Size(20, 10));
        rootGrid.Arrange(new Rect(0, 0, 20, 10));

        var buffer = new VirtualBuffer(20, 10);
        rootGrid.Render(buffer, 0, 0);

        // Left Grid Column: 0 to 9, Height 10
        Assert.Equal('\u250C', buffer.GetPixel(0, 0).Character); // Left Top-Left Single
        Assert.Equal('\u2510', buffer.GetPixel(9, 0).Character); // Left Top-Right Single
        Assert.Equal('\u2514', buffer.GetPixel(0, 9).Character); // Left Bottom-Left Single
        Assert.Equal('\u2518', buffer.GetPixel(9, 9).Character); // Left Bottom-Right Single

        // Inner Content of left groupbox (shifted by border margin)
        Assert.Equal('A', buffer.GetPixel(1, 1).Character);
        Assert.Equal('B', buffer.GetPixel(1, 2).Character);

        // Right Grid Column: 10 to 19. Canvas offsets by 2,2 => 12,2 for gbRight (10x10)
        // However, the gbRight is 10x10 and placed at Canvas 2,2. It should map into buffer at (12,2) to (21,11).
        // Since buffer is 20x10, the right and bottom edges are clipped.
        Assert.Equal('\u2554', buffer.GetPixel(12, 2).Character); // Double Top-Left

        // Dynamic State Mutation: Resize rootGrid to 30x15
        rootGrid.Measure(new Size(30, 15));
        rootGrid.Arrange(new Rect(0, 0, 30, 15));
        var resizedBuffer = new VirtualBuffer(30, 15);
        rootGrid.Render(resizedBuffer, 0, 0);

        // Left Grid Column: 0 to 14, Height 15
        Assert.Equal('\u250C', resizedBuffer.GetPixel(0, 0).Character);
        Assert.Equal('\u2510', resizedBuffer.GetPixel(14, 0).Character);
        Assert.Equal('\u2514', resizedBuffer.GetPixel(0, 14).Character);
        Assert.Equal('\u2518', resizedBuffer.GetPixel(14, 14).Character);

        // Right Grid Column: 15 to 29. Canvas offset 2,2 => 17,2.
        // gbRight is 10x10 => (17,2) to (26,11).
        // Now it fully fits inside the buffer.
        Assert.Equal('\u2554', resizedBuffer.GetPixel(17, 2).Character); // Double Top-Left
        Assert.Equal('\u2557', resizedBuffer.GetPixel(26, 2).Character); // Double Top-Right
        Assert.Equal('\u255A', resizedBuffer.GetPixel(17, 11).Character); // Double Bottom-Left
        Assert.Equal('\u255D', resizedBuffer.GetPixel(26, 11).Character); // Double Bottom-Right
    }

    [Fact]
    public void BoundaryAndEdgeVerification_ZeroSize_SingleSize()
    {
        var groupBox = new GroupBox { BoxStyle = BoxStyle.Single, Width = 10, Height = 10 };
        groupBox.ApplyTemplate();
        var border = (Border)groupBox.GetVisualChild(0);
        border.VerticalScrollBarVisibility = false;
        border.HorizontalScrollBarVisibility = false;

        // 0x0
        groupBox.Measure(new Size(0, 0));
        groupBox.Arrange(new Rect(0, 0, 0, 0));
        var buffer0 = new VirtualBuffer(10, 10);
        groupBox.Render(buffer0, 0, 0);
        Assert.Equal(' ', buffer0.GetPixel(0, 0).Character); // Shouldn't render anything

        // 1x1
        groupBox.Measure(new Size(1, 1));
        groupBox.Arrange(new Rect(0, 0, 1, 1));
        var buffer1 = new VirtualBuffer(10, 10);
        groupBox.Render(buffer1, 0, 0);
        Assert.Equal(' ', buffer1.GetPixel(0, 0).Character); // Border requires >= 2x2
    }
}
