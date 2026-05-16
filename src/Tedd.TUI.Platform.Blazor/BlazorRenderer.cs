using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Tedd.TUI;

namespace Tedd.TUI.Platform.Blazor;

public class BlazorRenderer : IRenderer, IRendererAsync, ICapabilityProvider
{
    private readonly IJSRuntime _js;
    private readonly string _canvasId;
    private Cell[,]? _lastBuffer;
    private int _charWidth = 10;
    private int _charHeight = 18;

    public BlazorRenderer(IJSRuntime js, string canvasId)
    {
        _js = js;
        _canvasId = canvasId;
    }

    /// <summary>
    /// The &lt;canvas&gt; renderer currently only draws character cells. Graphic overlays for
    /// the canvas surface are out of scope for this iteration, so we report text-only.
    /// </summary>
    public SurfaceCapabilities Capabilities => new SurfaceCapabilities
    {
        SupportsGraphics = false,
        CharPixelWidth = _charWidth,
        CharPixelHeight = _charHeight
    };

    public void Render(VirtualBuffer buffer)
    {
        // Fire and forget for sync interface compliance.
        // In a real loop, we should await RenderAsync.
        _ = RenderAsync(buffer);
    }

    public async Task RenderAsync(VirtualBuffer buffer)
    {
        var width = buffer.Width;
        var height = buffer.Height;

        if (_lastBuffer == null || _lastBuffer.GetLength(1) != width || _lastBuffer.GetLength(0) != height)
        {
            _lastBuffer = new Cell[height, width];

            // Full render
            var data = new int[width * height * 3];
            int ptr = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var cell = buffer.GetPixel(x, y);
                    _lastBuffer[y, x] = cell;
                    data[ptr++] = (int)cell.Character;
                    data[ptr++] = (int)cell.Foreground;
                    data[ptr++] = (int)cell.Background;
                }
            }
            await _js.InvokeVoidAsync("tuiInterop.render", _canvasId, width, height, data);
            return;
        }

        // Diff render
        var diffs = new System.Collections.Generic.List<int>();
        int changes = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var cell = buffer.GetPixel(x, y);
                var old = _lastBuffer[y, x];

                if (cell.Character != old.Character ||
                    cell.Foreground != old.Foreground ||
                    cell.Background != old.Background)
                {
                    changes++;
                    _lastBuffer[y, x] = cell;

                    diffs.Add(x);
                    diffs.Add(y);
                    diffs.Add((int)cell.Character);
                    diffs.Add((int)cell.Foreground);
                    diffs.Add((int)cell.Background);
                }
            }
        }

        if (changes == 0)
        {
            return;
        }

        // Check threshold (60% changes)
        // Full update: W * H * 3 ints
        // Diff update: changes * 5 ints
        if (changes * 5 > width * height * 3)
        {
            // Fallback to full render
            var data = new int[width * height * 3];
            int ptr = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // _lastBuffer is already updated
                    var cell = _lastBuffer[y, x];
                    data[ptr++] = (int)cell.Character;
                    data[ptr++] = (int)cell.Foreground;
                    data[ptr++] = (int)cell.Background;
                }
            }
            await _js.InvokeVoidAsync("tuiInterop.render", _canvasId, width, height, data);
        }
        else
        {
            // Send diffs
            await _js.InvokeVoidAsync("tuiInterop.renderDiff", _canvasId, diffs.ToArray());
        }
    }

    public async Task<(int CharWidth, int CharHeight)> InitAsync(int width, int height)
    {
        var res = await _js.InvokeAsync<MetricResult>("tuiInterop.init", _canvasId, width, height);
        _charWidth = res.CharWidth;
        _charHeight = res.CharHeight;
        return (res.CharWidth, res.CharHeight);
    }

    private class MetricResult
    {
        public int CharWidth { get; set; }
        public int CharHeight { get; set; }
    }
}
