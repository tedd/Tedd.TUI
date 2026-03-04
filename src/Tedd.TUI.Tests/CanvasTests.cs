using System;
using Xunit;

namespace Tedd.TUI.Tests;

public class CanvasTests
{
    [Fact]
    public void Canvas_PositionsChildrenUsingAttachedProperties()
    {
        var canvas = new Canvas();
        var child1 = new TextBlock { Width = 10, Height = 5 };
        var child2 = new TextBlock { Width = 10, Height = 5 };

        Canvas.SetLeft(child1, 15);
        Canvas.SetTop(child1, 20);

        Canvas.SetLeft(child2, 50);
        Canvas.SetTop(child2, 60);

        canvas.AddChild(child1);
        canvas.AddChild(child2);

        // Canvas itself should return 0,0 desired size
        canvas.Measure(new Size(100, 100));
        Assert.Equal(0, canvas.DesiredSize.Width);
        Assert.Equal(0, canvas.DesiredSize.Height);

        canvas.Arrange(new Rect(0, 0, 100, 100));

        // Child 1
        Assert.Equal(15, child1.RenderSize.X);
        Assert.Equal(20, child1.RenderSize.Y);

        // Child 2
        Assert.Equal(50, child2.RenderSize.X);
        Assert.Equal(60, child2.RenderSize.Y);
    }
}
