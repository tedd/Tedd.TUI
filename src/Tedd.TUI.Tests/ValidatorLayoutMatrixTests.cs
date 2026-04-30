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
    public void Canvas_HierarchicalCompositionAndDynamicStateMutation()
    {
        var canvas = new Canvas();
        var border = new Border { BoxStyle = BoxStyle.Single, Width = 5, Height = 5 };

        Canvas.SetLeft(border, 2);
        Canvas.SetTop(border, 2);

        canvas.Children.Add(border);

        canvas.Measure(new Size(20, 20));
        canvas.Arrange(new Rect(0, 0, 20, 20));

        var buffer = new VirtualBuffer(20, 20);
        canvas.Render(buffer, 0, 0);

        // Single Top-Left at 2,2
        Assert.Equal('\u250C', buffer.GetPixel(2, 2).Character);
        // Single Bottom-Right at 6,6
        Assert.Equal('\u2518', buffer.GetPixel(6, 6).Character);

        // Dynamic state mutation
        canvas.Measure(new Size(30, 30));
        canvas.Arrange(new Rect(0, 0, 30, 30));
        Canvas.SetLeft(border, 10);
        Canvas.SetTop(border, 10);

        canvas.Measure(new Size(30, 30));
        canvas.Arrange(new Rect(0, 0, 30, 30));

        var buffer2 = new VirtualBuffer(30, 30);
        canvas.Render(buffer2, 0, 0);

        // Single Top-Left at 10,10
        Assert.Equal('\u250C', buffer2.GetPixel(10, 10).Character);
        // Single Bottom-Right at 14,14
        Assert.Equal('\u2518', buffer2.GetPixel(14, 14).Character);
    }

    [Fact]
    public void StackPanel_HierarchicalCompositionAndDynamicStateMutation()
    {
        var stack = new StackPanel { Orientation = Orientation.Vertical };
        // Children are left-aligned so positions are deterministic: stacked top-to-bottom at x=0.
        var border1 = new Border { BoxStyle = BoxStyle.Single, Width = 10, Height = 5, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var border2 = new Border { BoxStyle = BoxStyle.Double, Width = 10, Height = 5, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };

        stack.Children.Add(border1);
        stack.Children.Add(border2);

        stack.Measure(new Size(20, 20));
        stack.Arrange(new Rect(0, 0, 20, 20));

        var buffer = new VirtualBuffer(20, 20);
        stack.Render(buffer, 0, 0);
        // border1 occupies rows 0-4 at x=0..9
        Assert.Equal('\u250C', buffer.GetPixel(0, 0).Character);
        Assert.Equal('\u2518', buffer.GetPixel(9, 4).Character);

        // border2 stacked below border1, occupies rows 5-9 at x=0..9
        Assert.Equal('\u2554', buffer.GetPixel(0, 5).Character);
        Assert.Equal('\u255D', buffer.GetPixel(9, 9).Character);

        // Dynamic mutation: Change to Horizontal
        stack.Orientation = Orientation.Horizontal;
        stack.Measure(new Size(20, 20));
        stack.Arrange(new Rect(0, 0, 20, 20));

        var buffer2 = new VirtualBuffer(20, 20);
        stack.Render(buffer2, 0, 0);

        // border1 at 0,0 to 9,4
        Assert.Equal('\u250C', buffer2.GetPixel(0, 0).Character);
        Assert.Equal('\u2518', buffer2.GetPixel(9, 4).Character);

        // border2 at 10,0 to 19,4
        Assert.Equal('\u2554', buffer2.GetPixel(10, 0).Character);
        Assert.Equal('\u255D', buffer2.GetPixel(19, 4).Character);
    }

    [Fact]
    public void DockPanel_HierarchicalCompositionAndDynamicStateMutation()
    {
        var dock = new DockPanel();
        var topBorder = new Border { BoxStyle = BoxStyle.Single, Height = 5 };
        var leftBorder = new Border { BoxStyle = BoxStyle.Double, Width = 5 };
        var centerBorder = new Border { BoxStyle = BoxStyle.Heavy };

        DockPanel.SetDock(topBorder, Dock.Top);
        DockPanel.SetDock(leftBorder, Dock.Left);

        dock.Children.Add(topBorder);
        dock.Children.Add(leftBorder);
        dock.Children.Add(centerBorder);

        dock.Measure(new Size(20, 20));
        dock.Arrange(new Rect(0, 0, 20, 20));

        var buffer = new VirtualBuffer(20, 20);
        dock.Render(buffer, 0, 0);

        // topBorder at 0,0 to 19,4 (Height 5, width matches dock width)
        Assert.Equal('\u250C', buffer.GetPixel(0, 0).Character); // Top-Left Single
        Assert.Equal('\u2510', buffer.GetPixel(19, 0).Character); // Top-Right Single
        Assert.Equal('\u2514', buffer.GetPixel(0, 4).Character); // Bottom-Left Single

        // leftBorder at 0,5 to 4,19 (Width 5, height is remaining 15)
        Assert.Equal('\u2554', buffer.GetPixel(0, 5).Character); // Top-Left Double
        Assert.Equal('\u255A', buffer.GetPixel(0, 19).Character); // Bottom-Left Double
        Assert.Equal('\u255D', buffer.GetPixel(4, 19).Character); // Bottom-Right Double

        // centerBorder at 5,5 to 19,19 (Width 15, Height 15)
        Assert.Equal('\u250F', buffer.GetPixel(5, 5).Character); // Top-Left Heavy
        Assert.Equal('\u251B', buffer.GetPixel(19, 19).Character); // Bottom-Right Heavy

        // Dynamic mutation: resize to 30x30
        dock.Measure(new Size(30, 30));
        dock.Arrange(new Rect(0, 0, 30, 30));

        var buffer2 = new VirtualBuffer(30, 30);
        dock.Render(buffer2, 0, 0);

        // centerBorder at 5,5 to 29,29
        Assert.Equal('\u251B', buffer2.GetPixel(29, 29).Character); // Bottom-Right Heavy
    }

    [Fact]
    public void WrapPanel_HierarchicalCompositionAndDynamicStateMutation()
    {
        var wrap = new WrapPanel();
        var border1 = new Border { BoxStyle = BoxStyle.Single, Width = 10, Height = 10 };
        var border2 = new Border { BoxStyle = BoxStyle.Double, Width = 10, Height = 10 };
        var border3 = new Border { BoxStyle = BoxStyle.Heavy, Width = 10, Height = 10 };

        wrap.Children.Add(border1);
        wrap.Children.Add(border2);
        wrap.Children.Add(border3);

        // Layout: 25 width means it can fit 2 items on first row, 1 item on second row
        wrap.Measure(new Size(25, 30));
        wrap.Arrange(new Rect(0, 0, 25, 30));

        var buffer = new VirtualBuffer(25, 30);
        wrap.Render(buffer, 0, 0);

        // border1 at 0,0 to 9,9
        Assert.Equal('\u250C', buffer.GetPixel(0, 0).Character); // Top-Left Single
        Assert.Equal('\u2518', buffer.GetPixel(9, 9).Character); // Bottom-Right Single

        // border2 at 10,0 to 19,9
        Assert.Equal('\u2554', buffer.GetPixel(10, 0).Character); // Top-Left Double
        Assert.Equal('\u255D', buffer.GetPixel(19, 9).Character); // Bottom-Right Double

        // border3 at 0,10 to 9,19
        Assert.Equal('\u250F', buffer.GetPixel(0, 10).Character); // Top-Left Heavy
        Assert.Equal('\u251B', buffer.GetPixel(9, 19).Character); // Bottom-Right Heavy

        // Dynamic mutation: expand width to fit all on one row
        wrap.Measure(new Size(40, 30));
        wrap.Arrange(new Rect(0, 0, 40, 30));

        var buffer2 = new VirtualBuffer(40, 30);
        wrap.Render(buffer2, 0, 0);

        // border3 should now be at 20,0 to 29,9
        Assert.Equal('\u250F', buffer2.GetPixel(20, 0).Character); // Top-Left Heavy
        Assert.Equal('\u251B', buffer2.GetPixel(29, 9).Character); // Bottom-Right Heavy
    }

    [Fact]
    public void UniformGrid_HierarchicalCompositionAndDynamicStateMutation()
    {
        var grid = new UniformGrid { Rows = 2, Columns = 2 };
        var border1 = new Border { BoxStyle = BoxStyle.Single };
        var border2 = new Border { BoxStyle = BoxStyle.Double };
        var border3 = new Border { BoxStyle = BoxStyle.Heavy };
        var border4 = new Border { BoxStyle = BoxStyle.Single };

        grid.Children.Add(border1);
        grid.Children.Add(border2);
        grid.Children.Add(border3);
        grid.Children.Add(border4);

        grid.Measure(new Size(20, 20));
        grid.Arrange(new Rect(0, 0, 20, 20));

        var buffer = new VirtualBuffer(20, 20);
        grid.Render(buffer, 0, 0);

        // Grid cells are 10x10 each
        // border1 at 0,0 to 9,9
        Assert.Equal('\u250C', buffer.GetPixel(0, 0).Character);
        Assert.Equal('\u2518', buffer.GetPixel(9, 9).Character);

        // border2 at 10,0 to 19,9
        Assert.Equal('\u2554', buffer.GetPixel(10, 0).Character);
        Assert.Equal('\u255D', buffer.GetPixel(19, 9).Character);

        // border3 at 0,10 to 9,19
        Assert.Equal('\u250F', buffer.GetPixel(0, 10).Character);
        Assert.Equal('\u251B', buffer.GetPixel(9, 19).Character);

        // border4 at 10,10 to 19,19
        Assert.Equal('\u250C', buffer.GetPixel(10, 10).Character);
        Assert.Equal('\u2518', buffer.GetPixel(19, 19).Character);

        // Dynamic mutation: resize to 30x30
        grid.Measure(new Size(30, 30));
        grid.Arrange(new Rect(0, 0, 30, 30));

        var buffer2 = new VirtualBuffer(30, 30);
        grid.Render(buffer2, 0, 0);

        // Cells are now 15x15
        // border4 at 15,15 to 29,29
        Assert.Equal('\u250C', buffer2.GetPixel(15, 15).Character);
        Assert.Equal('\u2518', buffer2.GetPixel(29, 29).Character);
    }

    [Fact]
    public void BoundaryAndEdgeVerification_NegativeConstraints()
    {
        var stack = new StackPanel();
        var border = new Border { BoxStyle = BoxStyle.Single };
        stack.Children.Add(border);

        // Negative size should be handled gracefully without exception
        // The Measure/Arrange algorithms typically clamp sizes to 0
        var ex = Record.Exception(() => {
            stack.Measure(new Size(-10, -10));
            stack.Arrange(new Rect(0, 0, -10, -10));
        });

        Assert.Null(ex); // Layout should not crash on negative dimensions

        var buffer = new VirtualBuffer(10, 10);
        var ex2 = Record.Exception(() => {
            stack.Render(buffer, 0, 0);
        });

        Assert.Null(ex2); // Render should not crash after negative dimensions
        Assert.Equal(' ', buffer.GetPixel(0, 0).Character); // Should not render
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
}
