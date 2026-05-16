using System;
using Xunit;
using Tedd.TUI;
using Tedd.TUI.Markdown;

namespace Tedd.TUI.Tests;

public class ImageLayoutTests
{
    private sealed class FakeResolver : IImageResolver
    {
        public bool TryResolve(string source, string? baseDirectory, out byte[] data, out string? mediaType)
        {
            data = new byte[] { 0 };
            mediaType = "image/png";
            return true;
        }
    }

    private sealed class FakeDecoder : IImageDecoder
    {
        public int Width { get; init; }
        public int Height { get; init; }

        public bool TryDecode(byte[] bytes, out RgbaImage image)
        {
            image = new RgbaImage
            {
                Width = Width,
                Height = Height,
                Pixels = new byte[Width * Height * 4]
            };
            return true;
        }
    }

    private static IDisposable WithDecoders(int pixelWidth, int pixelHeight)
    {
        var prevDecoder = Image.DefaultDecoder;
        var prevResolver = Image.DefaultResolver;
        Image.DefaultDecoder = new FakeDecoder { Width = pixelWidth, Height = pixelHeight };
        Image.DefaultResolver = new FakeResolver();
        return new Restore(() =>
        {
            Image.DefaultDecoder = prevDecoder;
            Image.DefaultResolver = prevResolver;
        });
    }

    private sealed class Restore : IDisposable
    {
        private readonly Action _action;
        public Restore(Action a) { _action = a; }
        public void Dispose() => _action();
    }

    [Fact]
    public void Paragraph_ClampsImage_To_AvailableWidth_PreservingAspectRatio()
    {
        // 800x400 pixel image, default 8x16 cell pixels => natural cell size ~100x25.
        using var _ = WithDecoders(800, 400);

        var img = new Image { Source = "fake://wide.png" };
        var p = new Paragraph();
        p.AddChild(img);

        // Constrain to 40 cells wide.
        p.Measure(new Size(40, int.MaxValue));

        Assert.True(img.DesiredSize.Width <= 40,
            $"Image width {img.DesiredSize.Width} should be clamped to <=40");

        // Aspect-preserving: 800x400 = 2:1, so 40 cells wide should give ~10 cells tall (with cell aspect 8x16, cells are 2x taller).
        // pixels per cell horiz=8, vert=16. Image cells natural = 100x25. After clamp to width=40, height = 25 * 40/100 = 10.
        Assert.Equal(10, img.DesiredSize.Height);
    }

    [Fact]
    public void Paragraph_LeavesImage_AtNaturalSize_When_LineIsWideEnough()
    {
        using var _ = WithDecoders(80, 80); // natural cells 10x5

        var img = new Image { Source = "fake://small.png" };
        var p = new Paragraph();
        p.AddChild(img);

        p.Measure(new Size(120, int.MaxValue));

        Assert.Equal(10, img.DesiredSize.Width);
        Assert.Equal(5, img.DesiredSize.Height);
    }

    [Fact]
    public void MarkdownView_ClampsImage_FromMarkdownSource()
    {
        using var _ = WithDecoders(1024, 768);

        var md = new MarkdownView { Text = "![alt](fake://img.png)" };
        // Force parse and layout at a constrained width.
        md.Measure(new Size(80, int.MaxValue));

        // The view's desired width must not exceed the constraint (i.e. the image
        // didn't blow past the line width).
        Assert.True(md.DesiredSize.Width <= 80,
            $"MarkdownView width {md.DesiredSize.Width} should be clamped to <=80");
    }

    /// <summary>
    /// Repro for the actual user-visible bug: a wide code block (e.g. a long C# line)
    /// makes the FlowDocument report a width &gt; screen, and Border.ArrangeOverride
    /// then arranges content at that wider width. The image inside must still be
    /// clamped to the original measured width — its render rect must not extend past
    /// the screen, and after arrange its RenderSize.Width must equal what was
    /// computed during measure.
    /// </summary>
    [Fact]
    public void MarkdownView_ImageStaysClamped_WhenSiblingForcesWideArrange()
    {
        using var _ = WithDecoders(2400, 1200);

        const int screenWidth = 80;

        // A long code line forces the document's reported desired width to be
        // significantly wider than the screen (CodeDocument is a vertical stack of
        // horizontal stacks, with no wrapping).
        string longCodeLine = new string('A', 200);
        var md = new MarkdownView
        {
            Text = "![alt](fake://big.png)\n\n```\n" + longCodeLine + "\n```\n"
        };

        md.Measure(new Size(screenWidth, int.MaxValue));
        // Simulate Border.ArrangeOverride which arranges content at max(viewport, desired).
        int arrangeWidth = Math.Max(screenWidth, md.DesiredSize.Width);
        md.Arrange(new Rect(0, 0, arrangeWidth, md.DesiredSize.Height));

        // Find the Image deep in the tree and verify its RenderSize.Width is clamped.
        var doc = (FlowDocument)md.GetVisualChild(0);
        var p = (Paragraph)doc.GetVisualChild(0);
        Image? imgInTree = null;
        for (int i = 0; i < p.VisualChildrenCount; i++)
        {
            if (p.GetVisualChild(i) is Image found) { imgInTree = found; break; }
        }
        Assert.NotNull(imgInTree);

        Assert.True(imgInTree!.RenderSize.Width <= screenWidth,
            $"Image RenderSize.Width {imgInTree.RenderSize.Width} should stay <= screen width {screenWidth} even when siblings force a wider arrange.");
    }

    /// <summary>
    /// End-to-end repro mimicking the BlogTUI PostViewer setup: outer Border with
    /// double box style, vertical StackPanel inside, then the MarkdownView. With a
    /// wide code block the inner content width grows past the viewport (Border
    /// arranges content at max(viewport, desired)). The image must still be drawn
    /// only within the screen viewport.
    /// </summary>
    [Fact]
    public void PostViewerLayout_KeepsImage_WithinScreenViewport()
    {
        using var _ = WithDecoders(2400, 1200);

        const int screenWidth = 80;
        const int screenHeight = 30;

        string longCodeLine = new string('A', 200);
        var md = new MarkdownView
        {
            Text = "![alt](fake://big.png)\n\n```\n" + longCodeLine + "\n```\n"
        };

        var stack = new StackPanel { Orientation = Orientation.Vertical };
        stack.AddChild(md);

        var border = new Border { BoxStyle = BoxStyle.Double, Content = stack };
        var window = new TuiWindow { Content = border };

        window.Measure(new Size(screenWidth, screenHeight));
        window.Arrange(new Rect(0, 0, screenWidth, screenHeight));

        // Find the Image and verify it sits entirely within the Border's content rect.
        var doc = (FlowDocument)md.GetVisualChild(0);
        var p = (Paragraph)doc.GetVisualChild(0);
        Image? img = null;
        for (int i = 0; i < p.VisualChildrenCount; i++)
        {
            if (p.GetVisualChild(i) is Image f) { img = f; break; }
        }
        Assert.NotNull(img);

        // Image is arranged in coordinates relative to Paragraph; the actual on-screen
        // X depends on the absolute offset of all parents. The simplest invariant: the
        // image's measured/arranged width must not exceed the inner viewport (border
        // takes 2 columns -> 78 cells of content area).
        Assert.True(img!.RenderSize.Width <= screenWidth - 2,
            $"Image RenderSize.Width {img.RenderSize.Width} must fit inside the Border viewport (<= {screenWidth - 2}).");
    }
}
