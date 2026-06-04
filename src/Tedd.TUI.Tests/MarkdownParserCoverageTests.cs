using System;
using Tedd.TUI;
using Tedd.TUI.Markdown;
using Tedd.TUI.CodeColoring;
using Xunit;
using System.Linq;

namespace Tedd.TUI.Tests
{
    public class MarkdownParserCoverageTests
    {
        [Fact]
        public void Parse_EmptyOrNull_ReturnsEmptyDocument()
        {
            var parser = new MarkdownParser(new MarkdownTheme());
            var doc1 = parser.Parse(null!);
            Assert.Empty(doc1.Children);

            var doc2 = parser.Parse("");
            Assert.Empty(doc2.Children);
        }

        [Fact]
        public void Parse_Headers_ReturnsParagraphsWithStyles()
        {
            var parser = new MarkdownParser(new MarkdownTheme());
            var doc = parser.Parse("# H1\n## H2\n### H3\n#### H4\n##### H5\n###### H6\n####### NotHeader");

            Assert.Equal(7, doc.Children.Count);
            Assert.IsType<Paragraph>(doc.Children[0]);
            Assert.IsType<Paragraph>(doc.Children[5]);
            Assert.IsType<Paragraph>(doc.Children[6]); // Paragraph for "####### NotHeader"

            var p1 = (Paragraph)doc.Children[0];
            Assert.Single(p1.Children);
            Assert.Equal("H1", ((TextBlock)p1.Children[0]).Text.Trim());
        }

        [Fact]
        public void Parse_Paragraphs_AreParsed()
        {
            var parser = new MarkdownParser(new MarkdownTheme());
            var doc = parser.Parse("Para 1\n\nPara 2");

            Assert.Equal(3, doc.Children.Count); // Para 1, Spacer, Para 2
            Assert.IsType<Paragraph>(doc.Children[0]);
            Assert.IsType<TextBlock>(doc.Children[1]); // Spacer
            Assert.IsType<Paragraph>(doc.Children[2]);
        }

        [Fact]
        public void Parse_Lists_AreParsed()
        {
            var parser = new MarkdownParser(new MarkdownTheme());
            var doc = parser.Parse("- Item 1\n* Item 2\n+ Item 3");

            Assert.Single(doc.Children);
            var stack = Assert.IsType<StackPanel>(doc.Children[0]);
            Assert.Equal(3, stack.Children.Count);
        }

        [Fact]
        public void Parse_CodeBlocks_AreParsed()
        {
            var parser = new MarkdownParser(new MarkdownTheme());
            var doc = parser.Parse("```csharp\ncode\n```\n~~~ \nmore code\n~~~");

            Assert.Equal(2, doc.Children.Count);
            Assert.IsType<CodeDocument>(doc.Children[0]);
            Assert.IsType<CodeDocument>(doc.Children[1]);
        }

        [Fact]
        public void Parse_Quotes_AreParsed()
        {
            var parser = new MarkdownParser(new MarkdownTheme());
            var doc = parser.Parse("> Quote\n> line 2");

            Assert.Single(doc.Children);
            var p = Assert.IsType<Paragraph>(doc.Children[0]);
            Assert.True(p.Children.Count >= 2); // Marker + contents
        }

        [Fact]
        public void Parse_Tables_AreParsed()
        {
            var parser = new MarkdownParser(new MarkdownTheme());
            var doc = parser.Parse("| H1 | H2 |\n|---|---|\n| A | B |\n| C |"); // C is short row

            Assert.Single(doc.Children);
            var table = Assert.IsType<Table>(doc.Children[0]);
            Assert.Equal(2, table.Columns.Count);
            Assert.Equal(2, table.Rows.Count);
            Assert.Equal(2, table.Rows[1].Cells.Count); // Padded short row
        }

        [Fact]
        public void Parse_Inline_Link()
        {
            var parser = new MarkdownParser(new MarkdownTheme());
            var doc = parser.Parse("Text with [Link](http://test)");

            var p = Assert.IsType<Paragraph>(doc.Children[0]);
            var link = p.Children.OfType<Hyperlink>().First();
            Assert.Equal("Link", link.Text);
            Assert.Equal("http://test", link.Url);
        }

        [Fact]
        public void Parse_Inline_Image()
        {
            var parser = new MarkdownParser(new MarkdownTheme());
            var doc = parser.Parse("Text with ![Alt](img.png)");

            var p = Assert.IsType<Paragraph>(doc.Children[0]);
            var img = p.Children.OfType<Image>().First();
            Assert.Equal("Alt", img.AltText);
            Assert.Equal("img.png", img.Source);
        }

        [Fact]
        public void Parse_Inline_BoldAndCode()
        {
            var theme = new MarkdownTheme();
            var parser = new MarkdownParser(theme);
            var doc = parser.Parse("Some **bold** and `code` text");

            var p = Assert.IsType<Paragraph>(doc.Children[0]);
            // The split space logic creates a bunch of tokens
            var boldTb = (TextBlock)p.Children.First(c => ((TextBlock)c).Text.Trim() == "bold");
            Assert.Equal(theme.Bold.Foreground, boldTb.Foreground);

            var codeTb = (TextBlock)p.Children.First(c => ((TextBlock)c).Text.Trim() == "code");
            Assert.Equal(theme.CodeSpan.Foreground, codeTb.Foreground);
        }

        [Fact]
        public void Parse_Inline_CodeSingle()
        {
            var parser = new MarkdownParser(new MarkdownTheme());
            var doc = parser.Parse("``` foo ```"); // inline block
            Assert.Single(doc.Children);
            var cd = Assert.IsType<CodeDocument>(doc.Children[0]);
        }

        [Fact]
        public void Parse_Inline_IncompleteLink()
        {
            var parser = new MarkdownParser(new MarkdownTheme());
            var doc = parser.Parse("[Link] (not url)");
            var p = Assert.IsType<Paragraph>(doc.Children[0]);
            // It parses as text because of space
            Assert.IsType<TextBlock>(p.Children[0]);
        }
    }
}
