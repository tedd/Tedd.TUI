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

    [Fact]
    public void MarkdownView_Parses_Multiline_Quotes_With_Spaces()
    {
        var md = new MarkdownView();
        md.Text = "> Line 1\n> Line 2";
        md.Refresh(); // Force parse

        var doc = (FlowDocument)md.GetVisualChild(0);
        // Doc children: Quote block -> Paragraph
        // Depending on how FlowDocument stores children (it inherits UIElement but usually has a collection)
        // FlowDocument.AddChild adds to a collection, but usually it exposes them via GetVisualChild if it implements it correctly.
        // Assuming FlowDocument implements GetVisualChild.

        var p = (Paragraph)doc.GetVisualChild(0);

        var text = "";
        for (int i = 0; i < p.VisualChildrenCount; i++)
        {
            var child = p.GetVisualChild(i);
            if (child is TextBlock tb)
            {
                text += tb.Text;
            }
        }

        // Expected: "| Line 1 Line 2" (Marker is "| ")
        Assert.Contains("Line 1 Line 2", text);
    }
}
