using System;
using Xunit;

namespace Tedd.TUI.Tests;

public class ThumbTests
{
    [Fact]
    public void Thumb_DragStarted_RaisedOnMouseDown()
    {
        var thumb = new Thumb();
        bool eventRaised = false;

        thumb.DragStarted += (s, e) =>
        {
            eventRaised = true;
            Assert.IsType<DragStartedEventArgs>(e);
            var args = (DragStartedEventArgs)e;
            Assert.Equal(0, args.HorizontalOffset);
            Assert.Equal(0, args.VerticalOffset);
        };

        var window = new TuiWindow();
        window.Content = thumb;
        window.Arrange(new Rect(0, 0, 100, 100));

        thumb.OnMouseDown(new MouseEventArgs(UIElement.MouseDownEvent) { GlobalX = 10, GlobalY = 10 });

        Assert.True(eventRaised);
        Assert.True(thumb.IsDragging);
        Assert.Equal(thumb, window.CapturedElement);
    }

    [Fact]
    public void Thumb_DragDelta_RaisedOnMouseMoveWhileDragging()
    {
        var thumb = new Thumb();
        int eventCount = 0;
        double lastH = 0;
        double lastV = 0;

        thumb.DragDelta += (s, e) =>
        {
            eventCount++;
            var args = (DragDeltaEventArgs)e;
            lastH = args.HorizontalChange;
            lastV = args.VerticalChange;
        };

        var window = new TuiWindow();
        window.Content = thumb;

        thumb.OnMouseDown(new MouseEventArgs(UIElement.MouseDownEvent) { GlobalX = 10, GlobalY = 10 });

        thumb.OnMouseMove(new MouseEventArgs(UIElement.MouseMoveEvent) { GlobalX = 15, GlobalY = 12 });

        Assert.Equal(1, eventCount);
        Assert.Equal(5, lastH);
        Assert.Equal(2, lastV);

        thumb.OnMouseMove(new MouseEventArgs(UIElement.MouseMoveEvent) { GlobalX = 14, GlobalY = 15 });

        Assert.Equal(2, eventCount);
        Assert.Equal(-1, lastH);
        Assert.Equal(3, lastV);
    }

    [Fact]
    public void Thumb_DragCompleted_RaisedOnMouseUp()
    {
        var thumb = new Thumb();
        bool eventRaised = false;

        thumb.DragCompleted += (s, e) =>
        {
            eventRaised = true;
            var args = (DragCompletedEventArgs)e;
            Assert.Equal(10, args.HorizontalChange);
            Assert.Equal(5, args.VerticalChange);
            Assert.False(args.Canceled);
        };

        var window = new TuiWindow();
        window.Content = thumb;

        thumb.OnMouseDown(new MouseEventArgs(UIElement.MouseDownEvent) { GlobalX = 10, GlobalY = 10 });
        thumb.OnMouseMove(new MouseEventArgs(UIElement.MouseMoveEvent) { GlobalX = 15, GlobalY = 12 });
        thumb.OnMouseUp(new MouseEventArgs(UIElement.MouseUpEvent) { GlobalX = 20, GlobalY = 15 });

        Assert.True(eventRaised);
        Assert.False(thumb.IsDragging);
        Assert.Null(window.CapturedElement);
    }

    [Fact]
    public void Thumb_DragEvents_BubbleUp()
    {
        var thumb = new Thumb();
        var panel = new StackPanel { Children = { thumb } };

        int startedCount = 0;
        int deltaCount = 0;
        int completedCount = 0;

        panel.AddHandler(Thumb.DragStartedEvent, new RoutedEventHandler((s, e) => startedCount++));
        panel.AddHandler(Thumb.DragDeltaEvent, new RoutedEventHandler((s, e) => deltaCount++));
        panel.AddHandler(Thumb.DragCompletedEvent, new RoutedEventHandler((s, e) => completedCount++));

        thumb.OnMouseDown(new MouseEventArgs(UIElement.MouseDownEvent) { GlobalX = 10, GlobalY = 10 });
        thumb.OnMouseMove(new MouseEventArgs(UIElement.MouseMoveEvent) { GlobalX = 15, GlobalY = 12 });
        thumb.OnMouseUp(new MouseEventArgs(UIElement.MouseUpEvent) { GlobalX = 20, GlobalY = 15 });

        Assert.Equal(1, startedCount);
        Assert.Equal(1, deltaCount);
        Assert.Equal(1, completedCount);
    }

    [Fact]
    public void Thumb_CancelDrag_RaisesDragCompletedWithCanceledTrue()
    {
        var thumb = new Thumb();
        bool eventRaised = false;

        thumb.DragCompleted += (s, e) =>
        {
            eventRaised = true;
            var args = (DragCompletedEventArgs)e;
            Assert.Equal(5, args.HorizontalChange);
            Assert.Equal(2, args.VerticalChange);
            Assert.True(args.Canceled);
        };

        var window = new TuiWindow();
        window.Content = thumb;

        thumb.OnMouseDown(new MouseEventArgs(UIElement.MouseDownEvent) { GlobalX = 10, GlobalY = 10 });
        thumb.OnMouseMove(new MouseEventArgs(UIElement.MouseMoveEvent) { GlobalX = 15, GlobalY = 12 });

        thumb.CancelDrag();

        Assert.True(eventRaised);
        Assert.False(thumb.IsDragging);
        Assert.Null(window.CapturedElement);
    }
}
