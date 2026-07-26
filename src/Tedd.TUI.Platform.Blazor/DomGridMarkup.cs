using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Tedd.TUI.Platform.Blazor;

// Intent: own every piece of HTML the DOM surface emits, outside of any Razor component.
// Why:
// - TuiDomGrid used to build its markup inline, which left the pixel math, the run coalescing,
//   the escaping and the row cache untestable without a component-test framework.
// - The prerender path needs the same markup as a plain string, with no renderer running.
// Constraints/Invariants:
// - GetRowHtml must return the *same string instance* for an unchanged row. Blazor's diff
//   compares MarkupString by value, so a fresh-but-equal string still patches the DOM; handing
//   back the previous instance is what lets a frame touch only the rows that really changed.
// - Scope ids must be stable across frames for that cache to hit. Callers assign them by
//   deterministic traversal order (BeginFrame + NextScope), not by content.
// - Structural wrappers are exposed as *style strings*, not as open/close tag pairs: TuiDomGrid
//   renders them as real Razor elements so children nest inside them. An unbalanced MarkupString
//   would not -- Blazor inserts each markup node as its own fragment, so a dangling <div> in one
//   node never wraps the nodes that follow it.
// Failure modes:
// - A scope id that shifts between frames silently disables the row cache (correct output, full
//   repaint). A duplicated scope id would serve one row's markup for another.
// Verification: src/Tedd.TUI.Platform.Blazor.Tests/DomGridMarkupTests.cs
public sealed class DomGridMarkup
{
    private readonly StringBuilder _sb = new StringBuilder();

    // Per-(scope,row) markup cache. The value of this cache is the string *identity* it
    // preserves, not the string building it avoids.
    private readonly Dictionary<long, string> _rowHtmlCache = new();

    // Keyed by the byte[] reference identity so the cache only invalidates when the underlying
    // image data changes. Base64 encoding is otherwise quadratic across frames.
    private readonly Dictionary<byte[], string> _imageSrcCache = new(ReferenceEqualityComparer.Instance);

    private int _nextScope;

    /// <summary>
    /// Starts a new frame's traversal. Resets the scope counter so the same structural position
    /// receives the same id it did last frame, which is what keeps the row cache hitting.
    /// </summary>
    public void BeginFrame() => _nextScope = 0;

    /// <summary>Allocates the next scope id — one per layer and one per scroll pane, in traversal order.</summary>
    public int NextScope() => _nextScope++;

    /// <summary>
    /// Builds one buffer row as a `tui-row` div of colour-run spans. Consecutive cells sharing a
    /// foreground and background collapse into a single span.
    /// </summary>
    public string GetRowHtml(int scope, int y, VirtualBuffer buffer, int charHeight)
    {
        _sb.Clear();
        _sb.Append("<div class=\"tui-row\" style=\"height: ").Append(charHeight).Append("px;\">");

        int col = 0;
        while (col < buffer.Width)
        {
            var startCell = buffer.GetPixel(col, y);
            var fg = startCell.Foreground;
            var bg = startCell.Background;

            _sb.Append("<span style=\"display:inline-block; height: ").Append(charHeight)
               .Append("px; color: ");
            AppendHtmlColor(_sb, fg);
            _sb.Append("; background-color: ");
            AppendHtmlColor(_sb, bg);
            _sb.Append(";\">");

            while (col < buffer.Width)
            {
                var current = buffer.GetPixel(col, y);
                if (current.Foreground != fg || current.Background != bg)
                    break;

                AppendHtmlEncoded(_sb, current.Character);
                col++;
            }

            _sb.Append("</span>");
        }

        _sb.Append("</div>");

        string html = _sb.ToString();
        long key = ((long)scope << 32) | (uint)y;
        if (_rowHtmlCache.TryGetValue(key, out var previous) && previous == html)
            return previous;

        _rowHtmlCache[key] = html;
        return html;
    }

    /// <summary>Style for the surface root, sized to the whole cell grid.</summary>
    public static string RootStyle(int width, int height, int charWidth, int charHeight) =>
        "position: relative; width: " + (width * charWidth) + "px; height: " + (height * charHeight)
        + "px; background-color: black; user-select: none;";

