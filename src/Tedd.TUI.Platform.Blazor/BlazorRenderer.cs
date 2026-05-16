using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
    /// The HTML5 canvas surface supports inline bitmap overlays via
    /// <c>tuiInterop.renderGraphics</c>: every <see cref="GraphicPlacement"/> is sent
    /// to JS along with the cell delta and drawn on top of the character grid using
    /// <c>CanvasRenderingContext2D.drawImage</c>. Images are cached browser-side by
    /// source so the per-frame cost is one hashtable lookup + one draw.
    /// </summary>
    public SurfaceCapabilities Capabilities => new SurfaceCapabilities
    {
        SupportsGraphics = true,
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

        // Emit any bitmap overlays attached to this frame. We send them every frame
        // because Image controls may reflow / move, and the JS side caches by source.
        // Drawing happens after the cell paint so images always end up on top.
        var graphicsPayload = BuildGraphicsPayload(buffer);

        if (_lastBuffer == null || _lastBuffer.GetLength(1) != width || _lastBuffer.GetLength(0) != height)
        {
            _lastBuffer = new Cell[height, width];

            // Full render. Color channels are packed 0xAARRGGBB and read by tuiInterop.js as
            // unsigned-int RGBA values; the legacy ConsoleColor table is no longer used.
            var data = new int[width * height * 3];
            int ptr = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var cell = buffer.GetPixel(x, y);
                    _lastBuffer[y, x] = cell;
                    data[ptr++] = (int)cell.Character;
                    data[ptr++] = unchecked((int)cell.Foreground.Packed);
                    data[ptr++] = unchecked((int)cell.Background.Packed);
                }
            }
            await _js.InvokeVoidAsync("tuiInterop.render", _canvasId, width, height, data);
            await EmitGraphicsAsync(graphicsPayload);
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
                    cell.Foreground.Packed != old.Foreground.Packed ||
                    cell.Background.Packed != old.Background.Packed)
                {
                    changes++;
                    _lastBuffer[y, x] = cell;

                    diffs.Add(x);
                    diffs.Add(y);
                    diffs.Add((int)cell.Character);
                    diffs.Add(unchecked((int)cell.Foreground.Packed));
                    diffs.Add(unchecked((int)cell.Background.Packed));
                }
            }
        }

        if (changes == 0)
        {
            // No text cells changed, but graphics positions may still need to be replayed
            // (e.g. an Image control became visible after a layout reflow). Always send
            // the current frame's graphics so the canvas stays in sync.
            await EmitGraphicsAsync(graphicsPayload);
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
                    data[ptr++] = unchecked((int)cell.Foreground.Packed);
                    data[ptr++] = unchecked((int)cell.Background.Packed);
                }
            }
            await _js.InvokeVoidAsync("tuiInterop.render", _canvasId, width, height, data);
        }
        else
        {
            // Send diffs
            await _js.InvokeVoidAsync("tuiInterop.renderDiff", _canvasId, diffs.ToArray());
        }

        await EmitGraphicsAsync(graphicsPayload);
    }

    /// <summary>
    /// Snapshot of one <see cref="GraphicPlacement"/> in the shape <c>tuiInterop.js</c>
    /// expects: cell coordinates + a data URL the browser can load directly into an
    /// HTMLImageElement. We base64-encode the bytes on the .NET side so the JS layer
    /// stays a thin draw loop with no codec dependencies.
    /// </summary>
    private sealed class GraphicPayload
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int W { get; set; }
        public int H { get; set; }
        public string? Key { get; set; }
        public string? Src { get; set; }
    }

    private readonly Dictionary<byte[], string> _dataUrlCache = new(ReferenceEqualityComparer.Instance);

    private GraphicPayload[]? BuildGraphicsPayload(VirtualBuffer buffer)
    {
        var graphics = buffer.Graphics;
        if (graphics == null || graphics.Count == 0) return null;

        var payload = new GraphicPayload[graphics.Count];
        for (int i = 0; i < graphics.Count; i++)
        {
            var g = graphics[i];
            payload[i] = new GraphicPayload
            {
                X = g.CharX,
                Y = g.CharY,
                W = g.CharWidth,
                H = g.CharHeight,
                Key = g.Source ?? (g.ImageData != null ? RuntimeHelpers.GetHashCode(g.ImageData).ToString() : null),
                Src = BuildDataUrl(g),
            };
        }
        return payload;
    }

    private string? BuildDataUrl(GraphicPlacement g)
    {
        // Prefer the encoded bytes: PNG/JPEG round-trips losslessly into HTMLImageElement
        // and lets the browser cache them. The cache key is the byte[] reference so
        // we don't re-base64 the same payload across frames.
        if (g.ImageData != null && g.ImageData.Length > 0)
        {
            if (!_dataUrlCache.TryGetValue(g.ImageData, out var cached))
            {
                var media = string.IsNullOrEmpty(g.MediaType) ? "image/png" : g.MediaType;
                cached = $"data:{media};base64,{Convert.ToBase64String(g.ImageData)}";
                _dataUrlCache[g.ImageData] = cached;
            }
            return cached;
        }

        // Pixel-only placements (e.g. dynamically-generated images): we'd have to encode
        // them to PNG ourselves to round-trip through HTMLImageElement. That requires a
        // codec dependency on the Blazor side, which we intentionally avoid; the JS
        // counterpart can fall back to direct putImageData in a follow-up if needed.
        if (!string.IsNullOrEmpty(g.Source) &&
            (g.Source.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
             || g.Source.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
             || g.Source.StartsWith("data:", StringComparison.OrdinalIgnoreCase)
             || g.Source.StartsWith("/")))
        {
            return g.Source;
        }

        return null;
    }

    private async Task EmitGraphicsAsync(GraphicPayload[]? payload)
    {
        if (payload == null || payload.Length == 0)
        {
            // Clear any previous graphics so a frame without images doesn't leave stale
            // bitmaps on the canvas.
            await _js.InvokeVoidAsync("tuiInterop.renderGraphics", _canvasId, _charWidth, _charHeight, Array.Empty<object>());
            return;
        }
        await _js.InvokeVoidAsync("tuiInterop.renderGraphics", _canvasId, _charWidth, _charHeight, payload);
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
