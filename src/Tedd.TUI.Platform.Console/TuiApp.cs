using System;
using System.Threading;

namespace Tedd.TUI.Platform.Console;

public class TuiApp
{
    private readonly TuiWindow _window;
    private readonly ConsoleRenderer _renderer;
    private readonly ConsoleInputManager _inputManager;
    private bool _running = true;

    public TuiApp(TuiWindow window)
    {
        _window = window;
        _renderer = new ConsoleRenderer();
        _inputManager = new ConsoleInputManager(window);
    }

    public void Run()
    {
        // Initial setup
        System.Console.Clear();

        // Main Loop
        while (_running)
        {
            // Input
            _inputManager.ProcessInput();

            // Measure & Arrange (Layout)
            // In a real app, we only do this on invalidation.
            var w = System.Console.WindowWidth;
            var h = System.Console.WindowHeight;
            _window.Measure(new Size(w, h));
            _window.Arrange(new Rect(0, 0, w, h));

            // Render
            var buffer = new VirtualBuffer(w, h);
            _window.Render(buffer);
            _renderer.Render(buffer);

            // Frame limiter
            Thread.Sleep(16); // ~60fps
        }
    }

    public void Stop()
    {
        _running = false;
        // Restore Console State
        System.Console.Write("\x1b[?1000l\x1b[?1006l");
        System.Console.CursorVisible = true;
        System.Console.ResetColor();
        System.Console.Clear();
    }
}
