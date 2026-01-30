using System;
using System.Threading.Tasks;
using Tedd.TUI;

namespace Tedd.TUI.Platform.Blazor;

public class DomRenderer : IRendererAsync
{
    public event Action? OnRender;
    public VirtualBuffer? CurrentBuffer { get; private set; }

    public Task<(int CharWidth, int CharHeight)> InitAsync(int width, int height)
    {
        // For DOM, we might rely on CSS or measure a test element.
        // For now, let's assume a default or matching what BlazorRenderer does (10x18)
        // ideally we should measure a span in the DOM.
        // But since we control the CSS, we can force a size.
        return Task.FromResult((10, 18));
    }

    public Task RenderAsync(VirtualBuffer buffer)
    {
        // Copy buffer or reference it?
        // Since VirtualBuffer might be reused/modified by TUI immediately after,
        // we should probably copy it if we were async, but since we are just triggering
        // a UI update that will happen on the UI thread, we might be okay.
        // However, TUI usually creates a new VirtualBuffer each frame (as seen in BlazorTuiApp).
        
        CurrentBuffer = buffer;
        OnRender?.Invoke();
        return Task.CompletedTask;
    }
}
