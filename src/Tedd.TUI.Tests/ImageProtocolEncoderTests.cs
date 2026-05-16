using System.Text;
using Xunit;
using Tedd.TUI;
using Tedd.TUI.Platform.LinuxTerminal;
using Tedd.TUI.Platform.WindowsTerminal;

namespace Tedd.TUI.Tests;

public class ImageProtocolEncoderTests
{
    private static readonly byte[] PngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x01, 0x02, 0x03 };

    [Fact]
    public void SixelEncoder_WrapsPayloadInDecPm()
    {
        var encoder = new SixelEncoder();
        var encoded = encoder.Encode(new GraphicPlacement { CharWidth = 3, CharHeight = 2 });

        Assert.StartsWith("\x1bP", encoded);
        Assert.EndsWith("\x1b\\", encoded);
        Assert.Contains("q", encoded);
        Assert.Equal("sixel", encoder.Protocol);
    }

    [Fact]
    public void SixelEncoder_EncodesRgbaPixels()
    {
        // 2x2 image: red, green, blue, white.
        var pixels = new byte[]
        {
            255, 0,   0,   255,   // (0,0) red
            0,   255, 0,   255,   // (1,0) green
            0,   0,   255, 255,   // (0,1) blue
            255, 255, 255, 255,   // (1,1) white
        };

        var encoded = new SixelEncoder().Encode(new GraphicPlacement
        {
            Pixels = pixels,
            PixelWidth = 2,
            PixelHeight = 2,
            CharWidth = 1,
            CharHeight = 1,
        });

        Assert.StartsWith("\x1bP0;1;0q", encoded);
        Assert.EndsWith("\x1b\\", encoded);
        // Each used color must declare itself: "#<idx>;2;<r>;<g>;<b>".
        Assert.Matches(@"#\d+;2;100;0;0", encoded); // red
        Assert.Matches(@"#\d+;2;0;100;0", encoded); // green
        Assert.Matches(@"#\d+;2;0;0;100", encoded); // blue
        // Each sixel character must land in the printable [?..~] range.
        for (int i = 8; i < encoded.Length - 2; i++)
        {
            char c = encoded[i];
            if (c == '#' || c == ';' || c == '$' || c == '-' || c == '"' || c == '!' || (c >= '0' && c <= '9')) continue;
            if (c < 0x3F || c > 0x7E) Assert.Fail($"Non-printable sixel byte 0x{(int)c:X2} at {i}");
        }
    }

    [Fact]
    public void SixelEncoder_HandlesTransparentPixels()
    {
        var pixels = new byte[8] { 0, 0, 0, 0, 255, 0, 0, 255 };
        var encoded = SixelEncoderCore.EncodePixels(pixels, 2, 1);
        // Transparent pixel emits no palette entry; only the red one does.
        Assert.Single(System.Text.RegularExpressions.Regex.Matches(encoded, @"#\d+;2;"));
    }

    [Fact]
    public void KittyEncoder_EmitsChunkedBase64()
    {
        var encoder = new KittyGraphicsEncoder();
        var encoded = encoder.Encode(new GraphicPlacement
        {
            ImageData = PngBytes,
            CharWidth = 4,
            CharHeight = 2,
        });

        Assert.StartsWith("\x1b_G", encoded);
        Assert.EndsWith("\x1b\\", encoded);
        Assert.Contains("a=T,f=100", encoded);
        Assert.Contains("c=4", encoded);
        Assert.Contains("r=2", encoded);
        Assert.Contains(System.Convert.ToBase64String(PngBytes), encoded);
        Assert.Equal("kitty", encoder.Protocol);
    }

    [Fact]
    public void KittyEncoder_NoBytes_ReturnsEmpty()
    {
        var encoded = new KittyGraphicsEncoder().Encode(new GraphicPlacement());
        Assert.Equal(string.Empty, encoded);
    }

    [Fact]
    public void ITerm2Encoder_EmitsInlineEscape()
    {
        var encoder = new ITerm2InlineEncoder();
        var encoded = encoder.Encode(new GraphicPlacement
        {
            ImageData = PngBytes,
            CharWidth = 10,
            CharHeight = 5,
            MediaType = "image/png",
        });

        Assert.StartsWith("\x1b]1337;File=inline=1", encoded);
        Assert.EndsWith("\x07", encoded);
        Assert.Contains("width=10", encoded);
        Assert.Contains("height=5", encoded);
        Assert.Contains("type=image/png", encoded);
        Assert.Contains(System.Convert.ToBase64String(PngBytes), encoded);
        Assert.Equal("iterm2", encoder.Protocol);
    }
}
