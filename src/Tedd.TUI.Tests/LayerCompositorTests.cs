using Xunit;
using Tedd.TUI;

namespace Tedd.TUI.Tests;

public class LayerCompositorTests
{
    [Fact]
    public void Flatten_EmptyStack_LeavesDestinationUntouched()
    {
        var dest = new VirtualBuffer(4, 2);
        dest.FillRect(0, 0, 4, 2, '.', TuiColor.White, TuiColor.Black);

        LayerCompositor.Flatten(System.Array.Empty<RenderLayer>(), dest);

        Assert.Equal('.', dest.GetPixel(0, 0).Character);
        Assert.Equal(TuiColor.White, dest.GetPixel(0, 0).Foreground);
    }

    [Fact]
    public void Flatten_TopOpaqueLayer_Overwrites()
    {
        var dest = new VirtualBuffer(3, 1);
        dest.FillRect(0, 0, 3, 1, 'a', TuiColor.White, TuiColor.Black);

        var layer = new RenderLayer(3, 1, 1);
        layer.Buffer.Clear(TuiColor.Transparent);
        layer.Buffer.SetPixel(1, 0, 'X', TuiColor.Red, TuiColor.Blue);

        LayerCompositor.Flatten(new[] { layer }, dest);

        Assert.Equal('a', dest.GetPixel(0, 0).Character);
        Assert.Equal('X', dest.GetPixel(1, 0).Character);
        Assert.Equal(TuiColor.Red, dest.GetPixel(1, 0).Foreground);
        Assert.Equal(TuiColor.Blue, dest.GetPixel(1, 0).Background);
    }

    [Fact]
    public void Flatten_TranslucentBackground_BlendsOverDestination()
    {
        var dest = new VirtualBuffer(1, 1);
        dest.SetPixel(0, 0, ' ', TuiColor.White, new TuiColor(0, 0, 0));

        var layer = new RenderLayer(1, 1, 1);
        layer.Buffer.Clear(TuiColor.Transparent);
        layer.Buffer.SetPixel(0, 0, ' ', TuiColor.Transparent, new TuiColor(255, 255, 255, 128));

        LayerCompositor.Flatten(new[] { layer }, dest);

        var px = dest.GetPixel(0, 0);
        // 50% white over black ≈ mid gray
        Assert.InRange(px.Background.R, 120, 140);
        Assert.Equal(px.Background.R, px.Background.G);
        Assert.Equal(px.Background.R, px.Background.B);
        Assert.Equal(255, px.Background.A);
    }

    [Fact]
    public void Flatten_ZIndexOrder_RespectsAscending()
    {
        var dest = new VirtualBuffer(1, 1);
        dest.SetPixel(0, 0, '.', TuiColor.White, TuiColor.Black);

        var bottom = new RenderLayer(1, 1, 1);
        bottom.Buffer.SetPixel(0, 0, 'B', TuiColor.Red, TuiColor.Black);
        var top = new RenderLayer(1, 1, 5);
        top.Buffer.SetPixel(0, 0, 'T', TuiColor.Green, TuiColor.Black);

        // Pass them in reverse insertion order to prove sorting kicks in.
        LayerCompositor.Flatten(new[] { top, bottom }, dest);

        Assert.Equal('T', dest.GetPixel(0, 0).Character);
        Assert.Equal(TuiColor.Green, dest.GetPixel(0, 0).Foreground);
    }

    [Fact]
    public void Flatten_OffsetClipsAndTranslates()
    {
        var dest = new VirtualBuffer(4, 2);
        dest.Clear(TuiColor.Black);

        var layer = new RenderLayer(2, 1, 1) { OffsetX = 3, OffsetY = 0 };
        layer.Buffer.SetPixel(0, 0, 'A', TuiColor.Red, TuiColor.Yellow);
        // out-of-bounds cell should be silently clipped
        layer.Buffer.SetPixel(1, 0, 'B', TuiColor.Red, TuiColor.Yellow);

        LayerCompositor.Flatten(new[] { layer }, dest);

        Assert.Equal('A', dest.GetPixel(3, 0).Character);
        Assert.Equal(' ', dest.GetPixel(0, 0).Character);
    }

    [Fact]
    public void Flatten_InvisibleLayer_IsSkipped()
    {
        var dest = new VirtualBuffer(1, 1);
        dest.SetPixel(0, 0, '.', TuiColor.White, TuiColor.Black);

        var layer = new RenderLayer(1, 1, 1) { IsVisible = false };
        layer.Buffer.SetPixel(0, 0, 'X', TuiColor.Red, TuiColor.Blue);

        LayerCompositor.Flatten(new[] { layer }, dest);

        Assert.Equal('.', dest.GetPixel(0, 0).Character);
    }
}
