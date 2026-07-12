using System;
using System.Threading;
using Tedd.TUI;

namespace Tedd.TUI.Platform.Console;

/// <summary>
/// Hosts a <see cref="TuiWindow"/> on top of an <see cref="ITuiPlatform"/>. By default
/// the auto-detecting <see cref="PlatformLoader"/> picks the best available backend
/// (Windows Terminal / Linux terminal / legacy 16-color) but callers can force a
/// specific one by passing it explicitly.
/// </summary>
/// <remarks>
/// Threading model: the UI (layout, rendering, input dispatch) runs entirely on the
/// thread that calls <see cref="Run"/>. <see cref="Stop"/> may be called from any thread
/// (e.g. a <c>Console.CancelKeyPress</c> handler); it only signals the run loop, which
/// performs console teardown itself after finishing the frame in flight, so shutdown
/// never races an ongoing render.
/// </remarks>
public class TuiApp
{
    private readonly TuiWindow _window;
    private readonly ITuiPlatform _platform;
    private readonly IRenderer _renderer;
    private readonly ITuiInputManager? _inputManager;
    private readonly LegacyConsolePlatform? _legacyPlatform;

    // Written by Stop() from arbitrary threads, read by the run loop.
    private volatile bool _running = true;
    // Whether the run loop is currently executing (and therefore owns teardown).
    private volatile bool _loopActive;
    // 0 = console not yet restored, 1 = restored. Interlocked so that the run loop's
    // finally-block and a late Stop() cannot both perform teardown.
    private int _cleanedUp;

    private readonly AutoResetEvent _renderWaitHandle = new AutoResetEvent(false);

    private int _lastWidth;
    private int _lastHeight;
    private VirtualBuffer? _buffer;

    public TuiApp(TuiWindow window) : this(window, PlatformLoader.Load())
    {
    }

    public TuiApp(TuiWindow window, ITuiPlatform platform)
    {
        _window = window;
        _platform = platform ?? throw new ArgumentNullException(nameof(platform));
        _platform.Initialize();
        _renderer = _platform.CreateRenderer();
        _inputManager = _platform.CreateInputManager(window);
        _legacyPlatform = platform as LegacyConsolePlatform;

        // Controls (e.g. Markdown.Image) read capabilities via TuiWindow.GetCapabilities().
        // Without this sync the window stayed on SurfaceCapabilities.TextOnly even when
        // the platform advertised Sixel/Kitty/graphics, so bitmap paths never activated.
        _window.Capabilities = _platform.Capabilities;

        _window.VisualChanged += (s, e) => _renderWaitHandle.Set();
        if (_inputManager != null)
        {
            _inputManager.WindowResized += (s, e) => _renderWaitHandle.Set();
        }
    }

    /// <summary>Capabilities advertised by the active platform.</summary>
    public SurfaceCapabilities Capabilities => _platform.Capabilities;

    public void Run()
    {
        if (!_running) return; // Stopped before it ever started.

        System.Console.Clear();
        _inputManager?.Start();

        // The legacy console path participates in the dual-handle wait loop (input
        // signal + render signal) on Windows so input doesn't depend on the polling
        // tick. Other platforms can supply richer pumping later; for now we drive
        // them via the same wait pattern.
        ConsoleInputManager? legacyInput = (_inputManager as LegacyInputAdapter)?.Inner;

        IntPtr[]? winHandles = null;
        WaitHandle[]? unixHandles = null;
        bool waitIncludesInput = false;

        if (legacyInput != null)
        {
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                // Only wait on stdin when it is a real console handle. A redirected
                // stdin (pipe) stays permanently signaled while ReadConsoleInput /
                // GetNumberOfConsoleInputEvents fail, which spun this loop at 100% CPU.
                if (NativeMethods.GetConsoleMode(legacyInput.InputHandle, out _))
                {
                    winHandles = new IntPtr[] { legacyInput.InputHandle, _renderWaitHandle.SafeWaitHandle.DangerousGetHandle() };
                    waitIncludesInput = true;
                }
                else
                {
                    unixHandles = new WaitHandle[] { _renderWaitHandle };
                }
            }
            else
            {
                unixHandles = new WaitHandle[] { legacyInput.InputWaitHandle, _renderWaitHandle };
                waitIncludesInput = true;
            }
        }
        else
        {
            // Non-legacy backend without our wait handles: render-only loop driven by the render signal.
            unixHandles = new WaitHandle[] { _renderWaitHandle };
        }

