using System;
using Tedd.TUI.Tests.TestInfrastructure;
using Xunit;

namespace Tedd.TUI.Tests;

public class WrapPanelTests
{
    [Fact]
    public void MouseClick_WrappedButtons_HitsCorrectRow()
    {
        var first = new Button { Content = "First", Width = 8, Height = 3 };
        var second = new Button { Content = "Second", Width = 9, Height = 3 };
        var panel = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            Width = 18
        };
        panel.AddChild(first);
        panel.AddChild(new TextBlock { Text = "gap", Width = 4, Height = 3 });
        panel.AddChild(second);

        var host = new ControlTestHost(new Border { Child = panel }, 22, 10);
        var firstClicks = 0;
        var secondClicks = 0;
        first.Click += (_, _) => firstClicks++;
        second.Click += (_, _) => secondClicks++;

        host.Click(first, 2, 1);
        host.Click(panel, 9, 1);
        host.Click(second, 2, 1);

        Assert.Equal(1, firstClicks);
        Assert.Equal(1, secondClicks);
        Assert.True(second.IsFocused);
        Assert.True(second.RenderSize.Y > first.RenderSize.Y);
    }

    [Fact]
    public void WrapPanel_Horizontal_FlowsAndWraps()
    {
        var wrapPanel = new WrapPanel { Orientation = Orientation.Horizontal };
        var child1 = new TextBlock { Width = 10, Height = 2 };
        var child2 = new TextBlock { Width = 10, Height = 2 };
        var child3 = new TextBlock { Width = 10, Height = 2 };

        wrapPanel.AddChild(child1);
        wrapPanel.AddChild(child2);
        wrapPanel.AddChild(child3);

        // Constrain width to 25 so it can fit 2 items per line, then wrap the 3rd
        wrapPanel.Measure(new Size(25, 100));
        wrapPanel.Arrange(new Rect(0, 0, 25, 100));

        // Total width should be 20 (child1 + child2)
        // Total height should be 4 (2 lines of height 2)
        Assert.Equal(20, wrapPanel.DesiredSize.Width);
        Assert.Equal(4, wrapPanel.DesiredSize.Height);

        // Child 1
        Assert.Equal(0, child1.RenderSize.X);
        Assert.Equal(0, child1.RenderSize.Y);

        // Child 2
        Assert.Equal(10, child2.RenderSize.X);
        Assert.Equal(0, child2.RenderSize.Y);

        // Child 3 (Wrapped)
        Assert.Equal(0, child3.RenderSize.X);
        Assert.Equal(2, child3.RenderSize.Y);
    }

    [Fact]
    public void WrapPanel_Vertical_FlowsAndWraps()
    {
        var wrapPanel = new WrapPanel { Orientation = Orientation.Vertical };
        var child1 = new TextBlock { Width = 5, Height = 10 };
        var child2 = new TextBlock { Width = 5, Height = 10 };
        var child3 = new TextBlock { Width = 5, Height = 10 };

        wrapPanel.AddChild(child1);
        wrapPanel.AddChild(child2);
        wrapPanel.AddChild(child3);

        // Constrain height to 25 so it can fit 2 items per column, then wrap the 3rd
        wrapPanel.Measure(new Size(100, 25));
        wrapPanel.Arrange(new Rect(0, 0, 100, 25));

        // Total width should be 10 (2 columns of width 5)
        // Total height should be 20 (child1 + child2)
        Assert.Equal(10, wrapPanel.DesiredSize.Width);
        Assert.Equal(20, wrapPanel.DesiredSize.Height);

        // Child 1
        Assert.Equal(0, child1.RenderSize.X);
        Assert.Equal(0, child1.RenderSize.Y);

        // Child 2
        Assert.Equal(0, child2.RenderSize.X);
        Assert.Equal(10, child2.RenderSize.Y);

        // Child 3 (Wrapped)
        Assert.Equal(5, child3.RenderSize.X);
        Assert.Equal(0, child3.RenderSize.Y);
    }
}
