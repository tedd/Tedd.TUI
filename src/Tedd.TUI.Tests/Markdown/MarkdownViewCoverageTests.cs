using Xunit;
using Tedd.TUI;
using Tedd.TUI.Markdown;
using System;

namespace Tedd.TUI.Tests.Markdown;

public class MarkdownViewCoverageTests
{
    [Theory]
    [InlineData("Hello World")]
    [InlineData("Another test")]
    public void MarkdownView_TextProperty_SetsCorrectly(string input)
    {
        var md = new MarkdownView();
        md.Text = input;
        Assert.Equal(input, md.Text);
    }

    [Theory]
    [InlineData("/test")]
    [InlineData("/another/test")]
    public void MarkdownView_BaseDirectoryProperty_SetsCorrectly(string input)
    {
        var md = new MarkdownView();
        md.BaseDirectory = input;
        Assert.Equal(input, md.BaseDirectory);

        md.BaseDirectory = input; // Hit equality branch
        Assert.Equal(input, md.BaseDirectory);
    }

    [Fact]
    public void MarkdownView_ThemeProperty_SetsCorrectly()
    {
        var md = new MarkdownView();
        var theme = new MarkdownTheme();
        md.Theme = theme;
        Assert.Equal(theme, md.Theme);

        md.Theme = new MarkdownTheme();
        Assert.NotEqual(theme, md.Theme);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void MarkdownView_Refresh_EmptyText_GeneratesEmptyDocument(string? input)
    {
        var md = new MarkdownView();
        md.Text = input;
        md.Refresh();
        Assert.Equal(1, md.VisualChildrenCount);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(-1)]
    public void MarkdownView_GetVisualChild_OutOfRange_Throws(int index)
    {
        var md = new MarkdownView();
        Assert.Throws<ArgumentOutOfRangeException>(() => md.GetVisualChild(index));
    }

    [Theory]
    [InlineData(10, 10, 0, 0)]
    public void MarkdownView_Render_NullDoc_DoesNotThrow(int width, int height, int offsetX, int offsetY)
    {
        var md = new MarkdownView();
        var buf = new VirtualBuffer(width, height);

        // This validates that an empty control doesn't throw during a render call.
        md.Render(buf, offsetX, offsetY);
        Assert.NotNull(buf);
    }
}