        _loopActive = true;
        int consecutiveWaitFailures = 0;

        try
        {
            UpdateAndRender();

            while (_running)
            {
                if (winHandles != null)
                {
                    uint result = NativeMethods.WaitForMultipleObjects((uint)winHandles.Length, winHandles, false, NativeMethods.INFINITE);

                    if (result == NativeMethods.WAIT_OBJECT_0)
                    {
                        consecutiveWaitFailures = 0;
                        legacyInput!.ProcessInput();
                    }
                    else if (result == NativeMethods.WAIT_OBJECT_0 + 1)
                    {
                        consecutiveWaitFailures = 0;
                        UpdateAndRender();
                    }
                    else
                    {
                        // WAIT_FAILED (e.g. the console handle became invalid). Retrying
                        // forever would tick every 16 ms for the rest of the process;
                        // after a few failures drop the input handle and fall back to
                        // the managed render-signal wait.
                        if (++consecutiveWaitFailures >= 3)
                        {
                            winHandles = null;
                            unixHandles = new WaitHandle[] { _renderWaitHandle };
                            waitIncludesInput = false;
                        }
                        else
                        {
                            Thread.Sleep(16);
                        }
                    }
                }
                else if (unixHandles != null)
                {
                    int result = WaitHandle.WaitAny(unixHandles, 500);

                    if (waitIncludesInput && result == 0)
                    {
                        legacyInput!.ProcessInput();
                    }
                    else if (result == (waitIncludesInput ? 1 : 0))
                    {
                        UpdateAndRender();
                    }
                    else if (result == WaitHandle.WaitTimeout)
                    {
                        if (System.Console.WindowWidth != _lastWidth || System.Console.WindowHeight != _lastHeight)
                        {
                            UpdateAndRender();
                        }
                    }
                }
            }
        }
        finally
        {
            _loopActive = false;
            // Teardown happens here, on the loop thread, after the last frame has
            // completed — never concurrently with rendering or input processing.
            RestoreConsole();
            // The native wait borrows the render event's raw handle; keep the managed
            // wrapper alive until the loop can no longer touch it.
            GC.KeepAlive(_renderWaitHandle);
        }
    }

    private void UpdateAndRender()
    {
        _window.EnsureInitialFocus();

        var w = System.Console.WindowWidth;
        var h = System.Console.WindowHeight;

        if (w != _lastWidth || h != _lastHeight)
        {
            try
            {
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

        _window.Measure(new Size(w, h));
        _window.Arrange(new Rect(0, 0, w, h));

        if (_buffer == null || _buffer.Width != w || _buffer.Height != h)
        {
            _buffer = new VirtualBuffer(w, h);
            if (_platform.Capabilities.SupportsGraphics)
            {
                _buffer.Graphics = new System.Collections.Generic.List<GraphicPlacement>();
            }
        }
        else
        {
            _buffer.Clear();
            _buffer.Graphics?.Clear();
        }

        _window.Render(_buffer);
        _renderer.Render(_buffer);
    }

    /// <summary>
    /// Requests shutdown. Safe to call from any thread and more than once. When the run
    /// loop is active it wakes up, exits, and restores the console itself; when it is not
    /// (Stop before/after <see cref="Run"/>), the console is restored directly here.
    /// </summary>
    public void Stop()
    {
        _running = false;
        _renderWaitHandle.Set();

        // If the loop is running it owns teardown (it may be mid-frame right now).
        // RestoreConsole is Interlocked-guarded, so even if the loop exits between this
        // check and the call below, the restore still happens exactly once.
        if (!_loopActive)
        {
            RestoreConsole();
        }
    }

    private void RestoreConsole()
    {
        if (Interlocked.Exchange(ref _cleanedUp, 1) != 0) return;

        // Disable mouse tracking first, while the terminal is still in the mode we
        // configured; the input manager's Stop also restores the original console modes.
        try { System.Console.Write("\x1b[?1000l\x1b[?1006l"); } catch { }

        try { _inputManager?.Stop(); } catch { }
        try { _platform.Shutdown(); } catch { }

        try
        {
            System.Console.CursorVisible = true;
            System.Console.ResetColor();
            System.Console.Clear();
        }
        catch { }
    }
}
