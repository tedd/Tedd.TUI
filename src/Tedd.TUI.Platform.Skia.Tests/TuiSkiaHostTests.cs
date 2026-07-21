using SkiaSharp;
using Tedd.TUI;
using Tedd.TUI.Platform.Skia;

namespace Tedd.TUI.Platform.Skia.Tests;

public class TuiSkiaHostTests
{
    private static SKColor PixelAt(SKImage image, int x, int y)
    {
        using var bitmap = SKBitmap.FromImage(image);
        return bitmap.GetPixel(x, y);
    }

    [Fact]
    public void RenderToImage_SizeMatchesGrid()
    {
        using var host = new TuiSkiaHost();
        host.SetContent(new TuiWindow());

        var (width, height) = host.SizeForCells(40, 12);
        using var image = host.RenderToImage(40, 12);

        Assert.Equal((int)MathF.Ceiling(width), image.Width);
        Assert.Equal((int)MathF.Ceiling(height), image.Height);
        Assert.Equal(40, host.Columns);
        Assert.Equal(12, host.Rows);
    }

    [Fact]
    public void Render_FillsWindowBackground()
    {
        using var host = new TuiSkiaHost();
        host.SetContent(new TuiWindow { Background = TuiColor.FromRgb(255, 0, 0) });

        using var image = host.RenderToImage(20, 5);
        var center = PixelAt(image, image.Width / 2, image.Height / 2);

        Assert.Equal(new SKColor(255, 0, 0), center);
    }

    [Fact]
    public void Render_ClearsAreaOutsideGridWithHostBackground()
    {
        using var host = new TuiSkiaHost { Background = SKColors.Blue };
        host.SetContent(new TuiWindow { Background = TuiColor.FromRgb(255, 0, 0) });

        // A surface slightly wider/taller than a whole number of cells leaves a band
        // on the right/bottom that only the host background paints.
        var (gridWidth, gridHeight) = host.SizeForCells(10, 4);
        int width = (int)MathF.Ceiling(gridWidth) + 7;
        int height = (int)MathF.Ceiling(gridHeight) + 5;
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        host.Render(surface.Canvas, width, height);
        using var image = surface.Snapshot();

        Assert.Equal(SKColors.Blue, PixelAt(image, width - 1, height - 1));
    }

    [Fact]
    public void MouseClick_RaisesButtonClick()
    {
        using var host = new TuiSkiaHost();
        bool clicked = false;
        var button = new Button { Content = "OK" };
        button.Click += (_, _) => clicked = true;
        host.SetContent(new TuiWindow { Content = button });

        using var _ = host.RenderToImage(20, 5); // arrange the tree so hit testing works

        // Button sizes to its content and sits left-aligned (not stretched to fill the
        // window), so it occupies only the first few columns — click inside cell (1, 1)
        // rather than the grid's center, which now falls outside it.
        var (cellWidth, cellHeight) = host.SizeForCells(1, 1);
        float px = cellWidth * 1.5f;
        float py = cellHeight * 1.5f;
        host.MouseDown(px, py);
        host.MouseUp(px, py);

        Assert.True(clicked);
    }

    [Fact]
    public void SendText_TypesIntoFocusedTextBox()
    {
        using var host = new TuiSkiaHost();
        var textBox = new TextBox { Width = 15 };
        host.SetContent(new TuiWindow { Content = textBox });

        using var _ = host.RenderToImage(20, 5); // initial focus lands on the TextBox
        host.SendText("Hi 5");

        Assert.Equal("Hi 5", textBox.Text);
    }

    [Fact]
    public void ProcessKey_ReachesHostedWindow()
    {
        using var host = new TuiSkiaHost();
        var textBox = new TextBox { Width = 15, Text = "abc" };
        host.SetContent(new TuiWindow { Content = textBox });

        using var _ = host.RenderToImage(20, 5);
        host.ProcessKey(ConsoleKey.End);
        host.ProcessKey(ConsoleKey.Backspace);

        Assert.Equal("ab", textBox.Text);
    }

    [Fact]
    public void XamlContent_LoadsWindow()
    {
        using var host = new TuiSkiaHost();
        host.SetContent(xaml: "<TuiWindow><TextBlock Text=\"hi\"/></TuiWindow>");

        Assert.NotNull(host.Window);
        Assert.Null(host.LoadError);
    }

    [Fact]
    public void InvalidXaml_SetsLoadError_AndRenderStillSucceeds()
    {
        using var host = new TuiSkiaHost();
        host.SetContent(xaml: "<Not-Valid-Xaml<");

        using var image = host.RenderToImage(30, 4); // draws the error, must not throw
        Assert.NotNull(host.LoadError);
        Assert.NotNull(image);
    }

    [Fact]
    public void RenderToPng_ProducesDecodablePng()
    {
        using var host = new TuiSkiaHost();
        host.SetContent(new TuiWindow { Background = TuiColor.FromRgb(0, 128, 0) });

        using var stream = new MemoryStream();
        host.RenderToPng(stream, 10, 3);
        stream.Position = 0;

        using var decoded = SKBitmap.Decode(stream);
        Assert.NotNull(decoded);
        var (width, height) = host.SizeForCells(10, 3);
        Assert.Equal((int)MathF.Ceiling(width), decoded.Width);
        Assert.Equal((int)MathF.Ceiling(height), decoded.Height);
    }

    [Fact]
    public void RenderRequested_FiresWhenWindowInvalidates()
    {
        using var host = new TuiSkiaHost();
        var text = new TextBlock { Text = "before" };
        host.SetContent(new TuiWindow { Content = text });
        using var _ = host.RenderToImage(20, 5); // resets the coalescing gate

        bool requested = false;
        host.RenderRequested += () => requested = true;
        text.Text = "after";

        Assert.True(requested);
    }

    [Fact]
    public void ToCell_ClampsToRenderedGrid()
    {
        using var host = new TuiSkiaHost();
        host.SetContent(new TuiWindow());
        using var _ = host.RenderToImage(10, 4);

        Assert.Equal((0, 0), host.ToCell(-100f, -100f));
        Assert.Equal((9, 3), host.ToCell(100000f, 100000f));
    }

    [Fact]
    public void CellsForSize_RoundTripsSizeForCells()
    {
        using var host = new TuiSkiaHost();
        var (width, height) = host.SizeForCells(33, 7);
        Assert.Equal((33, 7), host.CellsForSize(width, height));
    }

    [Fact]
    public void SetFont_ChangesCellMetrics_AndRequestsRender()
    {
        using var host = new TuiSkiaHost(fontSize: 16f);
        float before = host.CellHeight;

        bool requested = false;
        host.RenderRequested += () => requested = true;
        host.SetFont(null, 32f);

        Assert.True(requested);
        Assert.True(host.CellHeight > before);
        Assert.Equal(32f, host.FontSize);
    }
}
