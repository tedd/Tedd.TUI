using System;
using System.Collections.Generic;
using Tedd.TUI;
using Xunit;

namespace Tedd.TUI.Tests;

public class ThumbTests
{
    [Fact]
    public void Thumb_DragStarted_RaisedOnMouseDown()
    {
        // Arrange
        var window = new TuiWindow();
        var thumb = new Thumb();
        window.Content = thumb;

        window.Measure(new Size(100, 100));
        window.Arrange(new Rect(0, 0, 100, 100));

        bool eventRaised = false;
        double hOffset = -1;
        double vOffset = -1;

        thumb.DragStarted += (s, e) =>
        {
            eventRaised = true;
            hOffset = e.HorizontalOffset;
            vOffset = e.VerticalOffset;
        };

        // Act
        var args = new MouseEventArgs(UIElement.MouseDownEvent)
        {
            X = 5,
            Y = 5,
            GlobalX = 5,
            GlobalY = 5
        };
        thumb.RaiseEvent(args);

        // Assert
        Assert.True(eventRaised);
        Assert.Equal(0, hOffset);
        Assert.Equal(0, vOffset);
        Assert.True(thumb.IsDragging);
        Assert.Equal(thumb, window.CapturedElement);
    }

    [Fact]
    public void Thumb_DragDelta_RaisedOnMouseMove()
    {
        // Arrange
        var window = new TuiWindow();
        var thumb = new Thumb();
        window.Content = thumb;

        window.Measure(new Size(100, 100));
        window.Arrange(new Rect(0, 0, 100, 100));

        thumb.RaiseEvent(new MouseEventArgs(UIElement.MouseDownEvent) { X = 5, Y = 5, GlobalX = 5, GlobalY = 5 });

        bool eventRaised = false;
        double hChange = 0;
        double vChange = 0;

        thumb.DragDelta += (s, e) =>
        {
            eventRaised = true;
            hChange = e.HorizontalChange;
            vChange = e.VerticalChange;
        };

        // Act
        var moveArgs = new MouseEventArgs(UIElement.MouseMoveEvent)
        {
            X = 8,
            Y = 10,
            GlobalX = 8,
            GlobalY = 10
        };
        thumb.RaiseEvent(moveArgs);

        // Assert
        Assert.True(eventRaised);
        Assert.Equal(3, hChange);
        Assert.Equal(5, vChange);
    }

    [Fact]
    public void Thumb_DragCompleted_RaisedOnMouseUp()
    {
        // Arrange
        var window = new TuiWindow();
        var thumb = new Thumb();
        window.Content = thumb;

        window.Measure(new Size(100, 100));
        window.Arrange(new Rect(0, 0, 100, 100));

        thumb.RaiseEvent(new MouseEventArgs(UIElement.MouseDownEvent) { X = 5, Y = 5, GlobalX = 5, GlobalY = 5 });
        thumb.RaiseEvent(new MouseEventArgs(UIElement.MouseMoveEvent) { X = 8, Y = 10, GlobalX = 8, GlobalY = 10 });

        bool eventRaised = false;
        double hChange = 0;
        double vChange = 0;

        thumb.DragCompleted += (s, e) =>
        {
            eventRaised = true;
            hChange = e.HorizontalChange;
            vChange = e.VerticalChange;
        };

        // Act
        var upArgs = new MouseEventArgs(UIElement.MouseUpEvent)
        {
            X = 10,
            Y = 15,
            GlobalX = 10,
            GlobalY = 15
        };
        thumb.RaiseEvent(upArgs);

        // Assert
        Assert.True(eventRaised);
        Assert.Equal(5, hChange); // 10 - 5
        Assert.Equal(10, vChange); // 15 - 5
        Assert.False(thumb.IsDragging);
        Assert.Null(window.CapturedElement);
    }
}
