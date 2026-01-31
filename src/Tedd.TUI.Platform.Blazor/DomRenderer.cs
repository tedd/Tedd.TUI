using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Tedd.TUI;

namespace Tedd.TUI.Platform.Blazor;

public class DomRenderer : IRendererAsync, ILayeredRenderer
{
    public event Action? OnRender;
    public List<RenderLayer>? Layers { get; private set; }
    public int CharWidth { get; private set; } = 10;
    public int CharHeight { get; private set; } = 18;
    
    // For backward compatibility (single layer)
    public VirtualBuffer? CurrentBuffer => Layers?.Count > 0 ? Layers[0].Buffer : null;

    private readonly IJSRuntime? _js;

    public DomRenderer() { }
    
    public DomRenderer(IJSRuntime js) 
    {
        _js = js;
    }

    public async Task<(int CharWidth, int CharHeight)> InitAsync(int width, int height)
    {
        if (_js != null)
        {
            try 
            {
                var res = await _js.InvokeAsync<MetricResult>("tuiInterop.measureDom");
                CharWidth = (int)Math.Round(res.CharWidth);
                CharHeight = (int)Math.Round(res.CharHeight);
                return (CharWidth, CharHeight);
            }
            catch 
            {
                // Fallback if JS fails
            }
        }
        return (10, 18);
    }

    private class MetricResult
    {
        public double CharWidth { get; set; }
        public double CharHeight { get; set; }
    }

    public Task RenderAsync(VirtualBuffer buffer)
    {
        // Wrap single buffer in a layer
        Layers = new List<RenderLayer> 
        { 
            new RenderLayer { Buffer = buffer, X = 0, Y = 0, ZIndex = 0 } 
        };
        OnRender?.Invoke();
        return Task.CompletedTask;
    }

    public Task RenderLayersAsync(List<RenderLayer> layers)
    {
        Layers = layers;
        OnRender?.Invoke();
        return Task.CompletedTask;
    }
}
