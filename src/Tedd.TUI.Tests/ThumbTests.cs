using System;
using System.Collections.Generic;
using Tedd.TUI;
using Xunit;

namespace Tedd.TUI.Tests;

public class ThumbTests
{
    [Fact]
    public void Thumb_DragLifecycle_RaisesCorrectEvents()
    {
        var root = new TuiWindow();
        var thumb = new Thumb { Width = 1, Height = 1 };
        root.Content = thumb;

        root.Measure(new Size(100, 100));
        root.Arrange(new Rect(0, 0, 100, 100));

        bool dragStarted = false;
        double startedX = 0, startedY = 0;

        bool dragDelta = false;
        double deltaX = 0, deltaY = 0;

        bool dragCompleted = false;
        double completedX = 0, completedY = 0;
        bool completedCanceled = true;

        thumb.DragStarted += (s, e) =>
        {
            dragStarted = true;
            startedX = e.HorizontalOffset;
            startedY = e.VerticalOffset;
        };

        thumb.DragDelta += (s, e) =>
        {
            dragDelta = true;
            deltaX += e.HorizontalChange;
            deltaY += e.VerticalChange;
        };

        thumb.DragCompleted += (s, e) =>
        {
            dragCompleted = true;
            completedX = e.HorizontalChange;
            completedY = e.VerticalChange;
            completedCanceled = e.Canceled;
        };

        // Simulate Drag Start
        var mouseDownArgs = new MouseEventArgs(UIElement.MouseDownEvent) { GlobalX = 10, GlobalY = 10, X = 0, Y = 0 };
        thumb.RaiseEvent(mouseDownArgs);

        Assert.True(dragStarted);
        Assert.Equal(10, startedX);
        Assert.Equal(10, startedY);
        Assert.True(thumb.IsDragging);
        Assert.Equal(thumb, root.CapturedElement);

        // Simulate Drag Delta
        var mouseMoveArgs = new MouseEventArgs(UIElement.MouseMoveEvent) { GlobalX = 15, GlobalY = 12, X = 5, Y = 2 };
        thumb.RaiseEvent(mouseMoveArgs);

        Assert.True(dragDelta);
        Assert.Equal(5, deltaX);
        Assert.Equal(2, deltaY);

        // Simulate another Delta
        mouseMoveArgs = new MouseEventArgs(UIElement.MouseMoveEvent) { GlobalX = 17, GlobalY = 15, X = 7, Y = 5 };
        thumb.RaiseEvent(mouseMoveArgs);

        Assert.Equal(7, deltaX); // 5 + 2
        Assert.Equal(5, deltaY); // 2 + 3

        // Simulate Drag Complete
        var mouseUpArgs = new MouseEventArgs(UIElement.MouseUpEvent) { GlobalX = 17, GlobalY = 15, X = 7, Y = 5 };
        thumb.RaiseEvent(mouseUpArgs);

        Assert.True(dragCompleted);
        Assert.Equal(7, completedX);
        Assert.Equal(5, completedY);
        Assert.False(completedCanceled);
        Assert.False(thumb.IsDragging);
        Assert.Null(root.CapturedElement);
    }

    [Fact]
    public void Thumb_CancelDrag_RaisesDragCompletedWithCanceledTrue()
    {
        var root = new TuiWindow();
        var thumb = new Thumb { Width = 1, Height = 1 };
        root.Content = thumb;

        root.Measure(new Size(100, 100));
        root.Arrange(new Rect(0, 0, 100, 100));

        bool dragCompleted = false;
        bool completedCanceled = false;

        thumb.DragCompleted += (s, e) =>
        {
            dragCompleted = true;
            completedCanceled = e.Canceled;
        };

        var mouseDownArgs = new MouseEventArgs(UIElement.MouseDownEvent) { GlobalX = 10, GlobalY = 10 };
        thumb.RaiseEvent(mouseDownArgs);

        Assert.True(thumb.IsDragging);

        thumb.CancelDrag();

        Assert.True(dragCompleted);
        Assert.True(completedCanceled);
        Assert.False(thumb.IsDragging);
        Assert.Null(root.CapturedElement);
    }
}
