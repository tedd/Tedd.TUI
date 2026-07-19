using Xunit;
using Tedd.TUI;
using System;
using Tedd.TUI.Tests.TestInfrastructure;

namespace Tedd.TUI.Tests;

public class ScrollViewerTests
{
    private class MeasuringChild : UIElement
    {
        public Size LastMeasureSize { get; private set; }

        protected override Size MeasureOverride(Size availableSize)
        {
            LastMeasureSize = availableSize;
            return new Size(10, 10);
        }
    }

    private sealed class WideDesireChild : UIElement
    {
        public int DesiredWidth { get; init; } = 100;
        public int DesiredHeight { get; init; } = 5;
        public Size LastArrangeSize { get; private set; }

        protected override Size MeasureOverride(Size availableSize) => new Size(DesiredWidth, DesiredHeight);

        protected override void ArrangeOverride(Size finalSize)
        {
            LastArrangeSize = finalSize;
        }
    }

    [Fact]
    public void ScrollViewer_Auto_HScroll_Shows_When_Content_Overflows_Width()
    {
        var sv = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        var child = new WideDesireChild { DesiredWidth = 100, DesiredHeight = 5 };
        sv.Content = child;

        sv.Measure(new Size(50, 50));

        Assert.True(sv.IsHorizontalScrollBarShown);
        Assert.False(sv.IsVerticalScrollBarShown);
    }

    [Fact]
    public void ScrollViewer_Auto_HScroll_Hides_When_Content_Fits()
    {
        var sv = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        var child = new WideDesireChild { DesiredWidth = 10, DesiredHeight = 5 };
        sv.Content = child;

        sv.Measure(new Size(50, 50));

        Assert.False(sv.IsHorizontalScrollBarShown);
    }

    [Fact]
    public void ScrollViewer_With_HScrollDisabled_Clamps_Arrange_Width_To_Viewport()
    {
        // Mirror of the BorderTests regression case: Disabled axis must clamp the arrange
        // width even when the content reports a larger DesiredSize.Width.
        var sv = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        var child = new WideDesireChild { DesiredWidth = 100, DesiredHeight = 5 };
        sv.Content = child;

        sv.Measure(new Size(20, 20));
        sv.Arrange(new Rect(0, 0, 20, 20));

        Assert.Equal(20, child.LastArrangeSize.Width);
    }

    [Fact]
    public void ScrollViewer_Constraints_Child_When_Scroll_Disabled()
    {
        var sv = new ScrollViewer();
        var child = new MeasuringChild();
        sv.Content = child;

        // Disable Horizontal Scroll (should constrain width)
        sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        // Enable Vertical Scroll (should unconstrain height)
        sv.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;

        // Measure ScrollViewer with fixed size
        sv.Measure(new Size(50, 50));

        // Verify child received constrained width and infinite height
        // Note: ScrollViewer subtracts scrollbar width (1) from available width if Vertical scroll is visible.
        // So available width for content is 50 - 1 = 49.
        Assert.Equal(49, child.LastMeasureSize.Width);
        Assert.Equal(int.MaxValue, child.LastMeasureSize.Height);
    }

    [Fact]
    public void ScrollViewer_Constraints_Child_When_Both_Scroll_Disabled()
    {
        var sv = new ScrollViewer();
        var child = new MeasuringChild();
        sv.Content = child;

        sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
        sv.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;

        sv.Measure(new Size(50, 50));

        // No scrollbars visible, so full width/height available but constrained
        Assert.Equal(50, child.LastMeasureSize.Width);
        Assert.Equal(50, child.LastMeasureSize.Height);
    }

    [Fact]
    public void ScrollViewer_Unconstraints_Child_When_Both_Scroll_Enabled()
    {
        var sv = new ScrollViewer();
        var child = new MeasuringChild();
        sv.Content = child;

        sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
        sv.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;

        sv.Measure(new Size(50, 50));

        // Both enabled -> Infinite space in both directions
        // But scrollbars take space?
        // ScrollViewer implementation:
        // if (Vertical) contentAvailable.Height = int.MaxValue;
        // if (Horizontal) contentAvailable.Width = int.MaxValue;
        // Then subtract scrollbars.
        // int.MaxValue - 1 is still basically int.MaxValue (or close enough for layout logic usually).
        // Let's see what the implementation does exactly.
        // Math.Max(0, contentAvailable.Width - vScrollWidth);

        // If width is int.MaxValue, subtracting 1 is fine.

        // Let's assert > 50 to be safe, or check exact value if we care.
        Assert.True(child.LastMeasureSize.Width > 1000);
        Assert.True(child.LastMeasureSize.Height > 1000);
        // Specifically, it should be effectively infinite (int.MaxValue or int.MaxValue - scrollbarWidth)
        Assert.True(child.LastMeasureSize.Width > int.MaxValue - 100);
    }

    [Fact]
    public void MouseClick_NestedScrollViewer_ScrollsAndActivatesOnlyVisibleButton()
    {
        var top = new Button { Content = "Top", Width = 8 };
        var bottom = new Button { Content = "Bottom", Width = 8 };
        var topClicks = 0;
        var bottomClicks = 0;
        top.Click += (_, _) => topClicks++;
        bottom.Click += (_, _) => bottomClicks++;
        var content = new StackPanel();
        content.AddChild(top);
        content.AddChild(new TextBlock { Text = "row 1" });
        content.AddChild(new TextBlock { Text = "row 2" });
        content.AddChild(new TextBlock { Text = "row 3" });
        content.AddChild(new TextBlock { Text = "row 4" });
        content.AddChild(bottom);
        var scrollViewer = new ScrollViewer
        {
            Content = content,
            Width = 12,
            Height = 5,
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        var panel = new StackPanel();
        panel.AddChild(new TextBlock { Text = "scroll area" });
        panel.AddChild(scrollViewer);
        panel.AddChild(new TextBlock { Text = "status surface" });
        var host = new ControlTestHost(new Border { Child = panel }, 16, 9);

        var topClick = host.Click(top, 2, 1);

        Assert.True(topClick.Down.Handled);
        Assert.Equal(1, topClicks);
        Assert.Equal(0, bottomClicks);
        Assert.True(top.IsFocused);

        for (var i = 0; i < 5; i++)
        {
            host.Click(scrollViewer, scrollViewer.RenderSize.Width - 1, scrollViewer.RenderSize.Height - 1);
        }

        Assert.Equal(5, scrollViewer.VerticalOffset);
        Assert.Equal(1, topClicks);
        Assert.Equal(0, bottomClicks);

        var bottomClick = host.Click(bottom, 2, 1);

        Assert.True(bottomClick.Down.Handled);
        Assert.Equal(1, topClicks);
        Assert.Equal(1, bottomClicks);
        Assert.False(top.IsFocused);
        Assert.True(bottom.IsFocused);
    }
}
