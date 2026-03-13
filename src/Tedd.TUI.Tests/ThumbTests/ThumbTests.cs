using System;
using System.Reflection;
using Tedd.TUI;
using Xunit;

namespace Tedd.TUI.Tests.ThumbTests;

public class ThumbTests
{
    [Fact]
    public void DragStarted_FiredOnMouseDown()
    {
        var thumb = new Thumb();
        bool fired = false;

        thumb.DragStarted += (s, e) =>
        {
            fired = true;
            Assert.Equal(10, e.HorizontalOffset);
            Assert.Equal(20, e.VerticalOffset);
        };

        var args = new MouseEventArgs(UIElement.PreviewMouseDownEvent, thumb)
        {
            GlobalX = 10,
            GlobalY = 20,
            X = 5,
            Y = 5
        };

        // Emulate routed event invocation bypassing dispatch for test
        thumb.RaiseEvent(new MouseEventArgs(UIElement.MouseDownEvent, thumb) { GlobalX = 10, GlobalY = 20, X = 5, Y = 5 });

        Assert.True(fired);
        Assert.True(thumb.IsDragging);
    }

    [Fact]
    public void DragDelta_FiredOnMouseMove_WhenDragging()
    {
        var thumb = new Thumb();
        bool firedDelta = false;
        int dragDeltaX = 0;
        int dragDeltaY = 0;

        thumb.DragDelta += (s, e) =>
        {
            firedDelta = true;
            dragDeltaX = e.HorizontalChange;
            dragDeltaY = e.VerticalChange;
        };

        // Start drag
        thumb.RaiseEvent(new MouseEventArgs(UIElement.MouseDownEvent, thumb) { GlobalX = 10, GlobalY = 20 });

        // Move mouse
        thumb.RaiseEvent(new MouseEventArgs(UIElement.MouseMoveEvent, thumb) { GlobalX = 15, GlobalY = 18 });

        Assert.True(firedDelta);
        Assert.Equal(5, dragDeltaX); // 15 - 10
        Assert.Equal(-2, dragDeltaY); // 18 - 20
        Assert.True(thumb.IsDragging);
    }

    [Fact]
    public void DragCompleted_FiredOnMouseUp_WhenDragging()
    {
        var thumb = new Thumb();
        bool firedCompleted = false;

        thumb.DragCompleted += (s, e) =>
        {
            firedCompleted = true;
            Assert.Equal(5, e.HorizontalChange); // Final X - Start X
            Assert.Equal(-2, e.VerticalChange);  // Final Y - Start Y
        };

        // Start drag at (10, 20)
        thumb.RaiseEvent(new MouseEventArgs(UIElement.MouseDownEvent, thumb) { GlobalX = 10, GlobalY = 20 });

        // Move to (15, 18)
        thumb.RaiseEvent(new MouseEventArgs(UIElement.MouseMoveEvent, thumb) { GlobalX = 15, GlobalY = 18 });

        // End drag at (15, 18)
        thumb.RaiseEvent(new MouseEventArgs(UIElement.MouseUpEvent, thumb) { GlobalX = 15, GlobalY = 18 });

        Assert.True(firedCompleted);
        Assert.False(thumb.IsDragging);
    }

    [Fact]
    public void MouseCapture_AcquiredAndReleased_OnTuiWindow()
    {
        var window = new TuiWindow();
        var panel = new StackPanel();
        var thumb = new Thumb();
        panel.Children.Add(thumb);
        window.Content = panel;

        // Start drag
        thumb.RaiseEvent(new MouseEventArgs(UIElement.MouseDownEvent, thumb) { GlobalX = 0, GlobalY = 0 });

        // Check if window captured mouse via reflection or state
        var capturedField = typeof(TuiWindow).GetField("_capturedElement", BindingFlags.NonPublic | BindingFlags.Instance);
        var captured = capturedField?.GetValue(window) as UIElement;

        Assert.Same(thumb, captured);

        // End drag
        thumb.RaiseEvent(new MouseEventArgs(UIElement.MouseUpEvent, thumb) { GlobalX = 0, GlobalY = 0 });

        captured = capturedField?.GetValue(window) as UIElement;
        Assert.Null(captured);
    }
}
