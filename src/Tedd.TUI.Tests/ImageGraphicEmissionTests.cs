using System;
using System.Collections.Generic;
using Xunit;
using Tedd.TUI;
using Tedd.TUI.Markdown;

namespace Tedd.TUI.Tests;

public class ImageGraphicEmissionTests
{
    private sealed class PixelResolver : IImageResolver
    {
        public byte[] Bytes { get; init; } = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        public string Media { get; init; } = "image/png";

        public bool TryResolve(string source, string? baseDirectory, out byte[] data, out string? mediaType)
        {
            data = Bytes;
            mediaType = Media;
            return true;
        }
    }

    private sealed class PixelDecoder : IImageDecoder
    {
        public int Width { get; init; } = 4;
        public int Height { get; init; } = 4;

        public bool TryDecode(byte[] bytes, out RgbaImage image)
        {
            var px = new byte[Width * Height * 4];
            for (int i = 0; i < px.Length; i++) px[i] = (byte)(i & 0xFF);
            image = new RgbaImage { Width = Width, Height = Height, Pixels = px };
            return true;
        }
    }

    private static IDisposable WithImageHooks()
    {
        var prevDec = Image.DefaultDecoder;
        var prevRes = Image.DefaultResolver;
        Image.DefaultDecoder = new PixelDecoder();
        Image.DefaultResolver = new PixelResolver();
        return new Disposable(() => { Image.DefaultDecoder = prevDec; Image.DefaultResolver = prevRes; });
    }

    private sealed class Disposable : IDisposable
    {
        private readonly Action _a;
        public Disposable(Action a) { _a = a; }
        public void Dispose() => _a();
    }

    [Fact]
    public void Image_EmitsPlacement_WithEncodedAndDecodedPayload()
    {
        using var _ = WithImageHooks();

        var win = new TuiWindow
        {
            Capabilities = new SurfaceCapabilities { SupportsGraphics = true, CharPixelWidth = 8, CharPixelHeight = 16 }
        };
        var img = new Image { Source = "anything.png", RenderMode = ImageRenderMode.Graphic };
        win.Content = img;

        win.Measure(new Size(40, 10));
        win.Arrange(new Rect(0, 0, 40, 10));

        var buffer = new VirtualBuffer(40, 10) { Graphics = new List<GraphicPlacement>() };
        win.Render(buffer);

        Assert.Single(buffer.Graphics);
        var p = buffer.Graphics[0];

        // Encoded payload survives for HTML / Kitty / iTerm2.
        Assert.NotNull(p.ImageData);
        Assert.Equal("image/png", p.MediaType);

        // Decoded payload survives for Sixel / pixel-blitter surfaces.
        Assert.NotNull(p.Pixels);
        Assert.Equal(4, p.PixelWidth);
        Assert.Equal(4, p.PixelHeight);
        Assert.Equal(p.PixelWidth * p.PixelHeight * 4, p.Pixels!.Length);
    }

    [Fact]
    public void Image_NoGraphicsList_FallsBackToAscii()
    {
        using var _ = WithImageHooks();

        var win = new TuiWindow
        {
            Capabilities = new SurfaceCapabilities { SupportsGraphics = false }
        };
        var img = new Image { Source = "anything.png", RenderMode = ImageRenderMode.Auto };
        win.Content = img;

        win.Measure(new Size(20, 10));
        win.Arrange(new Rect(0, 0, 20, 10));

        // No Graphics list → falls back to ASCII renderer, which should write cells.
        var buffer = new VirtualBuffer(20, 10);
        win.Render(buffer);
        // No graphic placements should have been emitted.
        Assert.Null(buffer.Graphics);
    }
}
