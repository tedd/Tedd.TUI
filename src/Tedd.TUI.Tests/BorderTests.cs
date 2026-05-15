using Xunit;
using Tedd.TUI;
using System;

namespace Tedd.TUI.Tests;

public class BorderTests
{
    private class MeasuringChild : UIElement
    {
        public Size LastMeasureSize { get; private set; }
        public Size LastArrangeSize { get; private set; }

        protected override Size MeasureOverride(Size availableSize)
        {
            LastMeasureSize = availableSize;
            return new Size(10, 10); // Desired Size
        }

        protected override void ArrangeOverride(Size finalSize)
        {
            LastArrangeSize = finalSize;
        }
    }

    [Fact]
    public void Border_Is_ScrollViewer()
    {
        var border = new Border();
        Assert.IsAssignableFrom<ScrollViewer>(border);
    }

    [Fact]
    public void Border_Child_Alias_Content()
    {
        var border = new Border();
        var child = new MeasuringChild();
        border.Child = child;
        Assert.Same(child, border.Content);

        var child2 = new MeasuringChild();
        border.Content = child2;
        Assert.Same(child2, border.Child);
    }

    [Fact]
    public void Border_Measures_Content_Inside_Border()
    {
        var border = new Border();
        var child = new MeasuringChild();
        border.Child = child;

        // Disable scrolling to test constraints
        border.VerticalScrollBarVisibility = false;
        border.HorizontalScrollBarVisibility = false;

        // Border size 20x20. Border thickness 1 (implicit).
        // Available for child: 20 - 2 = 18.
        border.Measure(new Size(20, 20));

        Assert.Equal(18, child.LastMeasureSize.Width);
        Assert.Equal(18, child.LastMeasureSize.Height);
    }

    [Fact]
    public void Border_Measures_Content_With_Scrolling_Enabled()
    {
        var border = new Border();
        var child = new MeasuringChild();
        border.Child = child;

        // Default: VScroll=True, HScroll=False.
        // Expect: Width constrained to 18. Height infinite.

        border.Measure(new Size(20, 20));

        Assert.Equal(18, child.LastMeasureSize.Width);
        Assert.Equal(int.MaxValue, child.LastMeasureSize.Height);
    }

    [Fact]
    public void Border_Does_Not_Subtract_Scrollbar_Size_From_Content()
    {
        var border = new Border();
        var child = new MeasuringChild();
        border.Child = child;

        // Enable both scrollbars
        border.VerticalScrollBarVisibility = true;
        border.HorizontalScrollBarVisibility = true;

        // If standard ScrollViewer logic applied, width would be 20 - 2 (border) - 1 (vscroll) = 17?
        // But Border ScrollBars are embedded in border.
        // So width should be 20 - 2 = 18.
        // Height should be infinite (scrolling).

        border.Measure(new Size(20, 20));

        // Note: Infinite dimension means "Available", so child receives int.MaxValue.
        // We check the constrained dimension (none here, both infinite).

        // Let's disable scrolling to check strict sizing logic with scrollbars VISIBLE but not infinite?
        // Can't really do that easily with ScrollViewer logic (Visibility=True -> Infinite).
        // But we can check that if HScroll is FALSE, and VScroll is TRUE:
        // Width is 18 (not 17).

        border.HorizontalScrollBarVisibility = false;
        border.VerticalScrollBarVisibility = true;

        border.Measure(new Size(20, 20));

        Assert.Equal(18, child.LastMeasureSize.Width);
    }

    [Fact]
    public void Border_Arranges_Content_Correctly()
    {
        var border = new Border();
        var child = new MeasuringChild();
        border.Child = child;

        border.Measure(new Size(20, 20));
        border.Arrange(new Rect(0, 0, 20, 20));

        // Arrange logic:
        // Child arranged at (0, 0) relative to viewport.
        // Viewport size: 18x18.
        // Child DesiredSize: 10x10.
        // Arrange size: Max(Viewport, Desired) = 18x18.

        Assert.Equal(18, child.LastArrangeSize.Width);
        Assert.Equal(18, child.LastArrangeSize.Height);
    }

