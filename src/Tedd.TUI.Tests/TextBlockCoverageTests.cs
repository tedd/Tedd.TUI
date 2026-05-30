using System;
using System.Collections.Generic;
using Tedd.TUI;
using Xunit;

namespace Tedd.TUI.Tests;

public class TextBlockCoverageTests
{
    [Fact]
    public void TextBlock_DefaultProperties()
    {
        var tb = new TextBlock();
        Assert.Equal(string.Empty, tb.Text);
        Assert.Equal(TextWrapping.NoWrap, tb.TextWrapping);
    }

    [Theory]
    [InlineData("", 10, 0, 0)]
    [InlineData("hello", 10, 5, 1)]
    [InlineData("hello", -1, 5, 1)]
    public void MeasureOverride_NoWrap_ReturnsExpectedSize(string text, double width, double expectedWidth, double expectedHeight)
    {
        var tb = new TextBlock { Text = text, TextWrapping = TextWrapping.NoWrap };
        tb.Measure(new Size((int)width, 10));
        Assert.Equal(new Size((int)expectedWidth, (int)expectedHeight), tb.DesiredSize);
    }

    [Theory]
    [InlineData("", 10, 0, 0)] // Empty
    [InlineData("hello", 0, 5, 1)] // Zero width -> text length used? wait...
    [InlineData("hello world", 5, 5, 2)] // Wraps to "hello" (5) and "world" (5) -> 5x2
    [InlineData("helloworld", 5, 5, 2)] // Hard break word > maxWidth -> "hello" "world"
    [InlineData("line1\nline2", 10, 5, 2)] // Explicit newline
    [InlineData("line1\r\nline2", 10, 5, 2)] // Explicit windows newline
    [InlineData("a   b", 5, 5, 1)] // Whitespace handling
    [InlineData("   ", 5, 0, 1)] // Leading spaces are skipped initially
    [InlineData("word toooooooo long", 4, 4, 5)] // word, tooo, oooo, long
    public void MeasureOverride_Wrap_ReturnsExpectedSize(string text, double width, double expectedWidth, double expectedHeight)
    {
        var tb = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap };
        tb.Measure(new Size((int)width, 10));
        // WrapText handles width <= 0 by returning string.Empty, which is length 0.
        // Wait, text.Length is what is returned initially if width <= 0?
        // Ah, in MeasureOverride: "if (TextWrapping == TextWrapping.NoWrap || availableSize.Width <= 0) return new Size(text.Length, 1)"
        // Yes! width <= 0 ALWAYS returns (text.Length, 1) regardless of TextWrapping!
        Assert.Equal(new Size((int)expectedWidth, (int)expectedHeight), tb.DesiredSize);
    }

    [Fact]
    public void Render_EmptyText_DoesNothing()
    {
        var tb = new TextBlock { Text = "" };
        var buffer = new VirtualBuffer(10, 10);
        tb.Measure(new Size(10, 10));
        tb.Arrange(new Rect(0, 0, 10, 10));
        tb.Render(buffer, 0, 0);
        Assert.Equal(' ', buffer.GetPixel(0, 0).Character); // Unchanged
    }

    [Fact]
    public void Render_NoWrap_DrawsLine()
    {
        var tb = new TextBlock { Text = "test" };
        var buffer = new VirtualBuffer(10, 10);
        tb.Measure(new Size(10, 10));
        tb.Arrange(new Rect(0, 0, 10, 10));
        tb.Render(buffer, 0, 0);
        Assert.Equal('t', buffer.GetPixel(0, 0).Character);
        Assert.Equal('e', buffer.GetPixel(1, 0).Character);
        Assert.Equal('s', buffer.GetPixel(2, 0).Character);
        Assert.Equal('t', buffer.GetPixel(3, 0).Character);
    }

    [Fact]
    public void Render_Wrap_DrawsLines()
    {
        var tb = new TextBlock { Text = "hello world", TextWrapping = TextWrapping.Wrap };
        var buffer = new VirtualBuffer(10, 10);
        tb.Measure(new Size(5, 10)); // Width 5 forces wrap
        tb.Arrange(new Rect(0, 0, 5, 10));
        tb.Render(buffer, 0, 0);

        Assert.Equal('h', buffer.GetPixel(0, 0).Character);
        Assert.Equal('w', buffer.GetPixel(0, 1).Character);
    }

    [Fact]
    public void Render_ZeroHeight_EarlyExit()
    {
        var tb = new TextBlock { Text = "hello" };
        var buffer = new VirtualBuffer(10, 10);
        tb.Measure(new Size(10, 0));
        tb.Arrange(new Rect(0, 0, 10, 0)); // Height is 0
        tb.Render(buffer, 0, 0);
        Assert.Equal(' ', buffer.GetPixel(0, 0).Character);
    }

    [Fact]
    public void Render_WrapCached()
    {
        var tb = new TextBlock { Text = "hello world", TextWrapping = TextWrapping.Wrap };
        var buffer = new VirtualBuffer(10, 10);

        // Measure first to populate cache
        tb.Measure(new Size(5, 10));
        tb.Arrange(new Rect(0, 0, 5, 10));

        // First render uses cache
        tb.Render(buffer, 0, 0);
        Assert.Equal('h', buffer.GetPixel(0, 0).Character);

        // Change text width
        tb.Measure(new Size(11, 10));
        tb.Arrange(new Rect(0, 0, 11, 10));
        tb.Render(buffer, 0, 0);
        Assert.Equal('w', buffer.GetPixel(6, 0).Character);
    }

    [Fact]
    public void Foreground_CanBeSet()
    {
        var tb = new TextBlock { Text = "a", Foreground = TuiColor.Red };
        var buffer = new VirtualBuffer(10, 10);
        tb.Measure(new Size(10, 10));
        tb.Arrange(new Rect(0, 0, 10, 10));
        tb.Render(buffer, 0, 0);
        Assert.Equal(TuiColor.Red, buffer.GetPixel(0, 0).Foreground);
    }

    [Fact]
    public void Render_NoWrap_WidthZero()
    {
        var tb = new TextBlock { Text = "test", TextWrapping = TextWrapping.NoWrap };
        var buffer = new VirtualBuffer(10, 10);
        tb.Measure(new Size(0, 10)); // Width 0
        tb.Arrange(new Rect(0, 0, 0, 10));
        tb.Render(buffer, 0, 0);
        Assert.Equal(' ', buffer.GetPixel(0, 0).Character);
    }

    [Fact]
    public void MeasureOverride_Wrap_ZeroWidth()
    {
        var tb = new TextBlock { Text = "hello", TextWrapping = TextWrapping.Wrap };
        tb.Measure(new Size(0, 10));
        Assert.Equal(new Size(5, 1), tb.DesiredSize);
    }
}
