using System;
using System.Collections.Generic;
using Tedd.TUI;
using Xunit;

namespace Tedd.TUI.Tests;

public class ThumbTests
{
    [Fact]
    public void Thumb_RaisesDragEvents_InCorrectOrder()
    {
        var window = new TuiWindow();
        var thumb = new Thumb();
        window.Content = thumb;

        // Force layout pass
        window.Measure(new Size(80, 24));
        window.Arrange(new Rect(0, 0, 80, 24));

        var eventsReceived = new List<string>();
        int deltaX = 0;
        int deltaY = 0;

        thumb.DragStarted += (s, e) => eventsReceived.Add("DragStarted");
        thumb.DragDelta += (s, e) =>
        {
            eventsReceived.Add("DragDelta");
            deltaX += e.HorizontalChange;
            deltaY += e.VerticalChange;
        };
        thumb.DragCompleted += (s, e) => eventsReceived.Add("DragCompleted");

        // 1. Mouse Down (Starts Drag)
        thumb.RaiseEvent(new MouseEventArgs(UIElement.MouseDownEvent)
        {
            GlobalX = 10,
            GlobalY = 10,
            X = 0,
            Y = 0
        });

        Assert.True(thumb.IsDragging);
        Assert.Single(eventsReceived);
        Assert.Equal("DragStarted", eventsReceived[0]);

        // 2. Mouse Move (Delta)
        thumb.RaiseEvent(new MouseEventArgs(UIElement.MouseMoveEvent)
        {
            GlobalX = 15,
            GlobalY = 12,
            X = 5,
            Y = 2
        });

        Assert.Equal(2, eventsReceived.Count);
        Assert.Equal("DragDelta", eventsReceived[1]);
        Assert.Equal(5, deltaX);
        Assert.Equal(2, deltaY);

        // 3. Another Mouse Move (Delta)
        thumb.RaiseEvent(new MouseEventArgs(UIElement.MouseMoveEvent)
        {
            GlobalX = 14,
            GlobalY = 15,
            X = 4,
            Y = 5
        });

        Assert.Equal(3, eventsReceived.Count);
        Assert.Equal("DragDelta", eventsReceived[2]);
        Assert.Equal(4, deltaX); // 5 + (-1)
        Assert.Equal(5, deltaY); // 2 + 3

        // 4. Mouse Up (Completes Drag)
        thumb.RaiseEvent(new MouseEventArgs(UIElement.MouseUpEvent)
        {
            GlobalX = 14,
            GlobalY = 15,
            X = 4,
            Y = 5
        });

        Assert.False(thumb.IsDragging);
        Assert.Equal(4, eventsReceived.Count);
        Assert.Equal("DragCompleted", eventsReceived[3]);
    }

    [Fact]
    public void Thumb_IgnoresMouseMove_WhenNotDragging()
    {
        var window = new TuiWindow();
        var thumb = new Thumb();
        window.Content = thumb;

        window.Measure(new Size(80, 24));
        window.Arrange(new Rect(0, 0, 80, 24));

        bool dragDeltaFired = false;
        thumb.DragDelta += (s, e) => dragDeltaFired = true;

        thumb.RaiseEvent(new MouseEventArgs(UIElement.MouseMoveEvent)
        {
            GlobalX = 10,
            GlobalY = 10,
            X = 0,
            Y = 0
        });

        Assert.False(dragDeltaFired);
        Assert.False(thumb.IsDragging);
    }
}