    [Fact]
    public void Border_Title_Layout()
    {
        var border = new Border();
        var title = new MeasuringChild(); // 10x10 desired
        border.Title = title;
        border.TitleAlignment = HorizontalAlignment.Left;

        border.Measure(new Size(50, 20));
        border.Arrange(new Rect(0, 0, 50, 20));

        // Title arranged at (1, 0).
        // Check verify by manually inspecting arranged rect if we could exposed it,
        // but MeasuringChild only stores Size.
        // We can check RenderSize on title?

        // We need to access RenderSize or similar.
        // MeasuringChild inherits UIElement. RenderSize is public property?
        // UIElement.RenderSize is public { get; private set; }.

        Assert.Equal(10, title.RenderSize.Width);
        Assert.Equal(1, title.RenderSize.Height); // Height restricted to 1
        Assert.Equal(1, title.RenderSize.X);
        Assert.Equal(0, title.RenderSize.Y);

        // Center Alignment
        border.TitleAlignment = HorizontalAlignment.Center;
        border.Arrange(new Rect(0, 0, 50, 20));

        // Width 50. Title 10. Center: (50 - 10) / 2 = 20.
        Assert.Equal(20, title.RenderSize.X);

        // Right Alignment
        border.TitleAlignment = HorizontalAlignment.Right;
        border.Arrange(new Rect(0, 0, 50, 20));

        // Width 50. Title 10. Right: 50 - 1 - 10 = 39.
        Assert.Equal(39, title.RenderSize.X);
    }

    [Fact]
    public void Border_StatusBar_Layout()
    {
        var border = new Border();
        var status = new MeasuringChild(); // 10x10
        border.StatusBar = status;

        border.Measure(new Size(50, 20));
        border.Arrange(new Rect(0, 0, 50, 20));

        // StatusBar at bottom left.
        // X = 1. Y = 20 - 1 = 19.

        Assert.Equal(1, status.RenderSize.X);
        Assert.Equal(19, status.RenderSize.Y);
        Assert.Equal(10, status.RenderSize.Width);
        Assert.Equal(1, status.RenderSize.Height);
    }

    [Fact]
    public void Border_Positions_Content_At_Offset()
    {
        var border = new Border();
        var child = new MeasuringChild();
        border.Child = child;

        border.Measure(new Size(20, 20));
        border.Arrange(new Rect(0, 0, 20, 20));

        // Expect child at (1, 1) relative to Border
        // This ensures absolute position calculations (traversing RenderSize) work correctly for nested elements.
        Assert.Equal(1, child.RenderSize.X);
        Assert.Equal(1, child.RenderSize.Y);
    }

    // BoxStyle.None tests: zero thickness, no border drawing

    [Fact]
    public void Border_None_HasZeroBorderThickness()
    {
        var border = new Border { BoxStyle = BoxStyle.None };
        var child = new MeasuringChild();
        border.Child = child;

        // Disable scrolling so we can compare strict measure dimensions.
        border.VerticalScrollBarVisibility = false;
        border.HorizontalScrollBarVisibility = false;

        border.Measure(new Size(20, 20));

        // With BoxStyle.None there is no border thickness, so the child receives
        // the full available size (vs 18x18 with a regular border) and the border's
        // desired size equals the child's.
        Assert.Equal(20, child.LastMeasureSize.Width);
        Assert.Equal(20, child.LastMeasureSize.Height);
        Assert.Equal(10, border.DesiredSize.Width);
        Assert.Equal(10, border.DesiredSize.Height);
    }

    [Fact]
    public void Border_None_PositionsContentAtOrigin()
    {
        var border = new Border { BoxStyle = BoxStyle.None };
        var child = new MeasuringChild();
        border.Child = child;

        border.VerticalScrollBarVisibility = false;
        border.HorizontalScrollBarVisibility = false;

        border.Measure(new Size(20, 20));
        border.Arrange(new Rect(0, 0, 20, 20));

        Assert.Equal(0, child.RenderSize.X);
        Assert.Equal(0, child.RenderSize.Y);
    }

    [Fact]
    public void Border_None_DoesNotDrawBorderCharacters()
    {
        var border = new Border { BoxStyle = BoxStyle.None, BorderColor = ConsoleColor.Red };
        border.VerticalScrollBarVisibility = false;
        border.HorizontalScrollBarVisibility = false;

        border.Measure(new Size(5, 3));
        border.Arrange(new Rect(0, 0, 5, 3));

        var buffer = new VirtualBuffer(5, 3);
        buffer.Clear();
        border.Render(buffer, 0, 0);

        // Every cell should be a space; no box-drawing characters anywhere.
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 5; x++)
            {
                Assert.Equal(' ', buffer.GetPixel(x, y).Character);
            }
        }
    }
}
