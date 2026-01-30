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

    public TuiApp(TuiWindow window)
    {
        _window = window;
        _renderer = new ConsoleRenderer();
        _inputManager = new ConsoleInputManager(window);
        _window.VisualChanged += (s, e) => _renderWaitHandle.Set();
    }

    public void Run()
    {
        // Initial setup
        System.Console.Clear();

        // Use array for WaitHandle? No, WaitForMultipleObjects takes IntPtr array.
        // We have:
        // 1. Console Input Handle (Windows)
        // 2. Render Wait Handle (Event)

        IntPtr[] handles = null;
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
        {
            handles = new IntPtr[] { _inputManager.InputHandle, _renderWaitHandle.SafeWaitHandle.DangerousGetHandle() };
        }

        // Initial Layout & Render
        UpdateAndRender();

        // Main Loop
        while (_running)
        {
            if (handles != null)
            {
                // Wait for Input or Render Request
                uint result = NativeMethods.WaitForMultipleObjects((uint)handles.Length, handles, false, NativeMethods.INFINITE);
                
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
                    // Timeout or Failed
                    Thread.Sleep(16);
                }
            }
            else
            {
                // Non-Windows fallback: Polling with sleep
                if (System.Console.KeyAvailable)
                {
                    _inputManager.ProcessInput();
                }
                else
                {
                     // If we are not on windows, we can't easily wait on handles.
                     // We can check if _renderWaitHandle is set?
                     if (_renderWaitHandle.WaitOne(0))
                     {
                         UpdateAndRender();
                     }
                     else
                     {
                         Thread.Sleep(16);
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
        
        // Measure & Arrange (Layout)
        _window.Measure(new Size(w, h));
        _window.Arrange(new Rect(0, 0, w, h));

        // Render
        var buffer = new VirtualBuffer(w, h);
        _window.Render(buffer);
        _renderer.Render(buffer);
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
