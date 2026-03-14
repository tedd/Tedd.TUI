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
            Header = "Tst",
            Width = 10,
            Height = 10,
        };

        groupBox.ApplyTemplate();
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

        // Verify horizontal edges (bottom edge sample)
        Assert.Equal(h, buffer.GetPixel(5, 9).Character);

        // Verify vertical edges (sample middle points)
        Assert.Equal(v, buffer.GetPixel(0, 5).Character);
        Assert.Equal(v, buffer.GetPixel(9, 5).Character);

        // Verify Header text. Header is placed at X=1 on the top border.
        // "Tst" has 3 characters.
        Assert.Equal('T', buffer.GetPixel(1, 0).Character);
        Assert.Equal('s', buffer.GetPixel(2, 0).Character);
        Assert.Equal('t', buffer.GetPixel(3, 0).Character);

        // The top horizontal line should resume after the title (X=4)
        Assert.Equal(h, buffer.GetPixel(4, 0).Character);
        Assert.Equal(h, buffer.GetPixel(8, 0).Character);
    }

    [Fact]
    public void HierarchicalCompositionValidation_LayoutMatrix()
    {
        var rootGrid = new Grid();
        rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        var leftGroup = new GroupBox { BoxStyle = BoxStyle.Single, Header = "A" };
        Grid.SetColumn(leftGroup, 0);

        leftGroup.ApplyTemplate();
        var leftBorder = (Border)leftGroup.GetVisualChild(0);
        leftBorder.VerticalScrollBarVisibility = false;
        leftBorder.HorizontalScrollBarVisibility = false;

        var stackPanel = new StackPanel();
        var txt1 = new TextBlock { Text = "1" };
        var txt2 = new TextBlock { Text = "2" };
        stackPanel.Children.Add(txt1);
        stackPanel.Children.Add(txt2);
        leftGroup.Content = stackPanel;

        var rightGroup = new GroupBox { BoxStyle = BoxStyle.Double, Header = "B" };
        Grid.SetColumn(rightGroup, 1);

        rightGroup.ApplyTemplate();
        var rightBorder = (Border)rightGroup.GetVisualChild(0);
        rightBorder.VerticalScrollBarVisibility = false;
        rightBorder.HorizontalScrollBarVisibility = false;

        rootGrid.Children.Add(leftGroup);
        rootGrid.Children.Add(rightGroup);

        // Measure and arrange at 20x10
        rootGrid.Measure(new Size(20, 10));
        rootGrid.Arrange(new Rect(0, 0, 20, 10));

        var buffer = new VirtualBuffer(20, 10);
        rootGrid.Render(buffer, 0, 0);

        // Grid columns are 10 wide each.
        // Left GroupBox at (0,0) to (9,9)
        Assert.Equal('\u250C', buffer.GetPixel(0, 0).Character); // Left Top-Left Single
        Assert.Equal('\u2510', buffer.GetPixel(9, 0).Character); // Left Top-Right Single
        Assert.Equal('A', buffer.GetPixel(1, 0).Character);      // Title
        Assert.Equal('\u2500', buffer.GetPixel(2, 0).Character); // Line resumes

        // Content of Left GroupBox: TextBlock "1" at (1,1), "2" at (1,2)
        Assert.Equal('1', buffer.GetPixel(1, 1).Character);
        Assert.Equal('2', buffer.GetPixel(1, 2).Character);

        // Right GroupBox at (10,0) to (19,9)
        Assert.Equal('\u2554', buffer.GetPixel(10, 0).Character); // Right Top-Left Double
        Assert.Equal('\u2557', buffer.GetPixel(19, 0).Character); // Right Top-Right Double
        Assert.Equal('B', buffer.GetPixel(11, 0).Character);      // Title
    }

    [Fact]
    public void DynamicStateMutation()
    {
        var rootGrid = new Grid();
        rootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var groupBox = new GroupBox { BoxStyle = BoxStyle.Heavy, Header = "X" };
        Grid.SetColumn(groupBox, 0);
        rootGrid.Children.Add(groupBox);

        groupBox.ApplyTemplate();
        var border = (Border)groupBox.GetVisualChild(0);
        border.VerticalScrollBarVisibility = false;
        border.HorizontalScrollBarVisibility = false;

        // Measure and arrange at 20x10
        rootGrid.Measure(new Size(20, 10));
        rootGrid.Arrange(new Rect(0, 0, 20, 10));

        var buffer = new VirtualBuffer(20, 10);
        rootGrid.Render(buffer, 0, 0);

        // Check top right corner at X=19
        Assert.Equal('\u2513', buffer.GetPixel(19, 0).Character);

        // Dynamic State Mutation: Resize to 30x15
        rootGrid.Measure(new Size(30, 15));
        rootGrid.Arrange(new Rect(0, 0, 30, 15));
        var resizedBuffer = new VirtualBuffer(30, 15);
        rootGrid.Render(resizedBuffer, 0, 0);

        // Check new top right corner at X=29
        Assert.Equal('\u2513', resizedBuffer.GetPixel(29, 0).Character);
        // Check new bottom left corner at Y=14
        Assert.Equal('\u2517', resizedBuffer.GetPixel(0, 14).Character);
        // Check new bottom right corner at X=29, Y=14
        Assert.Equal('\u251B', resizedBuffer.GetPixel(29, 14).Character);
    }

    [Fact]
    public void BoundaryAndEdgeVerification_ZeroSize_SingleSize()
    {
        var groupBox = new GroupBox { BoxStyle = BoxStyle.Single, Header = "Test" };

        // Ensure template applies
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
