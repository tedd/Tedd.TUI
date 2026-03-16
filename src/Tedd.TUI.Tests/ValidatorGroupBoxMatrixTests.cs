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
    public void CoordinatePreciseCharacterAssertion_GroupBoxStyles(BoxStyle style, char tl, char tr, char bl, char br, char h, char v)
    {
        var panel = new Canvas();
        var groupBox = new GroupBox
        {
            BoxStyle = style,
            Header = "T"
        };
        panel.Children.Add(groupBox);

        // Explicitly set width and height so Canvas arrangement honors it.
        groupBox.Width = 10;
        groupBox.Height = 10;

        // GroupBox uses Border internally, we need to explicitly disable scrollbars to ensure deterministic geometry
        groupBox.ApplyTemplate();
        var border = (Border)groupBox.GetVisualChild(0);
        border.VerticalScrollBarVisibility = false;
        border.HorizontalScrollBarVisibility = false;

        panel.Measure(new Size(10, 10));
        panel.Arrange(new Rect(0, 0, 10, 10));

        var buffer = new VirtualBuffer(10, 10);
        panel.Render(buffer, 0, 0);

        // Verify Outer Corners
        Assert.Equal(tl, buffer.GetPixel(0, 0).Character);
        Assert.Equal(tr, buffer.GetPixel(9, 0).Character);
        Assert.Equal(bl, buffer.GetPixel(0, 9).Character);
        Assert.Equal(br, buffer.GetPixel(9, 9).Character);

        // Verify Horizontal Edges (sample points avoiding title "T" at x=1)
        Assert.Equal(h, buffer.GetPixel(5, 0).Character);
        Assert.Equal(h, buffer.GetPixel(5, 9).Character);

        // Verify Vertical Edges
        Assert.Equal(v, buffer.GetPixel(0, 5).Character);
        Assert.Equal(v, buffer.GetPixel(9, 5).Character);

        // Verify Title "T" is rendered at top left (x=1, y=0)
        Assert.Equal('T', buffer.GetPixel(1, 0).Character);
    }

    [Fact]
    public void HierarchicalCompositionValidation_DynamicStateMutation()
    {
        var rootGrid = new Grid();
        rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var leftGroupBox = new GroupBox { BoxStyle = BoxStyle.Single, Header = "A" };
        var rightGroupBox = new GroupBox { BoxStyle = BoxStyle.Double, Header = "B" };

        leftGroupBox.ApplyTemplate();
        var leftBorder = (Border)leftGroupBox.GetVisualChild(0);
        leftBorder.VerticalScrollBarVisibility = false;
        leftBorder.HorizontalScrollBarVisibility = false;

        rightGroupBox.ApplyTemplate();
        var rightBorder = (Border)rightGroupBox.GetVisualChild(0);
        rightBorder.VerticalScrollBarVisibility = false;
        rightBorder.HorizontalScrollBarVisibility = false;

        Grid.SetColumn(leftGroupBox, 0);
        Grid.SetColumn(rightGroupBox, 1);

        rootGrid.Children.Add(leftGroupBox);
        rootGrid.Children.Add(rightGroupBox);

        // Measure & Arrange at 20x10. Each GroupBox gets 10x10.
        rootGrid.Measure(new Size(20, 10));
        rootGrid.Arrange(new Rect(0, 0, 20, 10));

        var buffer = new VirtualBuffer(20, 10);
        rootGrid.Render(buffer, 0, 0);

        // Left GroupBox
        Assert.Equal('\u250C', buffer.GetPixel(0, 0).Character); // Left Top-Left Single
        Assert.Equal('\u2510', buffer.GetPixel(9, 0).Character); // Left Top-Right Single
        Assert.Equal('\u2514', buffer.GetPixel(0, 9).Character); // Left Bottom-Left Single
        Assert.Equal('\u2518', buffer.GetPixel(9, 9).Character); // Left Bottom-Right Single
        Assert.Equal('A', buffer.GetPixel(1, 0).Character); // Left Title

        // Right GroupBox
        Assert.Equal('\u2554', buffer.GetPixel(10, 0).Character); // Right Top-Left Double
        Assert.Equal('\u2557', buffer.GetPixel(19, 0).Character); // Right Top-Right Double
        Assert.Equal('\u255A', buffer.GetPixel(10, 9).Character); // Right Bottom-Left Double
        Assert.Equal('\u255D', buffer.GetPixel(19, 9).Character); // Right Bottom-Right Double
        Assert.Equal('B', buffer.GetPixel(11, 0).Character); // Right Title

        // Mutate State: Resize to 30x10
        rootGrid.Measure(new Size(30, 10));
        rootGrid.Arrange(new Rect(0, 0, 30, 10));

        var resizedBuffer = new VirtualBuffer(30, 10);
        rootGrid.Render(resizedBuffer, 0, 0);

        // Left GroupBox (0 to 14)
        Assert.Equal('\u250C', resizedBuffer.GetPixel(0, 0).Character);
        Assert.Equal('\u2510', resizedBuffer.GetPixel(14, 0).Character);
        Assert.Equal('\u2514', resizedBuffer.GetPixel(0, 9).Character);
        Assert.Equal('\u2518', resizedBuffer.GetPixel(14, 9).Character);
        Assert.Equal('A', resizedBuffer.GetPixel(1, 0).Character);

        // Right GroupBox (15 to 29)
        Assert.Equal('\u2554', resizedBuffer.GetPixel(15, 0).Character);
        Assert.Equal('\u2557', resizedBuffer.GetPixel(29, 0).Character);
        Assert.Equal('\u255A', resizedBuffer.GetPixel(15, 9).Character);
        Assert.Equal('\u255D', resizedBuffer.GetPixel(29, 9).Character);
        Assert.Equal('B', resizedBuffer.GetPixel(16, 0).Character);
    }

    [Fact]
    public void BoundaryAndEdgeVerification_ZeroAndExtremeClipping()
    {
        var panel = new Canvas();
        var groupBox = new GroupBox
        {
            BoxStyle = BoxStyle.Heavy,
            Header = "T"
        };
        // Explicitly size it to zero
        groupBox.Width = 0;
        groupBox.Height = 0;
        panel.Children.Add(groupBox);

        groupBox.ApplyTemplate();
        var border = (Border)groupBox.GetVisualChild(0);
        border.VerticalScrollBarVisibility = false;
        border.HorizontalScrollBarVisibility = false;

        // 0x0 validation: ensure Render does not throw
        panel.Measure(new Size(0, 0));
        panel.Arrange(new Rect(0, 0, 0, 0));
        var buffer0 = new VirtualBuffer(5, 5);
        panel.Render(buffer0, 0, 0);
        Assert.Equal(' ', buffer0.GetPixel(0, 0).Character); // Shouldn't render anything

        // 1x1 validation: not enough room for a full border
        panel.Measure(new Size(1, 1));
        panel.Arrange(new Rect(0, 0, 1, 1));
        var buffer1 = new VirtualBuffer(5, 5);
        panel.Render(buffer1, 0, 0);

        // At 1x1, the Border (GroupBox) doesn't render since w < 2 || h < 2
        Assert.Equal(' ', buffer1.GetPixel(0, 0).Character);
    }
}
