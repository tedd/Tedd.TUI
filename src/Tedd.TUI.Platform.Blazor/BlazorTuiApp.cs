using System;
using System.Threading.Tasks;
using Tedd.TUI;

namespace Tedd.TUI.Platform.Blazor;

public class BlazorTuiApp : IDisposable
{
    private readonly TuiWindow _window;
    private readonly IRendererAsync _renderer;
    private readonly BlazorInputManager _inputManager;
    private bool _running;
    private int _width;
    private int _height;

    public BlazorInputManager InputManager => _inputManager;
    public TuiWindow Window => _window;

    public BlazorTuiApp(TuiWindow window, IRendererAsync renderer)
    {
        _window = window;
        _renderer = renderer;
        _inputManager = new BlazorInputManager(window);
    }

    public async Task StartAsync(int width, int height)
    {
        _width = width;
        _height = height;

        // Init renderer
        var metrics = await _renderer.InitAsync(width, height);
        _inputManager.CharWidth = metrics.CharWidth;
        _inputManager.CharHeight = metrics.CharHeight;

        _running = true;
        _ = LoopAsync();
    }

    public void OnResize(int width, int height)
    {
        _width = width;
        _height = height;
    }

    private async Task LoopAsync()
    {
        while (_running)
        {
            var start = DateTime.UtcNow;

            // 1. Input
            _inputManager.ProcessInput();

            // 2. Measure & Arrange
            // Only strictly needed if something changed, but TUI usually redraws
            _window.Measure(new Size(_width, _height));
            _window.Arrange(new Rect(0, 0, _width, _height));

            // 3. Render
            if (_renderer is ILayeredRenderer layeredRenderer)
            {
                var layers = new System.Collections.Generic.List<RenderLayer>();

                // Layer 0: Main Content
                var contentBuffer = new VirtualBuffer(_width, _height);
                if (_window.Content != null)
                {
                     _window.Content.Render(contentBuffer, 0, 0);
                }
                layers.Add(new RenderLayer { Buffer = contentBuffer, X = 0, Y = 0, ZIndex = 0 });

                // Layer 1: Overlay
                if (_window.Overlay != null)
                {
                    // Overlay usually renders at Window coords, but we want it in its own layer.
                    // If Overlay is formatted to window size (like Dialog in current impl), 
                    // it draws relative to 0,0 anyway.
                    // But if we want to OPTIMIZE size, we should check RenderSize of overlay.
                    // DialogBox.Show() sets position and size in Arrange(rect).
                    // So RenderSize should be correct.
                    var overlay = _window.Overlay;
                    var ovW = overlay.RenderSize.Width;
                    var ovH = overlay.RenderSize.Height;
                    var ovX = overlay.RenderSize.X;
                    var ovY = overlay.RenderSize.Y;

                    if (ovW > 0 && ovH > 0)
                    {
                        var overlayBuffer = new VirtualBuffer(ovW, ovH);
                        // Render relative to itself (0,0 in its buffer)
                        // DialogBox.Render adds RenderSize.X/Y to the passed offset.
                        // So to render at 0,0 in this new buffer, we must subtract the component's position.
                        overlay.Render(overlayBuffer, -ovX, -ovY); 
                        
                        layers.Add(new RenderLayer { Buffer = overlayBuffer, X = ovX, Y = ovY, ZIndex = 10 });
                    }
                }

                await layeredRenderer.RenderLayersAsync(layers);
            }
            else
            {
                // Fallback / Canvas
                var buffer = new VirtualBuffer(_width, _height);
                _window.Render(buffer);
                await _renderer.RenderAsync(buffer);
            }

            // 4. Wait
            var elapsed = (DateTime.UtcNow - start).TotalMilliseconds;
            var delay = 16 - (int)elapsed; // Target ~60fps
            if (delay > 0) await Task.Delay(delay);
            else await Task.Yield();
        }
    }

    public void Stop()
    {
        _running = false;
    }

    public void Dispose()
    {
        Stop();
    }
}
