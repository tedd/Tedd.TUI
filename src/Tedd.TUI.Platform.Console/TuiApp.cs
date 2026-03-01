using System;
using System.Threading;

namespace Tedd.TUI.Platform.Console;

public class TuiApp
{
    private readonly TuiWindow _window;
    private readonly ConsoleRenderer _renderer;
    private readonly ConsoleInputManager _inputManager;
    private bool _running = true;

    private readonly AutoResetEvent _renderWaitHandle = new AutoResetEvent(false);

    private int _lastWidth;
    private int _lastHeight;
    private VirtualBuffer? _buffer;

    public TuiApp(TuiWindow window)
    {
        _window = window;
        _renderer = new ConsoleRenderer();
        _inputManager = new ConsoleInputManager(window);
        _window.VisualChanged += (s, e) => _renderWaitHandle.Set();
        _inputManager.WindowResized += (s, e) => _renderWaitHandle.Set();
    }

    public void Run()
    {
        // Initial setup
        System.Console.Clear();
        _inputManager.Start();

        // Use array for WaitHandle? No, WaitForMultipleObjects takes IntPtr array.
        // We have:
        // 1. Console Input Handle (Windows)
        // 2. Render Wait Handle (Event)

        IntPtr[] winHandles = null;
        WaitHandle[] unixHandles = null;

        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
        {
            winHandles = new IntPtr[] { _inputManager.InputHandle, _renderWaitHandle.SafeWaitHandle.DangerousGetHandle() };
        }
        else
        {
            unixHandles = new WaitHandle[] { _inputManager.InputWaitHandle, _renderWaitHandle };
        }

        // Initial Layout & Render
        UpdateAndRender();

        // Main Loop
        while (_running)
        {
            if (winHandles != null)
            {
                // Wait for Input or Render Request
                uint result = NativeMethods.WaitForMultipleObjects((uint)winHandles.Length, winHandles, false, NativeMethods.INFINITE);

                if (result == NativeMethods.WAIT_OBJECT_0) // Input
                {
                    _inputManager.ProcessInput();
                }
                else if (result == NativeMethods.WAIT_OBJECT_0 + 1) // Render Notified
                {
                    UpdateAndRender();
                }
                else
                {
                    // Failed
                    Thread.Sleep(16);
                }
            }
            else
            {
                // Non-Windows fallback: Blocking Wait
                // 0 = Input, 1 = Render
                // Timeout 500ms to poll for resize
                int result = WaitHandle.WaitAny(unixHandles, 500);

                if (result == 0) // Input
                {
                    _inputManager.ProcessInput();
                }
                else if (result == 1) // Render
                {
                    UpdateAndRender();
                }
                else if (result == WaitHandle.WaitTimeout)
                {
                    // Check for resize
                    if (System.Console.WindowWidth != _lastWidth || System.Console.WindowHeight != _lastHeight)
                    {
                        UpdateAndRender();
                    }
                }
            }
        }
    }

    private void UpdateAndRender()
    {
        // Ensure focus is set (e.g. first focusable in selected tab) so Tab and keys work
        _window.EnsureInitialFocus();

        var w = System.Console.WindowWidth;
        var h = System.Console.WindowHeight;

        if (w != _lastWidth || h != _lastHeight)
        {
            try
            {
                // Sync buffer size to window size to prevent scrolling/skewing artifacts
                // and ensure (0,0) remains the top-left of the viewport.
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                {
                    if (System.Console.BufferWidth != w || System.Console.BufferHeight != h)
                    {
                        System.Console.SetBufferSize(w, h);
                    }
                }
            }
            catch { }
        }

        _lastWidth = w;
        _lastHeight = h;

        // Measure & Arrange (Layout)
        _window.Measure(new Size(w, h));
        _window.Arrange(new Rect(0, 0, w, h));

        // Render
        if (_buffer == null || _buffer.Width != w || _buffer.Height != h)
        {
            _buffer = new VirtualBuffer(w, h);
        }
        else
        {
            _buffer.Clear();
        }

        _window.Render(_buffer);
        _renderer.Render(_buffer);
    }

    public void Stop()
    {
        _running = false;
        _renderWaitHandle.Set(); // Wake up loop

        // Restore Console State
        System.Console.Write("\x1b[?1000l\x1b[?1006l");
        System.Console.CursorVisible = true;
        System.Console.ResetColor();
        System.Console.Clear();
    }
}
