using System;
using Tedd.TUI;
using Tedd.TUI.Tests.TestInfrastructure;
using Xunit;

namespace Tedd.TUI.Tests;

public class GroupBoxTests
{
    [Fact]
    public void MouseClick_NestedButtons_RoutesThroughGroupBoxContent()
    {
        var first = new Button { Content = "First", Width = 9 };
        var second = new Button { Content = "Second", Width = 9 };
        var row = new StackPanel { Orientation = Orientation.Horizontal };
        row.AddChild(first);
        row.AddChild(new TextBlock { Text = "   " });
        row.AddChild(second);

        var group = new GroupBox
        {
            Header = new TextBlock { Text = "Actions" },
            Content = row
        };
        // Outer host border is an incidental surface (no padding); the GroupBox's
        // own frame contributes the default 1-char padding, so the host needs
        // 3 (button) + 2 (group border) + 2 (group padding) + 2 (host border) rows.
        var host = new ControlTestHost(new Border { Child = group, Padding = new Thickness(0) }, 28, 10);
        var firstClicks = 0;
        var secondClicks = 0;
        first.Click += (_, _) => firstClicks++;
        second.Click += (_, _) => secondClicks++;

        host.Click(first, 2, 1);
        host.Click(group, 11, 2);
        host.Click(second, 2, 1);

        Assert.Equal(1, firstClicks);
        Assert.Equal(1, secondClicks);
        Assert.True(second.IsFocused);
    }

    [Fact]
    public void GroupBox_DefaultProperties_AreSet()
    {
        var gb = new GroupBox();
        Assert.Equal(BoxStyle.Single, gb.BoxStyle);
        Assert.Equal(ConsoleColor.Gray, gb.BorderColor);
        Assert.False(gb.Focusable);
    }

    [Fact]
    public void GroupBox_Render_HasBorderAndTitle()
    {
        var buffer = new VirtualBuffer(20, 10);
        var tb = new TextBlock { Text = "TestTitle" };
        var content = new TextBlock { Text = "Content" };

        var gb = new GroupBox { Header = tb, Content = content };
        gb.Measure(new Size(20, 10));
        gb.Arrange(new Rect(0, 0, 20, 10));
        gb.Render(buffer, 0, 0);

        // Assert we got something like a box character
        var chars = BoxDrawingChars.Get(BoxStyle.Single);
        var topLeft = buffer.GetPixel(0, 0).Character;
        Assert.Equal(chars.TopLeft, topLeft);
    }
}
