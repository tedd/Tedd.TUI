using System;
using System.Threading;
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

    // Event-driven loop primitives
    private volatile bool _needsRender = true; // Start with a render
    private readonly SemaphoreSlim _loopSemaphore = new SemaphoreSlim(1, 1); // Start signaled

    public BlazorInputManager InputManager => _inputManager;
    public TuiWindow Window => _window;

    public BlazorTuiApp(TuiWindow window, IRendererAsync renderer)
    {
        _window = window;
        _renderer = renderer;
        _inputManager = new BlazorInputManager(window);

        // Subscribe to events to wake up the loop
        _window.VisualChanged += (s, e) => RequestRender();
        _inputManager.InputAvailable += () => SignalLoop();
    }

    private void RequestRender()
    {
        _needsRender = true;
        SignalLoop();
    }

    private void SignalLoop()
    {
        try
        {
            if (_loopSemaphore.CurrentCount == 0)
                _loopSemaphore.Release();
        }
        catch (SemaphoreFullException) { }
        catch (ObjectDisposedException) { }
    }

    public async Task StartAsync(int width, int height)
    {
        _width = width;
        _height = height;

        // Init renderer
        var metrics = await _renderer.InitAsync(width, height);
        _inputManager.CharWidth = metrics.CharWidth;
        _inputManager.CharHeight = metrics.CharHeight;

        // Surface a capability profile so graphics-aware controls can pick the right path.
        _window.Capabilities = (_renderer as ICapabilityProvider)?.Capabilities
                              ?? SurfaceCapabilities.TextOnly;

        _running = true;
        _ = LoopAsync();
    }

    public void OnResize(int width, int height)
    {
        _width = width;
        _height = height;
        RequestRender();
    }

    private async Task LoopAsync()
    {
        try
        {
            while (_running)
            {
                // Wait for signal with timeout (100ms) for safety/polling backup
                // We use WaitAsync to yield the thread.
                await _loopSemaphore.WaitAsync(100);

                if (!_running) break;

                // 1. Process Input
                // Processing input might trigger VisualChanged, setting _needsRender to true
                _inputManager.ProcessInput();

                // 2. Render if needed
                if (_needsRender)
                {
                    _needsRender = false;

                    // Measure & Arrange
                    _window.Measure(new Size(_width, _height));
                    _window.Arrange(new Rect(0, 0, _width, _height));

                    bool supportsGraphics = _window.Capabilities.SupportsGraphics;

                    // Render
                    if (_renderer is ILayeredRenderer layeredRenderer)
                    {
                        var layers = new System.Collections.Generic.List<RenderLayer>();

                        // Layer 0: Main Content
                        var contentBuffer = new VirtualBuffer(_width, _height);
                        if (supportsGraphics) contentBuffer.Graphics = new System.Collections.Generic.List<GraphicPlacement>();
                        if (_window.Content != null)
                        {
                            _window.Content.Render(contentBuffer, 0, 0);
                        }
                        layers.Add(new RenderLayer { Buffer = contentBuffer, X = 0, Y = 0, ZIndex = 0 });

                        // Layer 1: Overlay
                        if (_window.Overlay != null)
                        {
                            var overlay = _window.Overlay;
                            var ovW = overlay.RenderSize.Width;
                            var ovH = overlay.RenderSize.Height;
                            var ovX = overlay.RenderSize.X;
                            var ovY = overlay.RenderSize.Y;

                            if (ovW > 0 && ovH > 0)
                            {
                                var overlayBuffer = new VirtualBuffer(ovW, ovH);
                                if (supportsGraphics) overlayBuffer.Graphics = new System.Collections.Generic.List<GraphicPlacement>();
                                // Render relative to itself (0,0 in its buffer)
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
                        if (supportsGraphics) buffer.Graphics = new System.Collections.Generic.List<GraphicPlacement>();
                        _window.Render(buffer);
                        await _renderer.RenderAsync(buffer);
                    }
                }
            }
        }
        catch (ObjectDisposedException)
        {
            // Allowed during shutdown
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TUI Loop Error: {ex}");
        }
    }

    public void Stop()
    {
        _running = false;
        SignalLoop();
    }

    public void Dispose()
    {
        Stop();
        _loopSemaphore.Dispose();
    }
}