    /// <summary>
    /// Style for one composited layer. Carries the font metrics every descendant inherits,
    /// scroll panes included — `white-space: pre` in particular is what stops pre-rendered rows
    /// from wrapping inside a pane.
    /// </summary>
    public static string LayerStyle(int layerX, int layerY, int zIndex, int charWidth, int charHeight) =>
        "position: absolute; left: " + (layerX * charWidth) + "px; top: " + (layerY * charHeight)
        + "px; z-index: " + zIndex + "; font-family: 'Consolas', monospace; line-height: " + charHeight
        + "px; font-size: 16px; white-space: pre; color: white; pointer-events: none;";

    /// <summary>
    /// Style for a pre-rendered scroll region's clipping box, placed at the viewport rect.
    /// </summary>
    /// <remarks>
    /// Viewport coordinates are relative to the nearest positioned ancestor: the layer div for a
    /// top-level pane, the parent pane's content block for a nested one. In both cases that
    /// ancestor sits exactly at the origin of the buffer the viewport is measured in, so the same
    /// multiplication is correct at every depth.
    /// </remarks>
    public static string PaneStyle(ScrollPane pane, int charWidth, int charHeight)
    {
        var v = pane.Viewport;
        return "position: absolute; left: " + (v.X * charWidth) + "px; top: " + (v.Y * charHeight)
             + "px; width: " + (v.Width * charWidth) + "px; height: " + (v.Height * charHeight)
             + "px; overflow: hidden;";
    }

    /// <summary>
    /// Style for the full-extent block inside a scroll pane. Scrolling is a whole-cell translate,
    /// so it lands on exact row boundaries and reproduces the line-by-line and page steps the TUI
    /// applies in text mode.
    /// </summary>
    public static string PaneContentStyle(ScrollPane pane, int charWidth, int charHeight) =>
        "position: absolute; left: 0; top: 0; width: " + (pane.Content.Width * charWidth)
        + "px; height: " + (pane.Content.Height * charHeight)
        + "px; will-change: transform; transform: translate(" + (-pane.OffsetX * charWidth)
        + "px, " + (-pane.OffsetY * charHeight) + "px);";

    /// <summary>
    /// An absolutely positioned bitmap placement, in cell coordinates of its own buffer.
    /// </summary>
    /// <remarks>
    /// A placement the clip stack cut — an image scrolled partly out of its viewport — is wrapped
    /// in an <c>overflow: hidden</c> box covering the visible region, with the image offset back
    /// to its true position inside it. The image keeps its full size, so it is cropped rather
    /// than squashed and its aspect ratio survives.
    /// </remarks>
    public static string ImageHtml(GraphicPlacement g, string src, int charWidth, int charHeight)
    {
        string image =
            "<img src=\"" + src + "\" alt=\"\" style=\"position:absolute; left:"
            + ((g.IsClipped ? g.CharX - g.ClipCharX : g.CharX) * charWidth)
            + "px; top:" + ((g.IsClipped ? g.CharY - g.ClipCharY : g.CharY) * charHeight)
            + "px; width:" + (g.CharWidth * charWidth)
            + "px; height:" + (g.CharHeight * charHeight)
            + "px; pointer-events:none; image-rendering:auto;\" />";

        if (!g.IsClipped)
            return image;

        return "<div class=\"tui-graphic-clip\" style=\"position:absolute; left:" + (g.ClipCharX * charWidth)
             + "px; top:" + (g.ClipCharY * charHeight) + "px; width:" + (g.ClipCharWidth * charWidth)
             + "px; height:" + (g.ClipCharHeight * charHeight)
             + "px; overflow:hidden; pointer-events:none;\">" + image + "</div>";
    }

