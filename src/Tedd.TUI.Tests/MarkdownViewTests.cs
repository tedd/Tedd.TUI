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

    // ATX heading parsing: must have a space after the # chars to qualify as a
    // heading. Otherwise lines like CSS selectors (#myId { ... }) get rendered
    // as bold magenta H1s, which is what the WordPress-exported markdown looked
    // like before this fix.
    //
    // Both headings and paragraphs are rendered as Paragraph elements; we
    // distinguish them by the foreground color applied to their text children
    // (Header1 = Magenta by default, body Paragraph = Gray).

    private static TuiColor? FirstTextForeground(UIElement element)
    {
        if (element is not Paragraph p) return null;
        for (int i = 0; i < p.VisualChildrenCount; i++)
        {
            if (p.GetVisualChild(i) is TextBlock tb)
                return tb.Foreground;
        }
        return null;
    }

    [Fact]
    public void MarkdownView_HashWithoutSpace_IsNotHeading()
    {
        var md = new MarkdownView();
        md.Text = "#arrayTable1 {";
        md.Refresh();

        var doc = (FlowDocument)md.GetVisualChild(0);
        var first = doc.GetVisualChild(0);

        // The default Header1 color is Magenta and body Paragraph is Gray.
        // The CSS selector must be styled as body text, NOT as a heading.
        Assert.NotEqual<TuiColor?>(TuiColor.Magenta, FirstTextForeground(first));
    }

    [Fact]
    public void MarkdownView_HashWithSpace_IsHeading()
    {
        var md = new MarkdownView();
        md.Text = "# Real Heading";
        md.Refresh();

        var doc = (FlowDocument)md.GetVisualChild(0);
        var first = doc.GetVisualChild(0);

        // Default H1 color is Magenta; verifies the heading style was applied.
        Assert.Equal<TuiColor?>(TuiColor.Magenta, FirstTextForeground(first));
    }

    [Fact]
    public void MarkdownView_SevenHashes_IsNotHeading()
    {
        var md = new MarkdownView();
        // ATX max level is 6; 7 hashes is not a heading.
        md.Text = "####### nope";
        md.Refresh();

        var doc = (FlowDocument)md.GetVisualChild(0);
        var first = doc.GetVisualChild(0);

        // Should be styled as body text, not any heading color.
        Assert.NotEqual<TuiColor?>(TuiColor.Magenta, FirstTextForeground(first));
        Assert.NotEqual<TuiColor?>(TuiColor.Cyan, FirstTextForeground(first));
        Assert.NotEqual<TuiColor?>(TuiColor.Yellow, FirstTextForeground(first));
    }

    // Table parsing: WordPress export omits the leading | on every row.

    [Fact]
    public void MarkdownView_RecognizesTableWithoutLeadingPipe()
    {
        var md = new MarkdownView();
        md.Text = "Method| Mean| Allocated\n---|---|---\nPlainArray| 43.42 ms| 40 B\nJagged| 82.95 ms| 69 B";
        md.Refresh();

        var doc = (FlowDocument)md.GetVisualChild(0);
        var first = doc.GetVisualChild(0);

        // The block must be a Table, not a Paragraph (which would be the bug).
        Assert.IsType<Table>(first);

        var table = (Table)first;
        Assert.Equal(3, table.Columns.Count);
        Assert.Equal("Method", table.Columns[0].Header);
        Assert.Equal("Mean", table.Columns[1].Header);
        Assert.Equal("Allocated", table.Columns[2].Header);
    }

    [Fact]
    public void MarkdownView_RecognizesGfmStrictTable()
    {
        // GFM-strict form (with leading and trailing | on every row) must keep working.
        var md = new MarkdownView();
        md.Text = "| Col1 | Col2 |\n|------|------|\n| a    | b    |\n| c    | d    |";
        md.Refresh();

        var doc = (FlowDocument)md.GetVisualChild(0);
        var first = doc.GetVisualChild(0);

        Assert.IsType<Table>(first);
        var table = (Table)first;
        Assert.Equal(2, table.Columns.Count);
        Assert.Equal("Col1", table.Columns[0].Header);
        Assert.Equal("Col2", table.Columns[1].Header);
    }

    [Fact]
    public void MarkdownView_TablePreservesEmptyCells()
    {
        // The middle cell is intentionally empty -- must not be silently dropped,
        // which would shift "c" into the second column.
        var md = new MarkdownView();
        md.Text = "| A | B | C |\n|---|---|---|\n| a |   | c |";
        md.Refresh();

        var doc = (FlowDocument)md.GetVisualChild(0);
        var table = (Table)doc.GetVisualChild(0);

        // Row count check
        Assert.Equal(1, table.Rows.Count);
        var row = table.Rows[0];
        Assert.Equal(3, row.Cells.Count);
    }

    [Fact]
    public void MarkdownView_TablePadsShortRowsToColumnCount()
    {
        // WordPress sometimes drops the trailing empty cell when the source row
        // ends with `... |   ` -- after trimming, that's a row shorter than the
        // header by one cell. The parser pads with empty strings so columns
        // line up.
        var md = new MarkdownView();
        md.Text = "Port| Protocol| Name| Notes\n---|---|---|---\n53| TCP| DNS|";
        md.Refresh();

        var doc = (FlowDocument)md.GetVisualChild(0);
        var table = (Table)doc.GetVisualChild(0);

        Assert.Equal(4, table.Columns.Count);
        Assert.Equal(1, table.Rows.Count);
        Assert.Equal(4, table.Rows[0].Cells.Count);
    }

    [Fact]
    public void MarkdownView_LineWithSinglePipe_IsNotTable()
    {
        // A paragraph that happens to contain a pipe (e.g. shell command syntax)
        // must not be hijacked into the table parser.
        var md = new MarkdownView();
        md.Text = "Run `cmd | grep foo` to filter output.";
        md.Refresh();

        var doc = (FlowDocument)md.GetVisualChild(0);
        var first = doc.GetVisualChild(0);

        Assert.IsNotType<Table>(first);
    }
}
