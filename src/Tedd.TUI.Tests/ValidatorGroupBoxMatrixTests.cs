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
            Height = 10,
            Header = "T"
        };

        // Apply Template to get the internal elements configured
        groupBox.ApplyTemplate();

        // We explicitly turn off scrollbars of the internal Border (which inherits from ScrollViewer)
        // because we want deterministic rendering for character checking on boundaries.
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

        // Verify horizontal edges (sample points, mind the header 'T' at x=1 for left alignment)
        // The title "T" is at position 1. So we check position 3.
        Assert.Equal(h, buffer.GetPixel(3, 0).Character);
        Assert.Equal(h, buffer.GetPixel(5, 9).Character);

        // Verify vertical edges (sample middle points)
        Assert.Equal(v, buffer.GetPixel(0, 5).Character);
        Assert.Equal(v, buffer.GetPixel(9, 5).Character);

        // Verify Title character
        Assert.Equal('T', buffer.GetPixel(1, 0).Character);
    }

    [Fact]
    public void HierarchicalCompositionValidation_DynamicStateMutation()
    {
        var rootGrid = new Grid();
        rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var leftGroupBox = new GroupBox { BoxStyle = BoxStyle.Single, Header = "Grp1" };
        Grid.SetColumn(leftGroupBox, 0);
        leftGroupBox.ApplyTemplate();
        ((Border)leftGroupBox.GetVisualChild(0)).VerticalScrollBarVisibility = false;
        ((Border)leftGroupBox.GetVisualChild(0)).HorizontalScrollBarVisibility = false;

        var stackPanel = new StackPanel();
        var txt1 = new TextBlock { Text = "A" };
        var txt2 = new TextBlock { Text = "B" };
        stackPanel.Children.Add(txt1);
        stackPanel.Children.Add(txt2);
        leftGroupBox.Content = stackPanel;

        var rightGroupBox = new GroupBox { BoxStyle = BoxStyle.Double, Header = "Grp2" };
        Grid.SetColumn(rightGroupBox, 1);
        rightGroupBox.ApplyTemplate();
        ((Border)rightGroupBox.GetVisualChild(0)).VerticalScrollBarVisibility = false;
        ((Border)rightGroupBox.GetVisualChild(0)).HorizontalScrollBarVisibility = false;

        rootGrid.Children.Add(leftGroupBox);
        rootGrid.Children.Add(rightGroupBox);

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

        // Content of Left GroupBox: TextBlock "A" at (1,1), "B" at (1,2)
        Assert.Equal('A', buffer.GetPixel(1, 1).Character);
        Assert.Equal('B', buffer.GetPixel(1, 2).Character);

        // Titles
        Assert.Equal('G', buffer.GetPixel(1, 0).Character); // Grp1
        Assert.Equal('G', buffer.GetPixel(11, 0).Character); // Grp2

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
        var groupBox = new GroupBox { BoxStyle = BoxStyle.Single, Header = "T" };
        groupBox.ApplyTemplate();

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
