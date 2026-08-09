using System;
using System.Text;
using Xunit;
using Tedd.TUI;
using Tedd.TUI.Markdown;
using Tedd.TUI.CodeColoring;
using Tedd.TUI.Media;

namespace Tedd.TUI.Tests;

/// <summary>
/// Covers the fenced-code-block container: language title, height cap, and the
/// hover "Copy" button.
/// Shares the "ClipboardState" collection because <see cref="Clipboard"/> is static.
/// </summary>
[Collection("ClipboardState")]
public class MarkdownCodeBlockTests : IDisposable
{
    public MarkdownCodeBlockTests()
    {
        Clipboard.Provider = null;
        Clipboard.SetText(string.Empty);
    }

    public void Dispose()
    {
        Clipboard.Provider = null;
        Clipboard.SetText(string.Empty);
    }
    private static MarkdownCodeBlock ParseSingleBlock(string markdown, int w = 40, int h = 40)
    {
        var parser = new MarkdownParser(new MarkdownTheme());
        var doc = parser.Parse(markdown);
        var block = Assert.IsType<MarkdownCodeBlock>(doc.Children[0]);
        doc.Measure(new Size(w, h));
        doc.Arrange(new Rect(0, 0, w, h));
        return block;
    }

    [Fact]
    public void Language_Becomes_The_Frame_Title()
    {
        var block = ParseSingleBlock("```csharp\nvar x = 1;\n```");
        var title = Assert.IsType<TextBlock>(block.Title);
        Assert.Contains("csharp", title.Text);
    }

    [Fact]
    public void Short_Block_Fits_Its_Line_Count()
    {
        // 3 code lines + top/bottom border = 5 rows.
        var block = ParseSingleBlock("```csharp\na;\nb;\nc;\n```");
        Assert.Equal(5, block.RenderSize.Height);
    }

    [Fact]
    public void Tall_Block_Is_Capped_To_MaxVisibleLines()
    {
        var sb = new StringBuilder("```csharp\n");
        for (int i = 0; i < 40; i++) sb.Append("line;\n");
        sb.Append("```");
        var block = ParseSingleBlock(sb.ToString(), 40, 60);
        // 15 visible lines + 2 border rows.
        Assert.Equal(17, block.RenderSize.Height);
    }

    [Fact]
    public void Copy_Button_Copies_Raw_Code_When_Hovered_And_Clicked()
    {
        Clipboard.SetText("stale");
        var block = ParseSingleBlock("```csharp\nvar x = 1;\nvar y = 2;\n```");
        block.IsMouseOver = true;

        // Render so the copy button computes its hit region.
        var buf = new VirtualBuffer(40, 40);
        block.Render(buf, 0, 0);

        // Click inside the top-right "Copy" region: " Copy " is 6 wide, ending one cell
        // before the corner.
        int w = block.RenderSize.Width;
        int clickX = w - 1 - 3; // middle of the label
        block.OnMouseDown(new MouseEventArgs(UIElement.MouseDownEvent) { X = clickX, Y = 0 });

        Assert.Equal("var x = 1;\nvar y = 2;", Clipboard.GetText());
    }

    [Fact]
    public void Copy_Button_Is_Not_Drawn_When_Not_Hovered()
    {
        var block = ParseSingleBlock("```csharp\nvar x = 1;\n```");
        // Not hovered.
        var buf = new VirtualBuffer(40, 40);
        block.Render(buf, 0, 0);

        int w = block.RenderSize.Width;
        // Scan the top border for the word "Copy"; it must be absent.
        var top = new StringBuilder();
        for (int x = 0; x < w; x++) top.Append(buf.GetPixel(x, 0).Character);
        Assert.DoesNotContain("Copy", top.ToString());
    }
}
