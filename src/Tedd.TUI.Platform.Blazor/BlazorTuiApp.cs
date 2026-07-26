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

    /// <summary>
    /// Whether scrollable regions hand their whole content to the surface as
    /// <see cref="ScrollPane"/>s instead of being clipped to the viewport. Only the DOM surface
    /// can consume them; leaving this false is what keeps the canvas path on the flat grid.
    /// </summary>
    /// <remarks>
    /// This flag alone is the surface-level switch: attaching a
    /// <see cref="VirtualBuffer.ScrollPanes"/> list is what invites viewers to pre-render, and an
    /// individual viewer opts out through <see cref="ScrollViewer.PrerenderContentProperty"/>.
    /// </remarks>
    public bool PrerenderScrollContent { get; set; }

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

        PublishCapabilities();

        _running = true;
        _ = LoopAsync();
    }

    // Intent: produce one complete frame without a render loop, an event loop or JS interop.
    // Why:
    // - Static/prerendered Blazor never reaches OnAfterRender, so the loop that normally paints
    //   the grid never starts and a crawler receives an empty container. Rendering a frame from
    //   OnInitialized puts real markup in the prerendered HTML.
    // - It also removes the blank first paint in interactive WebAssembly.
    // Constraints/Invariants:
    // - Must stay synchronous and JS-free. Cell metrics fall back to the renderer's defaults,
    //   which only affects pixel sizing, never the text content that is the point of prerendering.
    // - DomRenderer.RenderLayersAsync completes synchronously (it stores the list and raises an
    //   event), so no async machinery is needed here.
    // Failure modes:
    // - Calling this on a renderer whose RenderAsync genuinely awaits would drop the frame; only
    //   the layered DOM path is expected to use it.
    /// <summary>
    /// Measures, arranges and renders a single frame immediately, for prerendering or for the
    /// first paint before the interactive loop starts.
    /// </summary>
    public void RenderStaticFrame(int width, int height)
    {
        _width = width;
        _height = height;

        PublishCapabilities();

        _window.Measure(new Size(_width, _height));
        _window.Arrange(new Rect(0, 0, _width, _height));

        if (_renderer is ILayeredRenderer layeredRenderer)
        {
            _ = layeredRenderer.RenderLayersAsync(BuildLayers());
        }
        else
        {
            var buffer = new VirtualBuffer(_width, _height);
            if (_window.Capabilities.SupportsGraphics)
                buffer.Graphics = new System.Collections.Generic.List<GraphicPlacement>();
            _window.Render(buffer);
            _ = _renderer.RenderAsync(buffer);
        }
    }

    /// <summary>Surfaces a capability profile so graphics-aware controls can pick the right path.</summary>
    private void PublishCapabilities()
    {
        _window.Capabilities = (_renderer as ICapabilityProvider)?.Capabilities
                              ?? SurfaceCapabilities.TextOnly;
    }

    /// <summary>
    /// Builds the composited layer stack for the current window state: base content at Z=0 and
    /// the modal overlay, if any, above it.
    /// </summary>
    private System.Collections.Generic.List<RenderLayer> BuildLayers()
    {
        bool supportsGraphics = _window.Capabilities.SupportsGraphics;
        var layers = new System.Collections.Generic.List<RenderLayer>();

        // Layer 0: Main Content
        var contentBuffer = new VirtualBuffer(_width, _height);
        if (supportsGraphics) contentBuffer.Graphics = new System.Collections.Generic.List<GraphicPlacement>();
        if (PrerenderScrollContent) contentBuffer.ScrollPanes = new System.Collections.Generic.List<ScrollPane>();
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
                if (PrerenderScrollContent) overlayBuffer.ScrollPanes = new System.Collections.Generic.List<ScrollPane>();
                // Render relative to itself (0,0 in its buffer)
                overlay.Render(overlayBuffer, -ovX, -ovY);

                layers.Add(new RenderLayer { Buffer = overlayBuffer, X = ovX, Y = ovY, ZIndex = 10 });
            }
        }

        return layers;
    }

    public void OnResize(int width, int height)
    {
        _width = width;
        _height = height;
        RequestRender();
    }

    private async Task LoopAsync()
    {
        while (_running)
        {
            try
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

                    // Render
                    if (_renderer is ILayeredRenderer layeredRenderer)
                    {
                        await layeredRenderer.RenderLayersAsync(BuildLayers());
                    }
                    else
                    {
                        // Fallback / Canvas
                        var buffer = new VirtualBuffer(_width, _height);
                        if (_window.Capabilities.SupportsGraphics)
                            buffer.Graphics = new System.Collections.Generic.List<GraphicPlacement>();
                        _window.Render(buffer);
                        await _renderer.RenderAsync(buffer);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // Allowed during shutdown
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TUI Loop Error: {ex}");
            }

            // Always hand the thread back to the browser before the next frame.
            //
            // On WebAssembly this loop shares the single UI thread with the browser, and
            // `await` on an already-completed task resumes *synchronously*. The semaphore
            // is signalled by every invalidation, so a frame whose own rendering triggers
            // another invalidation makes WaitAsync complete immediately, and the loop
            // spins without the event loop ever running again: no input, no timers, no
            // console output, no repaint — the tab appears hung rather than merely busy.
            // Yielding through a timer guarantees a browser turn per frame, so a
            // re-invalidating frame costs frame rate instead of freezing the page.
            await Task.Delay(1).ConfigureAwait(false);
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
