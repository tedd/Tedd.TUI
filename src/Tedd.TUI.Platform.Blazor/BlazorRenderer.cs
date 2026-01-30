using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Tedd.TUI;

namespace Tedd.TUI.Platform.Blazor;

public class BlazorRenderer : IRenderer, IRendererAsync
{
    private readonly IJSRuntime _js;
    private readonly string _canvasId;

    public BlazorRenderer(IJSRuntime js, string canvasId)
    {
        _js = js;
        _canvasId = canvasId;
    }

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

        // Flatten buffer: [char, fg, bg, char, fg, bg, ...]
        var data = new int[width * height * 3];
        int ptr = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                var cell = buffer.GetPixel(x, y);
                data[ptr++] = (int)cell.Character;
                data[ptr++] = (int)cell.Foreground;
                data[ptr++] = (int)cell.Background;
            }
        }

        await _js.InvokeVoidAsync("tuiInterop.render", _canvasId, width, height, data);
    }

    public async Task<(int CharWidth, int CharHeight)> InitAsync(int width, int height)
    {
         var res = await _js.InvokeAsync<MetricResult>("tuiInterop.init", _canvasId, width, height);
         return (res.CharWidth, res.CharHeight);
    }

    private class MetricResult
    {
        public int CharWidth { get; set; }
        public int CharHeight { get; set; }
    }
}
