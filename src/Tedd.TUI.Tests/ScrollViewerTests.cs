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
        sv.HorizontalScrollBarVisibility = false;
        // Enable Vertical Scroll (should unconstrain height)
        sv.VerticalScrollBarVisibility = true;

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

        sv.HorizontalScrollBarVisibility = false;
        sv.VerticalScrollBarVisibility = false;

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

        sv.HorizontalScrollBarVisibility = true;
        sv.VerticalScrollBarVisibility = true;

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

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(-5)]
    public void ScrollViewer_HorizontalOffset_ReturnsExpectedValue(int scrollValue)
    {
        var sv = new ScrollViewer();
        sv.HorizontalScrollBarVisibility = true;
        // Mock large content to ensure scroll value isn't clamped to 0
        sv.Content = new TextBlock { Text = new string('A', 100) };
        sv.Measure(new Size(50, 50));
        sv.Arrange(new Rect(0, 0, 50, 50));

        sv.ScrollToHorizontalOffset(scrollValue);
        // ScrollBar clamping logic means negative goes to 0, values over max go to max.
        // We know max here is approx 50.
        int expected = scrollValue < 0 ? 0 : scrollValue;
        Assert.Equal(expected, sv.HorizontalOffset);
    }

    [Fact]
    public void ScrollViewer_ScrollToHorizontalOffset_SetsHorizontalOffset()
    {
        var sv = new ScrollViewer();
        sv.HorizontalScrollBarVisibility = true; // Ensure scrollbar handles value
        sv.Content = new TextBlock { Text = new string('A', 100) }; // Provide content so maximum > 0
        sv.Measure(new Size(50, 50));
        sv.Arrange(new Rect(0, 0, 50, 50));

        sv.ScrollToHorizontalOffset(10);
        Assert.Equal(10, sv.HorizontalOffset);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    public void ScrollViewer_GetVisualChild_ThrowsArgumentOutOfRangeException_ForInvalidIndex(int index)
    {
        var sv = new ScrollViewer
        {
            Content = new TextBlock { Text = "Test" },
            HorizontalScrollBarVisibility = true,
            VerticalScrollBarVisibility = true
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => sv.GetVisualChild(index));
    }

    [Fact]
    public void ScrollViewer_OnMouseDown_HandlesBaseCall()
    {
        var sv = new ScrollViewer();
        sv.Measure(new Size(50, 50));
        sv.Arrange(new Rect(0, 0, 50, 50));

        var mouseEventArgs = new MouseEventArgs(UIElement.MouseDownEvent)
        {
            X = 5,
            Y = 5,
            GlobalX = 5,
            GlobalY = 5
        };

        // Ensure OnMouseDown doesn't throw and base method executes without issue
        var ex = Record.Exception(() => sv.RaiseEvent(mouseEventArgs));
        Assert.Null(ex);
    }
}
