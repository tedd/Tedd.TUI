using Tedd.TUI.Controls;
using Tedd.TUI.Media;
using System;
using Xunit;

namespace Tedd.TUI.Tests;

public class SeparatorCoverageTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(0, 10)]
    [InlineData(10, 0)]
    [InlineData(-1, -1)]
    public void Render_InvalidSize_ReturnsEarly(int width, int height)
    {
        var sep = new Separator();
        sep.Measure(new Size(width, height));
        sep.Arrange(new Rect(0, 0, width, height));

        var buffer = new VirtualBuffer(10, 10);
        sep.Render(buffer, 0, 0);

        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                var p = buffer.GetPixel(i, j);
                Assert.Equal(' ', p.Character);
            }
        }
    }

    [Theory]
    [InlineData(5, 1, ConsoleColor.Blue)]
    [InlineData(10, 1, ConsoleColor.Red)]
    [InlineData(2, 5, ConsoleColor.Green)]
    public void Render_WithoutBackground_PreservesBufferBackground(int width, int height, ConsoleColor bufferBgColor)
    {
        var sep = new Separator();
        sep.Width = width;
        sep.Height = height;
        Assert.Null(sep.Background);

        sep.Measure(new Size(width, height));
        sep.Arrange(new Rect(0, 0, width, height));

        var buffer = new VirtualBuffer(10, 10);
        var expectedBg = (TuiColor)bufferBgColor;

        for (int x = 0; x < width; x++)
        {
            buffer.SetPixel(x, 0, 'X', TuiColor.Red, expectedBg);
        }

        sep.Render(buffer, 0, 0);

        for (int x = 0; x < width; x++)
        {
            var p = buffer.GetPixel(x, 0);
            Assert.Equal('\u2500', p.Character);
            Assert.Equal(expectedBg, p.Background);
        }
    }
}
