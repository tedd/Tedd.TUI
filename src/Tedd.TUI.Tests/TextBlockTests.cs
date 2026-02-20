using System;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class TextBlockTests
{
    [Fact]
    public void Properties_DefaultValues()
    {
        var tb = new TextBlock();
        Assert.Equal(string.Empty, tb.Text);
        Assert.Equal(ConsoleColor.White, tb.Foreground);
    }

    [Fact]
    public void Render_DrawsText()
    {
        var tb = new TextBlock { Text = "Test", Foreground = ConsoleColor.Red };
        tb.Measure(new Size(10, 1));
        tb.Arrange(new Rect(0,0,10,1));

        var buffer = new VirtualBuffer(10, 1);
        tb.Render(buffer, 0, 0);

        Assert.Equal('T', buffer.GetPixel(0, 0).Character);
        Assert.Equal(ConsoleColor.Red, buffer.GetPixel(0, 0).Foreground);
        Assert.Equal('e', buffer.GetPixel(1, 0).Character);
        Assert.Equal('s', buffer.GetPixel(2, 0).Character);
        Assert.Equal('t', buffer.GetPixel(3, 0).Character);
    }
}
