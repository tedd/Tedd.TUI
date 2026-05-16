using System.Collections.Generic;
using System.IO;
using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class AnsiTrueColorRendererTests
{
    [Fact]
    public void Render_EmitsTruecolorSgrSequence()
    {
        var sw = new StringWriter();
        var renderer = new AnsiTrueColorRenderer(sw);
        var buffer = new VirtualBuffer(3, 1);
        buffer.SetPixel(0, 0, 'A', new TuiColor(10, 20, 30), new TuiColor(40, 50, 60));
        buffer.SetPixel(1, 0, 'B', new TuiColor(10, 20, 30), new TuiColor(40, 50, 60));
        buffer.SetPixel(2, 0, 'C', new TuiColor(10, 20, 30), new TuiColor(40, 50, 60));

        renderer.Render(buffer);

        string output = sw.ToString();
        Assert.Contains("\x1b[38;2;10;20;30m", output);
        Assert.Contains("\x1b[48;2;40;50;60m", output);
        Assert.Contains("ABC", output);
    }

    [Fact]
    public void Render_OnlyEmitsDiffsOnSecondFrame()
    {
        var sw = new StringWriter();
        var renderer = new AnsiTrueColorRenderer(sw);
        var buffer = new VirtualBuffer(2, 1);
        buffer.SetPixel(0, 0, 'X', TuiColor.White, TuiColor.Black);
        buffer.SetPixel(1, 0, 'Y', TuiColor.White, TuiColor.Black);

        renderer.Render(buffer);
        string firstFrame = sw.ToString();
        sw.GetStringBuilder().Clear();

        // No changes → no output.
        renderer.Render(buffer);
        Assert.Empty(sw.ToString());

        // One cell change → only that cell emitted.
        buffer.SetPixel(1, 0, 'Z', TuiColor.White, TuiColor.Black);
        renderer.Render(buffer);
        string diffFrame = sw.ToString();
        Assert.Contains("Z", diffFrame);
        Assert.DoesNotContain("XY", diffFrame);
    }

    [Fact]
    public void Render_InvokesImageEncoderForPlacements()
    {
        var sw = new StringWriter();
        var recorded = new List<GraphicPlacement>();
        var encoder = new FakeEncoder(recorded);
        var renderer = new AnsiTrueColorRenderer(sw) { ImageEncoder = encoder };
        var buffer = new VirtualBuffer(4, 2)
        {
            Graphics = new List<GraphicPlacement>
            {
                new GraphicPlacement { CharX = 1, CharY = 0, CharWidth = 2, CharHeight = 1, Source = "x" }
            }
        };
        buffer.SetPixel(0, 0, ' ', TuiColor.White, TuiColor.Black);

        renderer.Render(buffer);

        Assert.Single(recorded);
        Assert.Contains("\x1b[1;2H", sw.ToString()); // cursor at row 1, col 2 (1-based)
        Assert.Contains("FAKE", sw.ToString());
    }

    private sealed class FakeEncoder : IImageProtocolEncoder
    {
        private readonly List<GraphicPlacement> _recorded;
        public string Protocol => "fake";
        public FakeEncoder(List<GraphicPlacement> recorded) { _recorded = recorded; }
        public string Encode(GraphicPlacement placement)
        {
            _recorded.Add(placement);
            return "FAKE";
        }
    }
}
