using Xunit;
using Tedd.TUI;
using System;
using Tedd.TUI.Tests.TestInfrastructure;

namespace Tedd.TUI.Tests;

public class BorderTests
{
    [Fact]
    public void MouseClick_NestedButtons_RoutesThroughBorderToOnlyClickedButton()
    {
        var first = new Button { Content = "One", Width = 5, Height = 3 };
        var second = new Button { Content = "Two", Width = 5, Height = 3 };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal };
        buttons.AddChild(first);
        buttons.AddChild(new TextBlock { Text = ".." });
        buttons.AddChild(second);

        var border = new Border { Child = buttons, Width = 14, Height = 5 };
        var surface = new StackPanel();
        surface.AddChild(new TextBlock { Text = "outside" });
        surface.AddChild(border);
        var host = new ControlTestHost(surface, 14, 6);
        int firstClicks = 0;
        int secondClicks = 0;
        first.Click += (_, _) => firstClicks++;
        second.Click += (_, _) => secondClicks++;

        host.Click(first, 1, 1);
        Assert.Equal((1, 0), (firstClicks, secondClicks));

        host.Click(second, 1, 1);
        Assert.Equal((1, 1), (firstClicks, secondClicks));

        host.Click(0, 0);
        Assert.Equal((1, 1), (firstClicks, secondClicks));
    }

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

    private sealed class WideDesireChild : UIElement
    {
        public int DesiredWidth { get; init; } = 100;
        public Size LastMeasureSize { get; private set; }
        public Size LastArrangeSize { get; private set; }

        protected override Size MeasureOverride(Size availableSize)
        {
            LastMeasureSize = availableSize;
            return new Size(DesiredWidth, 5);
        }

        protected override void ArrangeOverride(Size finalSize)
        {
            LastArrangeSize = finalSize;
        }
    }

    [Fact]
    public void Border_With_HScrollDisabled_Clamps_Arrange_Width_To_Viewport_Even_When_Child_Is_Wider()
    {
        // Repro for the paragraph-wrap-on-resize regression: a non-wrapping sibling
        // (e.g. CodeDocument with a long line) reports a DesiredSize.Width larger than
        // the viewport. The old Border.ArrangeOverride forwarded that wider width to
        // wrappable children and they re-flowed to it, overflowing the viewport.
        // With HScroll = Disabled, the arrange width must be clamped to the viewport.
        var border = new Border
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        var child = new WideDesireChild { DesiredWidth = 100 };
        border.Content = child;

        border.Measure(new Size(20, 20));
        border.Arrange(new Rect(0, 0, 20, 20));

        // viewport = 20 - 2 (border) = 18. With HScroll = Disabled, arrange must clamp to 18.
        Assert.Equal(18, child.LastArrangeSize.Width);
    }

    [Fact]
    public void Border_With_HScrollAuto_Allows_Arrange_Width_To_Match_Wide_Content()
    {
        // With Auto, the scrollbar is allowed to appear and content is arranged at its
        // natural width so it can be scrolled into view.
        var border = new Border
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        var child = new WideDesireChild { DesiredWidth = 100 };
        border.Content = child;

        border.Measure(new Size(20, 20));
        border.Arrange(new Rect(0, 0, 20, 20));

        Assert.Equal(100, child.LastArrangeSize.Width);
        Assert.True(border.IsHorizontalScrollBarShown);
    }

    [Fact]
    public void Border_With_HScrollAuto_Hides_Bar_When_Content_Fits()
    {
        var border = new Border
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        var child = new WideDesireChild { DesiredWidth = 5 };
        border.Content = child;

        border.Measure(new Size(20, 20));
        border.Arrange(new Rect(0, 0, 20, 20));

        Assert.False(border.IsHorizontalScrollBarShown);
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
        border.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        border.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;

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
        border.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
        var child = new MeasuringChild();
        border.Child = child;

        // Enabled: VScroll=Visible, HScroll=Disabled.
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
        border.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
        border.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;

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

        border.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        border.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;

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
        border.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        border.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;

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

        border.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        border.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;

        border.Measure(new Size(20, 20));
        border.Arrange(new Rect(0, 0, 20, 20));

        Assert.Equal(0, child.RenderSize.X);
        Assert.Equal(0, child.RenderSize.Y);
    }

    [Fact]
    public void Border_None_DoesNotDrawBorderCharacters()
    {
        var border = new Border { BoxStyle = BoxStyle.None, BorderColor = ConsoleColor.Red };
        border.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        border.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;

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
