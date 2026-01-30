using System;
using System.Threading.Tasks;
using Tedd.TUI;

namespace Tedd.TUI.Platform.Blazor;

public class BlazorTuiApp : IDisposable
{
    private readonly TuiWindow _window;
    private readonly BlazorRenderer _renderer;
    private readonly BlazorInputManager _inputManager;
    private bool _running;
    private int _width;
    private int _height;

    public BlazorInputManager InputManager => _inputManager;
    public TuiWindow Window => _window;

    public BlazorTuiApp(TuiWindow window, BlazorRenderer renderer)
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
            var buffer = new VirtualBuffer(_width, _height);
            _window.Render(buffer);

            await _renderer.RenderAsync(buffer);

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