    /// <summary>
    /// Resolves a placement to an &lt;img&gt; source: a cached data URI for raw bytes, or the
    /// original URL when the source is already fetchable by the browser.
    /// </summary>
    public string GetImageSrc(GraphicPlacement g)
    {
        if (g.ImageData != null && g.ImageData.Length > 0)
        {
            if (!_imageSrcCache.TryGetValue(g.ImageData, out var cached))
            {
                var media = string.IsNullOrEmpty(g.MediaType) ? "application/octet-stream" : g.MediaType;
                cached = $"data:{media};base64,{Convert.ToBase64String(g.ImageData)}";
                _imageSrcCache[g.ImageData] = cached;
            }
            return cached;
        }

        // No bytes resolved but a URL-style source: let the browser fetch it directly.
        if (!string.IsNullOrEmpty(g.Source) &&
            (g.Source.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
             || g.Source.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
             || g.Source.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
             || g.Source.StartsWith("/")))
        {
            return g.Source;
        }
        return string.Empty;
    }

    /// <summary>
    /// Renders a whole frame to a single HTML string from the same style builders and row markup
    /// <c>TuiDomGrid</c> composes as Razor nodes.
    /// </summary>
    /// <remarks>
    /// This is the prerender path: it needs no <c>IJSRuntime</c> and no running render loop, so a
    /// server-side render — and a unit test — can produce exactly the markup a browser would.
    /// Keep the element nesting here in step with <c>TuiDomGrid</c>'s.
    /// </remarks>
    public string RenderDocument(IReadOnlyList<RenderLayer> layers, int width, int height, int charWidth, int charHeight)
    {
        BeginFrame();

        var outp = new StringBuilder();
        outp.Append("<div class=\"tui-root-container\" style=\"")
            .Append(RootStyle(width, height, charWidth, charHeight)).Append("\">");

        for (int i = 0; i < layers.Count; i++)
        {
            var layer = layers[i];
            outp.Append("<div class=\"tui-layer\" style=\"")
                .Append(LayerStyle(layer.X, layer.Y, layer.ZIndex, charWidth, charHeight)).Append("\">");
            AppendBufferContent(outp, layer.Buffer, charWidth, charHeight);
            outp.Append("</div>");
        }

        outp.Append("</div>");
        return outp.ToString();
    }

    /// <summary>
    /// Appends one buffer's rows, bitmaps and nested scroll panes, mirroring the traversal
    /// <c>TuiDomGrid</c> performs — including the order in which scope ids are allocated.
    /// </summary>
    private void AppendBufferContent(StringBuilder outp, VirtualBuffer buffer, int charWidth, int charHeight)
    {
        int scope = NextScope();

        for (int y = 0; y < buffer.Height; y++)
        {
            outp.Append(GetRowHtml(scope, y, buffer, charHeight));
        }

        if (buffer.Graphics != null)
        {
            foreach (var g in buffer.Graphics)
            {
                var src = GetImageSrc(g);
                if (!string.IsNullOrEmpty(src))
                {
                    outp.Append(ImageHtml(g, src, charWidth, charHeight));
                }
            }
        }

        if (buffer.ScrollPanes != null)
        {
            foreach (var pane in buffer.ScrollPanes)
            {
                outp.Append("<div class=\"tui-scroll-pane\" style=\"")
                    .Append(PaneStyle(pane, charWidth, charHeight)).Append("\">");
                outp.Append("<div class=\"tui-scroll-content\" style=\"")
                    .Append(PaneContentStyle(pane, charWidth, charHeight)).Append("\">");

                AppendBufferContent(outp, pane.Content, charWidth, charHeight);

                outp.Append("</div></div>");
            }
        }
    }

    private static void AppendHtmlEncoded(StringBuilder sb, char c)
    {
        switch (c)
        {
            case '<': sb.Append("&lt;"); break;
            case '>': sb.Append("&gt;"); break;
            case '&': sb.Append("&amp;"); break;
            default: sb.Append(c); break;
        }
    }

    private static void AppendHtmlColor(StringBuilder sb, TuiColor color)
    {
        // Always emit rgba(...) so 24-bit color and alpha both survive the trip to CSS.
        sb.Append("rgba(").Append(color.R).Append(',').Append(color.G).Append(',').Append(color.B).Append(',')
          .Append((color.A / 255.0).ToString("0.###", CultureInfo.InvariantCulture)).Append(')');
    }
}
