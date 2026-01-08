using Xunit;
using Tedd.TUI;
using System;

namespace Tedd.TUI.Tests;

public class StackPanelTests
{
    [Fact]
    public void TestVerticalStackPanel()
    {
        var stack = new StackPanel { Orientation = Orientation.Vertical };
        var t1 = new TextBlock { Text = "Hello" };
        var t2 = new TextBlock { Text = "World" };
        
        stack.AddChild(t1);
        stack.AddChild(t2);

        // Measure
        stack.Measure(new Size(100, 100));
        
        // "Hello" = 5x1, "World" = 5x1. Stack should be 5x2.
        Assert.Equal(5, stack.DesiredSize.Width);
        Assert.Equal(2, stack.DesiredSize.Height);

        // Arrange
        stack.Arrange(new Rect(0, 0, 100, 100));

        // Render
        var buffer = new VirtualBuffer(10, 5);
        stack.Render(buffer, 0, 0);

        Assert.Equal('H', buffer.GetPixel(0, 0).Character);
        Assert.Equal('W', buffer.GetPixel(0, 1).Character);
    }
}
