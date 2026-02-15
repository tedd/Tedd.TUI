using Xunit;
using Tedd.TUI;
using Tedd.TUI.Markdown;
using System;

namespace Tedd.TUI.Tests;

public class MarkdownViewTests
{
    [Fact]
    public void MarkdownView_Wraps_Text_When_Constrained()
    {
        var md = new MarkdownView();
        // A long string that should definitely wrap on a small width
        md.Text = "This is a very long text that should wrap when the available width is small.";

        // Measure with unlimited width
        md.Measure(new Size(int.MaxValue, int.MaxValue));
        var unlimitedWidth = md.DesiredSize.Width;
        var unlimitedHeight = md.DesiredSize.Height;

        // Measure with constrained width (e.g. 10 chars)
        int constrainedWidth = 10;
        md.Measure(new Size(constrainedWidth, int.MaxValue));

        Assert.True(md.DesiredSize.Width <= constrainedWidth, $"Width {md.DesiredSize.Width} should be <= {constrainedWidth}");
        Assert.True(md.DesiredSize.Height > unlimitedHeight, $"Height {md.DesiredSize.Height} should be > {unlimitedHeight} (wrapped)");
    }

    [Fact]
    public void MarkdownView_Is_Not_Scrollable()
    {
        var md = new MarkdownView();
        // Check that it does not inherit from ScrollViewer (by checking type or properties if possible)
        // Since we changed inheritance, this is compile-time check mostly.
        Assert.False(md.GetType().IsSubclassOf(typeof(ScrollViewer)));
    }
}
