using System;
using System.Collections.Generic;
using Tedd.TUI;
using Xunit;

namespace Tedd.TUI.Tests;

public class ThumbTests
{
    [Fact]
    public void Thumb_DragLifecycle_FiresEventsCorrectly()
    {
        // Arrange
        var window = new TuiWindow();
        var canvas = new Canvas { Width = 100, Height = 100 };
        var thumb = new Thumb { Width = 10, Height = 10 };
        canvas.Children.Add(thumb);
        window.Content = canvas;

        window.Measure(new Size(100, 100));
        window.Arrange(new Rect(0, 0, 100, 100));

        var startedEvents = new List<DragStartedEventArgs>();
        var deltaEvents = new List<DragDeltaEventArgs>();
        var completedEvents = new List<DragCompletedEventArgs>();

        thumb.DragStarted += (s, e) => startedEvents.Add(e);
        thumb.DragDelta += (s, e) => deltaEvents.Add(e);
        thumb.DragCompleted += (s, e) => completedEvents.Add(e);

        // Act & Assert

        // 1. Mouse Down -> DragStarted
        var mouseDownEvent = new MouseEventArgs { RoutedEvent = UIElement.MouseDownEvent, GlobalX = 5, GlobalY = 5, X = 5, Y = 5 };
        thumb.RaiseEvent(mouseDownEvent);

        Assert.True(thumb.IsDragging);
        Assert.Single(startedEvents);
        Assert.Equal(5, startedEvents[0].HorizontalOffset);
        Assert.Equal(5, startedEvents[0].VerticalOffset);
        Assert.Same(thumb, window.CapturedElement);

        // 2. Mouse Move -> DragDelta
        var mouseMoveEvent1 = new MouseEventArgs { RoutedEvent = UIElement.MouseMoveEvent, GlobalX = 15, GlobalY = 10, X = 15, Y = 10 };
        thumb.RaiseEvent(mouseMoveEvent1);

        Assert.Single(deltaEvents);
        Assert.Equal(10, deltaEvents[0].HorizontalChange);
        Assert.Equal(5, deltaEvents[0].VerticalChange);

        // 3. Mouse Move 2 -> DragDelta
        var mouseMoveEvent2 = new MouseEventArgs { RoutedEvent = UIElement.MouseMoveEvent, GlobalX = 12, GlobalY = 18, X = 12, Y = 18 };
        thumb.RaiseEvent(mouseMoveEvent2);

        Assert.Equal(2, deltaEvents.Count);
        Assert.Equal(-3, deltaEvents[1].HorizontalChange);
        Assert.Equal(8, deltaEvents[1].VerticalChange);

        // 4. Mouse Up -> DragCompleted
        var mouseUpEvent = new MouseEventArgs { RoutedEvent = UIElement.MouseUpEvent, GlobalX = 12, GlobalY = 18, X = 12, Y = 18 };
        thumb.RaiseEvent(mouseUpEvent);

        Assert.False(thumb.IsDragging);
        Assert.Single(completedEvents);
        Assert.Equal(12 - 5, completedEvents[0].HorizontalChange); // total change
        Assert.Equal(18 - 5, completedEvents[0].VerticalChange); // total change
        Assert.False(completedEvents[0].Canceled);
        Assert.Null(window.CapturedElement);
    }

    [Fact]
    public void Thumb_CancelDrag_FiresCompletedWithCanceledTrue()
    {
        // Arrange
        var window = new TuiWindow();
        var canvas = new Canvas { Width = 100, Height = 100 };
        var thumb = new Thumb { Width = 10, Height = 10 };
        canvas.Children.Add(thumb);
        window.Content = canvas;

        window.Measure(new Size(100, 100));
        window.Arrange(new Rect(0, 0, 100, 100));

        var completedEvents = new List<DragCompletedEventArgs>();
        thumb.DragCompleted += (s, e) => completedEvents.Add(e);

        // Act
        var mouseDownEvent = new MouseEventArgs { RoutedEvent = UIElement.MouseDownEvent, GlobalX = 5, GlobalY = 5, X = 5, Y = 5 };
        thumb.RaiseEvent(mouseDownEvent);

        var mouseMoveEvent = new MouseEventArgs { RoutedEvent = UIElement.MouseMoveEvent, GlobalX = 15, GlobalY = 10, X = 15, Y = 10 };
        thumb.RaiseEvent(mouseMoveEvent);

        thumb.CancelDrag();

        // Assert
        Assert.False(thumb.IsDragging);
        Assert.Single(completedEvents);
        Assert.True(completedEvents[0].Canceled);
        Assert.Equal(10, completedEvents[0].HorizontalChange);
        Assert.Equal(5, completedEvents[0].VerticalChange);
        Assert.Null(window.CapturedElement);
    }
}
