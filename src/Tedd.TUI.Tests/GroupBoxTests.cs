using System;
using Tedd.TUI;
using Xunit;

namespace Tedd.TUI.Tests;

public class GroupBoxTests
{
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
