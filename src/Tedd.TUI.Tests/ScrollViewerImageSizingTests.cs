using System;
using Xunit;
using Tedd.TUI;
using Tedd.TUI.Markdown;

namespace Tedd.TUI.Tests;

/// <summary>
/// Pins down the layout contract a scrollable region must give its content: the width it hands
/// out already excludes whatever column a shown vertical scrollbar occupies, so an image (or
/// text) sized to "fit available width" never ends up sized wider than the viewport actually is.
/// Height stays unconstrained on purpose -- that is what makes vertical scrolling meaningful --
/// so an image or a paragraph is expected to extend past the bottom of the viewport when the
/// document is taller than it; <see cref="GraphicClippingTests"/> covers that being clipped
/// rather than painted over whatever the viewport doesn't own.
/// </summary>
[Collection(ImageTestCollection.Name)]
public class ScrollViewerImageSizingTests
{
    private sealed class WideResolver : IImageResolver
    {
        public bool TryResolve(string source, string? baseDirectory, out byte[] data, out string? mediaType)
        {
            data = new byte[] { 1, 2, 3 };
            mediaType = "image/png";
            return true;
        }
    }

    private sealed class WideDecoder : IImageDecoder
    {
        public int Width { get; init; }
        public int Height { get; init; }

        public bool TryDecode(byte[] bytes, out RgbaImage image)
        {
            image = new RgbaImage { Width = Width, Height = Height, Pixels = new byte[Width * Height * 4] };
            return true;
        }
    }

    private static IDisposable WithImageHooks(int pixelW, int pixelH)
    {
        var prevDec = Image.DefaultDecoder;
        var prevRes = Image.DefaultResolver;
        Image.DefaultDecoder = new WideDecoder { Width = pixelW, Height = pixelH };
        Image.DefaultResolver = new WideResolver();
        return new Disposable(() => { Image.DefaultDecoder = prevDec; Image.DefaultResolver = prevRes; });
    }

    private sealed class Disposable : IDisposable
    {
        private readonly Action _a;
        public Disposable(Action a) { _a = a; }
        public void Dispose() => _a();
    }

    /// <summary>
    /// An image far wider (in cells) than the viewport, wrapped the way the markdown reader
    /// wraps one: Image inside a Paragraph inside a FlowDocument inside a ScrollViewer, with
    /// nothing set beyond the defaults (Vertical=Visible, Horizontal=Disabled). Nothing here
    /// asks for horizontal scrolling, so the image must be laid out to fit the width that is
    /// actually left once the scrollbar's column is reserved -- not clipped to look right, sized
    /// to be right.
    /// </summary>
    [Fact]
    public void DefaultScrollViewer_ShrinksWideImageToFitBesideTheScrollbar()
    {
        // 200 cells wide at the default 8x16 text-only cell metrics -- far wider than any
        // reasonable viewport, and with no MaxCellWidth set on the control to do this for us.
        using var _ = WithImageHooks(pixelW: 200 * 8, pixelH: 20 * 16);

        var img = new Image { Source = "wide.png" };
        var paragraph = new Paragraph();
        paragraph.AddChild(img);

        // MarkdownView.MeasureOverride is a pure pass-through to its FlowDocument (a vertical
        // StackPanel, which hands children the available width unchanged), so exercising
        // Paragraph/Image directly against the viewport width the ScrollViewer hands down
        // covers the same contract without needing MarkdownView's private document plumbing.
        var viewer = new ScrollViewer
        {
            Content = paragraph,
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };
        viewer.Measure(new Size(70, 15));
        viewer.Arrange(new Rect(0, 0, 70, 15));

        // 70 cells available, minus the one column the vertical scrollbar reserves.
        Assert.True(img.RenderSize.Width <= 69,
            $"image width {img.RenderSize.Width} exceeds the 69-cell viewport left after the scrollbar column");
        Assert.True(img.RenderSize.Width > 0);
    }

    /// <summary>Same shape, but the scrollbar is Auto rather than forced Visible.</summary>
    [Fact]
    public void AutoVerticalScrollbar_StillReservesItsColumnForTheImage()
    {
        using var _ = WithImageHooks(pixelW: 200 * 8, pixelH: 60 * 16); // tall enough to force Auto on

        var img = new Image { Source = "wide.png" };
        var paragraph = new Paragraph();
        paragraph.AddChild(img);

        var viewer = new ScrollViewer
        {
            Content = paragraph,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };
        viewer.Measure(new Size(70, 15));
        viewer.Arrange(new Rect(0, 0, 70, 15));

        Assert.True(viewer.IsVerticalScrollBarShown, "the tall image should have forced Auto to show");
        Assert.True(img.RenderSize.Width <= 69,
            $"image width {img.RenderSize.Width} exceeds the 69-cell viewport left after the scrollbar column");
    }

    /// <summary>
    /// Text is not exempt from the same reservation: a run of words must wrap inside the
    /// scrollbar-reduced width, not the raw control width.
    /// </summary>
    [Fact]
    public void DefaultScrollViewer_WrapsTextBesideTheScrollbar()
    {
        var paragraph = new Paragraph();
        // One long unbroken token wider than the viewport left after the scrollbar column, so
        // the wrap point itself proves the width Paragraph measured against.
        paragraph.AddChild(new TextBlock { Text = new string('x', 75) });

        var viewer = new ScrollViewer
        {
            Content = paragraph,
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };
        viewer.Measure(new Size(70, 15));
        viewer.Arrange(new Rect(0, 0, 70, 15));

        Assert.True(paragraph.DesiredSize.Width <= 69,
            $"paragraph width {paragraph.DesiredSize.Width} exceeds the 69-cell viewport left after the scrollbar column");
    }

    /// <summary>
    /// Border's scrollbar rides the border line rather than stealing a content column, so its
    /// content width is the padded interior width, unreduced -- confirms that model still holds
    /// and isn't accidentally widened.
    /// </summary>
    [Fact]
    public void BorderWithVerticalScrollbar_BoundsImageToThePaddedInterior()
    {
        using var _ = WithImageHooks(pixelW: 200 * 8, pixelH: 20 * 16);

        var img = new Image { Source = "wide.png" };
        var paragraph = new Paragraph();
        paragraph.AddChild(img);

        var border = new Border
        {
            Child = paragraph,
            BoxStyle = BoxStyle.Single,
            VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };
        border.Measure(new Size(70, 15));
        border.Arrange(new Rect(0, 0, 70, 15));

        // Border.MeasureOverride's insetW is 2 (border columns) + Padding.Left + Padding.Right;
        // Padding defaults to Thickness(1), so 2 + 1 + 1 = 4 reserved, not the border-plus-padding
        // total on each side -- 70 - 4 = 66.
        Assert.True(img.RenderSize.Width <= 66,
            $"image width {img.RenderSize.Width} exceeds the 66-cell interior");
    }
}
