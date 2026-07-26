using System.Collections.Generic;
using Tedd.TUI;
using Tedd.TUI.Platform.Blazor;

namespace Tedd.TUI.Platform.Blazor.Tests;

/// <summary>
/// Covers the HTML the DOM surface emits. <see cref="DomGridMarkup"/> holds every piece of it
/// outside any Razor component precisely so the pixel math, the run coalescing, the escaping and
/// the row cache can be asserted directly.
/// </summary>
public class DomGridMarkupTests
{
    private const int CharWidth = 10;
    private const int CharHeight = 18;

    private static VirtualBuffer BufferWith(string text)
    {
        var buffer = new VirtualBuffer(text.Length, 1);
        buffer.DrawString(0, 0, text, TuiColor.White, TuiColor.Black);
        return buffer;
    }

    [Fact]
    public void Row_CoalescesCellsSharingColorsIntoOneSpan()
    {
        var markup = new DomGridMarkup();
        var buffer = BufferWith("abc");

        string html = markup.GetRowHtml(0, 0, buffer, CharHeight);

        Assert.Equal(1, CountOccurrences(html, "<span"));
        Assert.Contains(">abc</span>", html);
        Assert.StartsWith("<div class=\"tui-row\" style=\"height: 18px;\">", html);
    }

    [Fact]
    public void Row_SplitsSpansWhenColorsChange()
    {
        var markup = new DomGridMarkup();
        var buffer = BufferWith("ab");
        buffer.SetPixel(1, 0, 'b', TuiColor.Red, TuiColor.Black);

        string html = markup.GetRowHtml(0, 0, buffer, CharHeight);

        Assert.Equal(2, CountOccurrences(html, "<span"));
    }

    [Fact]
    public void Row_EscapesMarkupCharacters()
    {
        var markup = new DomGridMarkup();
        var buffer = BufferWith("<&>");

        string html = markup.GetRowHtml(0, 0, buffer, CharHeight);

        Assert.Contains("&lt;&amp;&gt;", html);
    }

    [Fact]
    public void Row_EmitsRgbaSoAlphaSurvives()
    {
        var markup = new DomGridMarkup();
        var buffer = new VirtualBuffer(1, 1);
        buffer.Clear(TuiColor.Transparent);

        string html = markup.GetRowHtml(0, 0, buffer, CharHeight);

        Assert.Contains("background-color: rgba(0,0,0,0)", html);
    }

    // The cache's value is the string *identity* it preserves: Blazor's diff compares
    // MarkupString by value, so a fresh-but-equal string still patches the DOM.
    [Fact]
    public void Row_ReturnsTheSameInstanceWhenUnchanged()
    {
        var markup = new DomGridMarkup();
        var buffer = BufferWith("abc");

        string first = markup.GetRowHtml(0, 0, buffer, CharHeight);
        string second = markup.GetRowHtml(0, 0, buffer, CharHeight);

        Assert.Same(first, second);
    }

    [Fact]
    public void Row_ReturnsANewInstanceWhenCellsChange()
    {
        var markup = new DomGridMarkup();
        var buffer = BufferWith("abc");

        string first = markup.GetRowHtml(0, 0, buffer, CharHeight);
        buffer.SetPixel(0, 0, 'z', TuiColor.White, TuiColor.Black);
        string second = markup.GetRowHtml(0, 0, buffer, CharHeight);

        Assert.NotSame(first, second);
        Assert.Contains("zbc", second);
    }

    [Fact]
    public void Row_DoesNotConfuseTwoScopesAtTheSameRow()
    {
        var markup = new DomGridMarkup();
        var a = BufferWith("aaa");
        var b = BufferWith("bbb");

        string rowA = markup.GetRowHtml(0, 0, a, CharHeight);
        string rowB = markup.GetRowHtml(1, 0, b, CharHeight);

        Assert.Contains("aaa", rowA);
        Assert.Contains("bbb", rowB);
    }

    [Fact]
    public void PaneStyle_ClipsToTheViewportRectInPixels()
    {
        var pane = new ScrollPane
        {
            Viewport = new Rect(2, 3, 7, 4),
            Content = new VirtualBuffer(7, 20),
        };

        string style = DomGridMarkup.PaneStyle(pane, CharWidth, CharHeight);

        Assert.Contains("left: 20px", style);   // 2 cells
        Assert.Contains("top: 54px", style);    // 3 rows
        Assert.Contains("width: 70px", style);  // 7 cells
        Assert.Contains("height: 72px", style); // 4 rows
        Assert.Contains("overflow: hidden", style);
    }

