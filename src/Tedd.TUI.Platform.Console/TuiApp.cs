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
public class TuiApp
{
    private readonly TuiWindow _window;
    private readonly ITuiPlatform _platform;
    private readonly IRenderer _renderer;
    private readonly ITuiInputManager? _inputManager;
    private readonly LegacyConsolePlatform? _legacyPlatform;
    private bool _running = true;

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
        System.Console.Clear();
        _inputManager?.Start();

        // The legacy console path participates in the dual-handle wait loop (input
        // signal + render signal) on Windows so input doesn't depend on the polling
        // tick. Other platforms can supply richer pumping later; for now we drive
        // them via the same wait pattern.
        IntPtr[]? winHandles = null;
        WaitHandle[]? unixHandles = null;
        ConsoleInputManager? legacyInput = (_inputManager as LegacyInputAdapter)?.Inner;

        if (legacyInput != null)
        {
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                winHandles = new IntPtr[] { legacyInput.InputHandle, _renderWaitHandle.SafeWaitHandle.DangerousGetHandle() };
            }
            else
            {
                unixHandles = new WaitHandle[] { legacyInput.InputWaitHandle, _renderWaitHandle };
            }
        }
        else
        {
            // Non-legacy backend without our wait handles: spin off a render-only loop driven by the render signal.
            unixHandles = new WaitHandle[] { _renderWaitHandle };
        }

        UpdateAndRender();

        while (_running)
        {
            if (winHandles != null)
            {
                uint result = NativeMethods.WaitForMultipleObjects((uint)winHandles.Length, winHandles, false, NativeMethods.INFINITE);

                if (result == NativeMethods.WAIT_OBJECT_0)
                {
                    legacyInput!.ProcessInput();
                }
                else if (result == NativeMethods.WAIT_OBJECT_0 + 1)
                {
                    UpdateAndRender();
                }
                else
                {
                    Thread.Sleep(16);
                }
            }
            else if (unixHandles != null)
            {
                int result = WaitHandle.WaitAny(unixHandles, 500);

                if (legacyInput != null && result == 0)
                {
                    legacyInput.ProcessInput();
                }
                else if ((legacyInput != null && result == 1) || (legacyInput == null && result == 0))
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

    public void Stop()
    {
        _running = false;
        _renderWaitHandle.Set();

        try { _inputManager?.Stop(); } catch { }
        try { _platform.Shutdown(); } catch { }

        System.Console.Write("\e[?1000l\e[?1006l");
        System.Console.CursorVisible = true;
        System.Console.ResetColor();
        System.Console.Clear();
    }
}
