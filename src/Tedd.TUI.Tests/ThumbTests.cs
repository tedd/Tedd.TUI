using System;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class ThumbTests
{
    [Fact]
    public void Thumb_DragLifecycle_BubblingRoutedEvents()
    {
        var window = new TuiWindow();
        var panel = new StackPanel();
        var thumb = new Thumb();

        panel.Children.Add(thumb);
        window.Content = panel;

        // Add to visual tree, measure, arrange
        window.Measure(new Size(100, 100));
        window.Arrange(new Rect(0, 0, 100, 100));

        bool dragStarted = false;
        bool dragDelta = false;
        bool dragCompleted = false;

        panel.AddHandler(Thumb.DragStartedEvent, new DragStartedEventHandler((s, e) => dragStarted = true));
        panel.AddHandler(Thumb.DragDeltaEvent, new DragDeltaEventHandler((s, e) => dragDelta = true));
        panel.AddHandler(Thumb.DragCompletedEvent, new DragCompletedEventHandler((s, e) => dragCompleted = true));

        // Start Drag
        var mousedown = new MouseEventArgs(UIElement.MouseDownEvent)
        {
            GlobalX = 10,
            GlobalY = 10
        };
        thumb.RaiseEvent(mousedown);

        Assert.True(dragStarted);
        Assert.True(thumb.IsDragging);
        Assert.Equal(thumb, window.CapturedElement);

        // Delta Drag
        var mousemove = new MouseEventArgs(UIElement.MouseMoveEvent)
        {
            GlobalX = 15,
            GlobalY = 12
        };
        thumb.RaiseEvent(mousemove);

        Assert.True(dragDelta);
        Assert.True(thumb.IsDragging);

        // Completed Drag
        var mouseup = new MouseEventArgs(UIElement.MouseUpEvent)
        {
            GlobalX = 15,
            GlobalY = 12
        };
        thumb.RaiseEvent(mouseup);

        Assert.True(dragCompleted);
        Assert.False(thumb.IsDragging);
        Assert.Null(window.CapturedElement);
    }
}
