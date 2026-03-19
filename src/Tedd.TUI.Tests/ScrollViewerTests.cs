using Xunit;
using Tedd.TUI;
using System;

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

    [Fact]
    public void ScrollViewer_Constraints_Child_When_Scroll_Disabled()
    {
        var sv = new ScrollViewer();
        var child = new MeasuringChild();
        sv.Content = child;

        // Disable Horizontal Scroll (should constrain width)
        sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
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

        sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
        sv.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;

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
}
