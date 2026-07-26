using System.Collections.Generic;
using Tedd.TUI;
using Tedd.TUI.Platform.Blazor;

namespace Tedd.TUI.Platform.Blazor.Tests;

/// <summary>
/// Covers the prerender path end to end: a frame rendered with no <c>IJSRuntime</c> and no render
/// loop, turned into markup that contains the whole of a scrolled region rather than the visible
/// slice. This is the situation a server-side render is in — <c>OnAfterRender</c> never runs and
/// JS interop is unavailable — so these tests stand in for what a crawler would receive.
/// </summary>
public class DomPrerenderTests
{
    private const int CharWidth = 10;
    private const int CharHeight = 18;

    private static StackPanel TallContent(int lines)
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical };
        for (int i = 0; i < lines; i++)
        {
            panel.Children.Add(new TextBlock { Text = "LINE" + i, Width = 8, Height = 1 });
        }
        return panel;
    }

    private static TuiWindow WindowWithScroller(int lines) => new TuiWindow
    {
        Content = new ScrollViewer
        {
            Content = TallContent(lines),
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        }
    };

    private static string Render(BlazorTuiApp app, DomRenderer renderer, int w, int h)
    {
        app.RenderStaticFrame(w, h);
        Assert.NotNull(renderer.Layers);
        return new DomGridMarkup().RenderDocument(renderer.Layers!, w, h, CharWidth, CharHeight);
    }

    [Fact]
    public void StaticFrame_EmitsContentWithoutJsInterop()
    {
        // A DomRenderer built without an IJSRuntime is exactly the prerender situation: it must
        // fall back to its default cell metrics rather than reach for tuiInterop.measureDom.
        var renderer = new DomRenderer();
        using var app = new BlazorTuiApp(WindowWithScroller(20), renderer) { PrerenderScrollContent = true };

        string html = Render(app, renderer, 20, 5);

        Assert.Contains("tui-root-container", html);
        Assert.Contains("LINE0", html);
    }

    [Fact]
    public void StaticFrame_IncludesRowsFarBelowTheViewport()
    {
        var renderer = new DomRenderer();
        using var app = new BlazorTuiApp(WindowWithScroller(40), renderer) { PrerenderScrollContent = true };

        string html = Render(app, renderer, 20, 5);

        // Row 39 is 35 rows below a five-row viewport. Without pre-rendering it would never
        // reach the DOM at all.
        Assert.Contains("LINE39", html);
        Assert.Contains("tui-scroll-pane", html);
    }

    [Fact]
    public void PrerenderScrollContentOff_EmitsOnlyTheVisibleRows()
    {
        var renderer = new DomRenderer();
        using var app = new BlazorTuiApp(WindowWithScroller(40), renderer) { PrerenderScrollContent = false };

        string html = Render(app, renderer, 20, 5);

        Assert.DoesNotContain("tui-scroll-pane", html);
        Assert.Contains("LINE0", html);
        Assert.DoesNotContain("LINE39", html);
    }

    [Fact]
    public void ScrollingChangesOnlyTheTransform_NotTheContent()
    {
        var window = WindowWithScroller(40);
        var scroller = (ScrollViewer)window.Content;
        var renderer = new DomRenderer();
        using var app = new BlazorTuiApp(window, renderer) { PrerenderScrollContent = true };

        Render(app, renderer, 20, 5);
        var before = renderer.Layers![0].Buffer.ScrollPanes![0];
        Assert.Equal(0, before.OffsetY);

        scroller.ScrollToVerticalOffset(7);
        Render(app, renderer, 20, 5);
        var after = renderer.Layers![0].Buffer.ScrollPanes![0];

        // The whole point of the pane: scrolling moves the box, it does not re-slice the content.
        Assert.Equal(7, after.OffsetY);
        Assert.Contains("translate(0px, -126px)",
            DomGridMarkup.PaneContentStyle(after, CharWidth, CharHeight));
        Assert.Equal(before.Content.Height, after.Content.Height);
    }

    [Fact]
    public void StaticFrame_LeavesCanvasStyleRenderersOnTheFlatBuffer()
    {
        // A renderer that is not layered still gets a frame, just without panes to compose.
        var renderer = new SingleBufferRenderer();
        using var app = new BlazorTuiApp(WindowWithScroller(20), renderer);

        app.RenderStaticFrame(20, 5);

        Assert.NotNull(renderer.Buffer);
        Assert.Equal(20, renderer.Buffer!.Width);
        Assert.Null(renderer.Buffer.ScrollPanes);
    }

    private sealed class SingleBufferRenderer : IRendererAsync
    {
        public VirtualBuffer? Buffer { get; private set; }

        public Task<(int CharWidth, int CharHeight)> InitAsync(int width, int height) =>
            Task.FromResult((CharWidth, CharHeight));

        public Task RenderAsync(VirtualBuffer buffer)
        {
            Buffer = buffer;
            return Task.CompletedTask;
        }
    }
}
