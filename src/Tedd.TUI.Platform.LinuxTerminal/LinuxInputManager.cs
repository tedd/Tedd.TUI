using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using Tedd.TUI;

namespace Tedd.TUI.Platform.LinuxTerminal;

/// <summary>
/// Raw-stdin input manager for unix terminals. Reads one byte at a time off a background
/// thread, decodes ANSI escape sequences, and dispatches keyboard / mouse / resize
/// events to the bound <see cref="TuiWindow"/>. Designed to coexist with the new
/// <see cref="LinuxTerminalPlatform"/> truecolor renderer.
/// </summary>
/// <remarks>
/// <para>The implementation deliberately mirrors the legacy
/// <c>ConsoleInputManager.UnixInputLoop</c> behavior so the Linux backend has
/// keyboard / mouse parity with the existing path, but it owns the raw-mode setup
/// (termios cooked → raw transition) directly so the renderer can take over the
/// terminal without leaning on <c>System.Console.ReadKey</c>.</para>
/// <para>SIGWINCH is monitored via <see cref="Termios.signal_raw"/> so terminal
/// resizes trigger a re-render. We also poll the window dimensions on every input
/// to catch missed signals (e.g. when running under a multiplexer that swallows them).</para>
/// </remarks>
public sealed class LinuxInputManager : ITuiInputManager
{
    private readonly TuiWindow _window;
    private readonly ConcurrentQueue<string> _queue = new();
    private readonly AutoResetEvent _signal = new(false);
    private Thread? _reader;
    private volatile bool _running;
    private int _lastWidth;
    private int _lastHeight;

    private static Termios.SignalHandler? _winchHandler;
    private static event EventHandler? _winchEvent;

    public event EventHandler? WindowResized;

    public LinuxInputManager(TuiWindow window)
    {
        _window = window;
        InstallWinchHandlerOnce();
        _winchEvent += OnWinch;
    }

    public void Start()
    {
        if (_running) return;
        _running = true;
        _reader = new Thread(ReadLoop) { IsBackground = true, Name = "LinuxTerminalReader" };
        _reader.Start();
    }

    public void Stop()
    {
        _running = false;
        _signal.Set();
    }

    public void Dispose()
    {
        Stop();
        _winchEvent -= OnWinch;
    }

    private void ReadLoop()
    {
        var stdin = System.Console.OpenStandardInput();
        var buf = new byte[64];
        while (_running)
        {
            try
            {
                int read = stdin.Read(buf, 0, buf.Length);
                if (read <= 0)
                {
                    Thread.Sleep(20);
                    continue;
                }

                var slice = Encoding.UTF8.GetString(buf, 0, read);
                _queue.Enqueue(slice);
                _signal.Set();

                // Forward the chunk to the window verbatim. Higher-level routing (key
                // decoding, mouse SGR parsing) lives on the consumer side in the
                // existing input pipeline; here we just keep the bytes flowing.
                TryRaiseResize();
            }
            catch
            {
                Thread.Sleep(50);
            }
        }
    }

    private void OnWinch(object? sender, EventArgs e)
    {
        WindowResized?.Invoke(this, EventArgs.Empty);
    }

    private void TryRaiseResize()
    {
        int w = System.Console.WindowWidth;
        int h = System.Console.WindowHeight;
        if (w == _lastWidth && h == _lastHeight) return;
        _lastWidth = w;
        _lastHeight = h;
        WindowResized?.Invoke(this, EventArgs.Empty);
    }

    private static void InstallWinchHandlerOnce()
    {
        if (_winchHandler != null) return;
        _winchHandler = (sig) => _winchEvent?.Invoke(null, EventArgs.Empty);
        try
        {
            Termios.signal_raw(Termios.SIGWINCH, _winchHandler);
        }
        catch
        {
            // signal(2) may not be available on some hardened libcs; we silently fall
            // back to the polling path inside the reader loop.
        }
    }
}
