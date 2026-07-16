using System;
using Xunit;
using Tedd.TUI.Tests.TestInfrastructure;

namespace Tedd.TUI.Tests;

public class CanvasTests
{
    [Fact]
    public void MouseClick_PositionedButtons_ActivatesOnlyCanvasHitTarget()
    {
        var first = new Button { Content = "A", Width = 5, Height = 3 };
        var second = new Button { Content = "B", Width = 5, Height = 3 };
        Canvas.SetLeft(first, 2);
        Canvas.SetTop(first, 1);
        Canvas.SetLeft(second, 12);
        Canvas.SetTop(second, 4);

        var canvas = new Canvas { Width = 20, Height = 8 };
        canvas.AddChild(first);
        canvas.AddChild(second);
        var border = new Border
        {
            Child = canvas,
            Width = 22,
            Height = 10,
            BoxStyle = BoxStyle.Double
        };
        var surface = new StackPanel();
        surface.AddChild(new TextBlock { Text = "canvas" });
        surface.AddChild(border);
        var host = new ControlTestHost(surface, 22, 11);
        int firstClicks = 0;
        int secondClicks = 0;
        first.Click += (_, _) => firstClicks++;
        second.Click += (_, _) => secondClicks++;

        host.Click(first, 1, 1);
        host.Click(second, 1, 1);
        host.Click(canvas, 9, 6);

        Assert.Equal((1, 1), (firstClicks, secondClicks));
    }

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
