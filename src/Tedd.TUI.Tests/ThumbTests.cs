using System;
using System.Collections.Generic;
using Tedd.TUI;
using Tedd.TUI.Tests.TestInfrastructure;
using Xunit;

namespace Tedd.TUI.Tests;

public class ThumbTests
{
    [Fact]
    public void MouseDrag_NestedThumb_RoutesLifecycleAndCaptureOutsideBounds()
    {
        var thumb = new Thumb { Width = 3, Height = 2 };
        var sibling = new Thumb { Width = 3, Height = 2 };
        var canvas = new Canvas { Width = 30, Height = 10 };
        var caption = new TextBlock { Text = "Drag handles" };
        Canvas.SetLeft(caption, 1);
        Canvas.SetTop(caption, 0);
        Canvas.SetLeft(thumb, 4);
        Canvas.SetTop(thumb, 3);
        Canvas.SetLeft(sibling, 18);
        Canvas.SetTop(sibling, 3);
        canvas.AddChild(caption);
        canvas.AddChild(thumb);
        canvas.AddChild(sibling);
        var host = new ControlTestHost(new Border { Child = canvas }, 34, 14);

        var dragStarted = false;
        double startedX = 0, startedY = 0;
        var dragDelta = false;
        double deltaX = 0, deltaY = 0;
        var dragCompleted = false;
        double completedX = 0, completedY = 0;
        var completedCanceled = true;

        thumb.DragStarted += (_, e) =>
        {
            dragStarted = true;
            startedX = e.HorizontalOffset;
            startedY = e.VerticalOffset;
        };
        thumb.DragDelta += (_, e) =>
        {
            dragDelta = true;
            deltaX += e.HorizontalChange;
            deltaY += e.VerticalChange;
        };
        thumb.DragCompleted += (_, e) =>
        {
            dragCompleted = true;
            completedX = e.HorizontalChange;
            completedY = e.VerticalChange;
            completedCanceled = e.Canceled;
        };

        var start = thumb.PointToScreen(new Point(1, 1));
        host.MouseDown(start.X, start.Y);

        Assert.True(dragStarted);
        // Whole-cell input (terminal hosts) reports positions at the cell center.
        Assert.Equal(start.X + 0.5, startedX);
        Assert.Equal(start.Y + 0.5, startedY);
        Assert.True(thumb.IsDragging);
        Assert.False(sibling.IsDragging);
        Assert.Same(thumb, host.Window.CapturedElement);

        // Captured moves continue to target the thumb outside its bounds.
        host.MouseMove(start.X + 5, start.Y + 2);
        Assert.True(dragDelta);
        Assert.Equal(5, deltaX);
        Assert.Equal(2, deltaY);

        host.MouseMove(start.X + 7, start.Y + 5);
        Assert.Equal(7, deltaX);
        Assert.Equal(5, deltaY);
        Assert.False(sibling.IsDragging);

        host.MouseUp(start.X + 7, start.Y + 5);

        Assert.True(dragCompleted);
        Assert.Equal(7, completedX);
        Assert.Equal(5, completedY);
        Assert.False(completedCanceled);
        Assert.False(thumb.IsDragging);
        Assert.False(sibling.IsDragging);
        Assert.Null(host.Window.CapturedElement);
    }

    [Fact]
    public void MouseDrag_SubCellMoves_ReportFractionalDeltas()
    {
        var thumb = new Thumb { Width = 3, Height = 2 };
        var canvas = new Canvas { Width = 20, Height = 8 };
        Canvas.SetLeft(thumb, 4);
        Canvas.SetTop(thumb, 2);
        canvas.AddChild(thumb);
        var host = new ControlTestHost(new Border { Child = canvas }, 24, 12);

        double deltaX = 0, deltaY = 0;
        double completedX = 0, completedY = 0;
        thumb.DragDelta += (_, e) =>
        {
            deltaX += e.HorizontalChange;
            deltaY += e.VerticalChange;
        };
        thumb.DragCompleted += (_, e) =>
        {
            completedX = e.HorizontalChange;
            completedY = e.VerticalChange;
        };

        var start = thumb.PointToScreen(new Point(1, 1));
        double x = start.X + 0.5, y = start.Y + 0.5;
        host.MouseDownF(x, y);
        Assert.True(thumb.IsDragging);

        // Pixel-based hosts report positions between cell boundaries; deltas pass
        // through fractionally instead of being quantized away.
        host.MouseMoveF(x + 0.25, y + 0.5);
        Assert.Equal(0.25, deltaX, 10);
        Assert.Equal(0.5, deltaY, 10);

        host.MouseMoveF(x + 0.75, y + 1.25);
        Assert.Equal(0.75, deltaX, 10);
        Assert.Equal(1.25, deltaY, 10);

        host.MouseUpF(x + 0.75, y + 1.25);
        Assert.Equal(0.75, completedX, 10);
        Assert.Equal(1.25, completedY, 10);
        Assert.False(thumb.IsDragging);
    }

    [Fact]
    public void MouseDown_NestedThumb_CancelDragReleasesCaptureAndCancelsOnlyTarget()
    {
        var thumb = new Thumb { Width = 3, Height = 2 };
        var sibling = new Thumb { Width = 3, Height = 2 };
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.AddChild(new TextBlock { Text = "before " });
        row.AddChild(thumb);
        row.AddChild(new TextBlock { Text = " between " });
        row.AddChild(sibling);
        row.AddChild(new TextBlock { Text = " after" });
        var surface = new StackPanel();
        surface.AddChild(new TextBlock { Text = "Thumb surface" });
        surface.AddChild(row);
        var host = new ControlTestHost(new Border { Child = surface }, 36, 8);
        var dragCompleted = false;
        var completedCanceled = false;

        thumb.DragCompleted += (_, e) =>
        {
            dragCompleted = true;
            completedCanceled = e.Canceled;
        };

        var start = thumb.PointToScreen(new Point(1, 1));
        host.MouseDown(start.X, start.Y);

        Assert.True(thumb.IsDragging);
        Assert.False(sibling.IsDragging);
        Assert.Same(thumb, host.Window.CapturedElement);

        thumb.CancelDrag();

        Assert.True(dragCompleted);
        Assert.True(completedCanceled);
        Assert.False(thumb.IsDragging);
        Assert.False(sibling.IsDragging);
        Assert.Null(host.Window.CapturedElement);
    }
}