    [Fact]
    public void PaneContentStyle_TranslatesByWholeCellsAndSizesToTheExtent()
    {
        var pane = new ScrollPane
        {
            Viewport = new Rect(0, 0, 7, 4),
            Content = new VirtualBuffer(7, 20),
            OffsetX = 1,
            OffsetY = 5,
        };

        string style = DomGridMarkup.PaneContentStyle(pane, CharWidth, CharHeight);

        // Whole-cell translation is what reproduces the TUI's line-by-line and page steps.
        Assert.Contains("transform: translate(-10px, -90px)", style);
        Assert.Contains("height: 360px", style); // the full 20-row extent, not the 4-row viewport
    }

    [Fact]
    public void Document_ContainsOffScreenPaneContent()
    {
        // The crawler-visibility claim, asserted directly: rows scrolled out of the viewport
        // are present in the emitted HTML.
        var content = new VirtualBuffer(6, 20);
        content.DrawString(0, 19, "BOTTOM", TuiColor.White, TuiColor.Black);

        var layerBuffer = new VirtualBuffer(8, 4)
        {
            ScrollPanes = new List<ScrollPane>
            {
                new ScrollPane
                {
                    Viewport = new Rect(0, 0, 6, 4),
                    Content = content,
                }
            }
        };

        var markup = new DomGridMarkup();
        string html = markup.RenderDocument(
            new List<RenderLayer> { new RenderLayer { Buffer = layerBuffer, X = 0, Y = 0, ZIndex = 0 } },
            8, 4, CharWidth, CharHeight);

        Assert.Contains("BOTTOM", html);
        Assert.Contains("tui-scroll-pane", html);
        Assert.Contains("tui-scroll-content", html);
    }

    [Fact]
    public void Document_NestsPanesInsideTheirParent()
    {
        var innerContent = new VirtualBuffer(4, 12);
        innerContent.DrawString(0, 11, "DEEP", TuiColor.White, TuiColor.Black);

        var outerContent = new VirtualBuffer(6, 10)
        {
            ScrollPanes = new List<ScrollPane>
            {
                new ScrollPane { Viewport = new Rect(1, 1, 4, 3), Content = innerContent }
            }
        };

        var layerBuffer = new VirtualBuffer(8, 4)
        {
            ScrollPanes = new List<ScrollPane>
            {
                new ScrollPane { Viewport = new Rect(0, 0, 6, 4), Content = outerContent }
            }
        };

        var markup = new DomGridMarkup();
        string html = markup.RenderDocument(
            new List<RenderLayer> { new RenderLayer { Buffer = layerBuffer, X = 0, Y = 0, ZIndex = 0 } },
            8, 4, CharWidth, CharHeight);

        Assert.Equal(2, CountOccurrences(html, "tui-scroll-pane"));
        Assert.Contains("DEEP", html);
        // The inner pane opens after the outer one and closes before it.
        Assert.True(html.IndexOf("DEEP") > html.IndexOf("tui-scroll-pane"));
        AssertBalancedDivs(html);
    }

    [Fact]
    public void Document_IsWellFormedWithoutPanes()
    {
        var buffer = new VirtualBuffer(4, 2);
        var markup = new DomGridMarkup();

        string html = markup.RenderDocument(
            new List<RenderLayer> { new RenderLayer { Buffer = buffer, X = 0, Y = 0, ZIndex = 0 } },
            4, 2, CharWidth, CharHeight);

        Assert.Contains("tui-root-container", html);
        Assert.Contains("tui-layer", html);
        Assert.Equal(2, CountOccurrences(html, "tui-row"));
        AssertBalancedDivs(html);
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        int count = 0, index = 0;
        while ((index = haystack.IndexOf(needle, index)) >= 0)
        {
            count++;
            index += needle.Length;
        }
        return count;
    }

    private static void AssertBalancedDivs(string html) =>
        Assert.Equal(CountOccurrences(html, "<div"), CountOccurrences(html, "</div>"));
}
