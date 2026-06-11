using Xunit;
using Tedd.TUI;
using Tedd.TUI.Markdown;
using System;

namespace Tedd.TUI.Tests.Markdown;

public class MarkdownParserCoverageTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void MarkdownParser_EmptyInputs_ReturnEmptyDocument(string? input)
    {
        var parser = new MarkdownParser(new MarkdownTheme());
        var doc = parser.Parse(input);
        Assert.Equal(0, doc.VisualChildrenCount);
    }

    [Theory]
    [InlineData("\n\n")]
    public void MarkdownParser_UnknownBlock_ReturnsDocument(string input)
    {
        var parser = new MarkdownParser(new MarkdownTheme());
        var doc = parser.Parse(input);
        Assert.NotNull(doc);
        Assert.Equal(0, doc.VisualChildrenCount);
    }

    [Theory]
    [InlineData("# H1\n## H2", 2)]
    [InlineData("### H3\n#### H4\n##### H5", 3)]
    [InlineData("###### H6\n####### H7", 2)]
    public void MarkdownParser_HeaderStyles_ParsedSuccessfully(string input, int expectedParagraphs)
    {
        var parser = new MarkdownParser(new MarkdownTheme());
        var doc = parser.Parse(input);
        Assert.Equal(expectedParagraphs, doc.VisualChildrenCount);
    }

    [Theory]
    [InlineData("Test `code` and **bold** and [link](url) and ![img](url)")]
    [InlineData("Test `missing and **missing and [link(url and ![img(url")]
    public void MarkdownParser_RenderInline_ParsesContent(string input)
    {
        var parser = new MarkdownParser(new MarkdownTheme());
        var doc = parser.Parse(input);
        Assert.Equal(1, doc.VisualChildrenCount);
    }

    [Theory]
    [InlineData("- Item 1\n* Item 2\n+ Item 3", 1)]
    public void MarkdownParser_Lists_ParsesListBlock(string input, int expectedCount)
    {
        var parser = new MarkdownParser(new MarkdownTheme());
        var doc = parser.Parse(input);
        Assert.Equal(expectedCount, doc.VisualChildrenCount);
    }

    [Theory]
    [InlineData("| A | B |\n|---|---|\n| 1 | 2 |", 1)]
    [InlineData("A | B\n--- | ---\n1 | 2", 1)]
    [InlineData("A | B\nx | y\n1 | 2", 1)]
    public void MarkdownParser_Table_ParsesTable(string input, int expectedCount)
    {
        var parser = new MarkdownParser(new MarkdownTheme());
        var doc = parser.Parse(input);
        Assert.Equal(expectedCount, doc.VisualChildrenCount);
    }

    [Theory]
    [InlineData("```csharp\ncode\n```")]
    [InlineData("~~~csharp\ncode\n~~~")]
    [InlineData("``` single ```")]
    [InlineData("```\ncode ```")]
    public void MarkdownParser_CodeFence_ParsesSuccessfully(string input)
    {
        var parser = new MarkdownParser(new MarkdownTheme());
        var doc = parser.Parse(input);
        Assert.Equal(1, doc.VisualChildrenCount);
    }

    [Theory]
    [InlineData("> quote\n> line 2\n\n> another quote", 3)]
    public void MarkdownParser_Quotes_ParsesQuoteBlocks(string input, int expectedCount)
    {
        var parser = new MarkdownParser(new MarkdownTheme());
        var doc = parser.Parse(input);
        Assert.Equal(expectedCount, doc.VisualChildrenCount);
    }

    [Theory]
    [InlineData("Para 1\n\n\n\nPara 2", 3)]
    public void MarkdownParser_ConsecutiveBlanks_ProducesSpacerBlocks(string input, int expectedCount)
    {
        var parser = new MarkdownParser(new MarkdownTheme());
        var doc = parser.Parse(input);
        Assert.Equal(expectedCount, doc.VisualChildrenCount);
    }

    [Theory]
    [InlineData("[link](url")]
    public void MarkdownParser_LinkNoClosingParen_RendersText(string input)
    {
        var parser = new MarkdownParser(new MarkdownTheme());
        var doc = parser.Parse(input);
        Assert.Equal(1, doc.VisualChildrenCount);
    }

    [Theory]
    [InlineData("| A |\n|---|\n| 1 |")]
    public void MarkdownParser_TableWithThemeBackground_AppliesTheme(string input)
    {
        var theme = new MarkdownTheme();
        theme.Table.HeaderBackground = TuiColor.Blue;
        theme.Table.CellForeground = null;
        theme.Table.CellBackground = null;

        var parser = new MarkdownParser(theme);
        var doc = parser.Parse(input);
        Assert.Equal(1, doc.VisualChildrenCount);

        var table = doc.GetVisualChild(0) as Table;
        Assert.NotNull(table);
        Assert.Equal(TuiColor.Blue, table.HeaderBackground);
    }
}
