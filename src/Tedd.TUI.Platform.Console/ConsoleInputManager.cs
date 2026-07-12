using System;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using Tedd.TUI;

namespace Tedd.TUI.Platform.Console;

public class ConsoleInputManager
{
    private readonly TuiWindow _window;
    private uint _lastButtonState;
    private readonly ConcurrentQueue<InputEvent> _inputQueue = new();
    private readonly AutoResetEvent _inputWaitHandle = new AutoResetEvent(false);
    // Read by the Unix reader thread, written by Start/Stop from other threads.
    private volatile bool _running;

    // Original console input mode captured before we patch it, so Stop() can hand the
    // terminal back the way we found it (raw mode used to leak into the parent shell).
    private uint _originalInputMode;
    private bool _inputModeSaved;
    private bool _mouseTrackingEnabled;

    private struct InputEvent
    {
        public ConsoleKeyInfo Key;
        public string Sequence;
    }

    public ConsoleInputManager(TuiWindow window)
    {
        _window = window;

        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
        {
            SetupWindowsConsole();
        }

        // Enable Mouse Tracking
        // CSI ? 1000 h  (Normal tracking)
        // CSI ? 1003 h  (All motion tracking)
        // CSI ? 1006 h  (SGR ext mode)
        // Skipped when stdout is redirected: the escapes would pollute piped output and
        // there is no terminal to interpret them anyway.
        if (!System.Console.IsOutputRedirected)
        {
            System.Console.Write("\x1b[?1000h\x1b[?1006h");
            _mouseTrackingEnabled = true;
        }
    }

    public IntPtr InputHandle { get; private set; }
    public WaitHandle InputWaitHandle => _inputWaitHandle;

    public void Start()
    {
        _running = true;
        if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
        {
            var t = new Thread(UnixInputLoop) { IsBackground = true, Name = "UnixInputReader" };
            t.Start();
        }
    }

    /// <summary>
    /// Stops input processing and restores the console to its pre-startup state:
    /// mouse tracking off and the original Windows console input mode re-applied.
    /// The Unix reader thread exits after its current blocking read completes (it is a
    /// background thread, so it never keeps the process alive).
    /// </summary>
    public void Stop()
    {
        _running = false;

        if (_mouseTrackingEnabled)
        {
            try { System.Console.Write("\x1b[?1000l\x1b[?1006l"); } catch { }
            _mouseTrackingEnabled = false;
        }

        if (_inputModeSaved)
        {
            try { NativeMethods.SetConsoleMode(InputHandle, _originalInputMode); } catch { }
            _inputModeSaved = false;
        }
    }

    private void UnixInputLoop()
    {
        while (_running)
        {
            try
            {
                if (System.Console.IsInputRedirected)
                {
                    Thread.Sleep(100);
                    continue;
                }

                // Blocking read
                var key = System.Console.ReadKey(true);

                string seq = null;
                // Check for Escape Sequence
                if (key.Key == ConsoleKey.Escape && System.Console.KeyAvailable)
                {
                    seq = ReadSequence();
                }

                _inputQueue.Enqueue(new InputEvent { Key = key, Sequence = seq });
                _inputWaitHandle.Set();
            }
            catch
            {
                // In case of error (e.g. strict environment), wait a bit to avoid hot loop
                Thread.Sleep(100);
            }
        }
    }

    public void ProcessInput()
    {
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
        {
            ProcessWindowsInput();
        }
        else
        {
            ProcessUnixInput();
        }
    }

    public event EventHandler WindowResized;

    private void ProcessWindowsInput()
    {
        var handle = NativeMethods.GetStdHandle(NativeMethods.STD_INPUT_HANDLE);
        // Check if anything is available. Both calls fail when stdin is not a real
        // console (redirected/pipe); bail out rather than acting on garbage counts.
        if (!NativeMethods.GetNumberOfConsoleInputEvents(handle, out uint numEvents)) return;
        if (numEvents == 0) return;

        var buffer = new NativeMethods.INPUT_RECORD[numEvents];
        if (!NativeMethods.ReadConsoleInput(handle, buffer, numEvents, out uint eventsRead)) return;

        for (int i = 0; i < eventsRead; i++)
        {
            var record = buffer[i];
            if (record.EventType == NativeMethods.KEY_EVENT)
            {
                // Emit both KeyDown and KeyUp so controls relying on release semantics
                // (e.g. ButtonBase.OnKeyUp triggering OnClick for ClickMode.Release,
                //  which RadioButton/CheckBox depend on for the Space/Enter activation)
                // work correctly on the Windows console backend.
                var routedEvent = record.KeyEvent.bKeyDown != 0
                    ? UIElement.KeyDownEvent
                    : UIElement.KeyUpEvent;

                var args = new KeyEventArgs(routedEvent)
                {
                    Key = (ConsoleKey)record.KeyEvent.wVirtualKeyCode,
                    KeyChar = record.KeyEvent.UnicodeChar,
                    Modifiers = GetModifiers(record.KeyEvent.dwControlKeyState)
                };
                _window.ProcessKey(args);
            }
            else if (record.EventType == NativeMethods.MOUSE_EVENT)
            {
                // Track Mouse State
                // dwButtonState: lowest bit is Left Button
                bool leftDown = (record.MouseEvent.dwButtonState & 0x01) != 0;
                bool wasLeftDown = (_lastButtonState & 0x01) != 0;

                _lastButtonState = record.MouseEvent.dwButtonState;

                // Note: Mouse coordinates are 0-based in current console window
                int x = record.MouseEvent.dwMousePosition.X;
                int y = record.MouseEvent.dwMousePosition.Y;

                _window.ProcessMouse(new MouseEventArgs(UIElement.MouseMoveEvent)
                {
                    GlobalX = x,
                    GlobalY = y
                });

                if (leftDown && !wasLeftDown)
                {
                    _window.ProcessMouse(new MouseEventArgs(UIElement.MouseDownEvent)
                    {
                        GlobalX = x,
                        GlobalY = y
                    });
                }

                if (!leftDown && wasLeftDown)
                {
                    _window.ProcessMouse(new MouseEventArgs(UIElement.MouseUpEvent)
                    {
                        GlobalX = x,
                        GlobalY = y
                    });
                }
            }
            else if (record.EventType == NativeMethods.WINDOW_BUFFER_SIZE_EVENT)
            {
                WindowResized?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private ConsoleModifiers GetModifiers(uint dwControlKeyState)
    {
        // Per wincon.h: RIGHT_ALT_PRESSED = 0x0001, LEFT_ALT_PRESSED = 0x0002,
        // RIGHT_CTRL_PRESSED = 0x0004, LEFT_CTRL_PRESSED = 0x0008, SHIFT_PRESSED = 0x0010.
        const uint RIGHT_ALT_PRESSED = 0x0001;
        const uint LEFT_ALT_PRESSED = 0x0002;
        const uint RIGHT_CTRL_PRESSED = 0x0004;
        const uint LEFT_CTRL_PRESSED = 0x0008;
        const uint SHIFT_PRESSED = 0x0010;

        ConsoleModifiers mod = 0;
        if ((dwControlKeyState & (LEFT_ALT_PRESSED | RIGHT_ALT_PRESSED)) != 0) mod |= ConsoleModifiers.Alt;
        if ((dwControlKeyState & SHIFT_PRESSED) != 0) mod |= ConsoleModifiers.Shift;
        if ((dwControlKeyState & (LEFT_CTRL_PRESSED | RIGHT_CTRL_PRESSED)) != 0) mod |= ConsoleModifiers.Control;
        return mod;
    }

    private void ProcessUnixInput()
    {
        while (_inputQueue.TryDequeue(out var item))
        {
            if (item.Sequence != null)
            {
                if (item.Sequence.StartsWith("[<"))
                {
                    ParseMouseSGR(item.Sequence);
                }
                else
                {
                    // Treat as normal Escape if not recognized or handle other VT keys
                    DispatchUnixKey(item.Key);
                }
            }
            else
            {
                DispatchUnixKey(item.Key);
            }
        }
    }

    private void DispatchUnixKey(ConsoleKeyInfo info)
    {
        _window.ProcessKey(ToKeyArgs(info, UIElement.KeyDownEvent));
        // Terminals report no key releases, so synthesize KeyUp right after KeyDown.
        // Controls with release semantics (ButtonBase defaults to ClickMode.Release,
        // which CheckBox/RadioButton rely on for Space/Enter) otherwise never activate
        // from the keyboard on Unix, while the Windows backend delivers real KeyUp events.
        _window.ProcessKey(ToKeyArgs(info, UIElement.KeyUpEvent));
    }

    private void SetupWindowsConsole()
    {
        try
        {
            var iStdIn = NativeMethods.GetStdHandle(NativeMethods.STD_INPUT_HANDLE);
            InputHandle = iStdIn;
            if (NativeMethods.GetConsoleMode(iStdIn, out uint inMode))
            {
                // Remember the pre-patch mode so Stop() can restore it.
                _originalInputMode = inMode;
                _inputModeSaved = true;

                // Disable Blocking / QuickEdit
                inMode &= ~NativeMethods.ENABLE_QUICK_EDIT_MODE;
                inMode &= ~NativeMethods.ENABLE_LINE_INPUT;
                inMode &= ~NativeMethods.ENABLE_ECHO_INPUT;

                inMode |= NativeMethods.ENABLE_EXTENDED_FLAGS;
                inMode |= NativeMethods.ENABLE_WINDOW_INPUT;
                inMode |= NativeMethods.ENABLE_MOUSE_INPUT;
                // DISABLE VT INPUT so we get raw INPUT_RECORDs for Mouse!
                inMode &= ~NativeMethods.ENABLE_VIRTUAL_TERMINAL_INPUT;

                NativeMethods.SetConsoleMode(iStdIn, inMode);
            }

            var iStdOut = NativeMethods.GetStdHandle(NativeMethods.STD_OUTPUT_HANDLE);
            if (NativeMethods.GetConsoleMode(iStdOut, out uint outMode))
            {
                outMode |= NativeMethods.ENABLE_VIRTUAL_TERMINAL_PROCESSING;
                outMode |= NativeMethods.DISABLE_NEWLINE_AUTO_RETURN;
                NativeMethods.SetConsoleMode(iStdOut, outMode);
            }
        }
        catch { }
    }

    private KeyEventArgs ToKeyArgs(ConsoleKeyInfo info, RoutedEvent routedEvent)
    {
        return new KeyEventArgs(routedEvent)
        {
            Key = info.Key,
            KeyChar = info.KeyChar,
            Modifiers = info.Modifiers
        };
    }

    private string ReadSequence()
    {
        // Reads the remainder of an escape sequence. The caller has already consumed
        // ESC and verified at least one more char is pending.
        //
        // Sequences can arrive split across reads (SSH, slow PTYs), so once a CSI has
        // started we wait briefly for the terminator instead of bailing the moment
        // KeyAvailable momentarily reads false — otherwise a truncated mouse report
        // leaks its tail into the app as garbage keystrokes.
        const int continuationTimeoutMs = 8;

        var sb = new StringBuilder();
        var deadline = Environment.TickCount64 + continuationTimeoutMs;

        while (true)
        {
            if (System.Console.KeyAvailable)
            {
                var k = System.Console.ReadKey(true);
                sb.Append(k.KeyChar);
                // The first char is the introducer ('[' for CSI, 'O' for SS3); both fall
                // in the final-byte range 0x40-0x7E, so testing it would truncate every
                // sequence to a single char (which is why SGR mouse never parsed).
                if (sb.Length > 1 && IsSequenceTerminator(k.KeyChar)) break;
                deadline = Environment.TickCount64 + continuationTimeoutMs;
            }
            else
            {
                // Only wait for continuation when we are clearly inside a CSI ("[...")
                // and haven't hit the terminator yet.
                bool midSequence = sb.Length > 0 && sb[0] == '[';
                if (!midSequence || Environment.TickCount64 >= deadline) break;
                Thread.Sleep(1);
            }
        }
        return sb.ToString();
    }

    private bool IsSequenceTerminator(char c)
    {
        return (c >= 64 && c <= 126); // @ to ~ are standard final bytes
    }

    private void ParseMouseSGR(string seq)
    {
        // Format: [<0;x;yM  or [<0;x;ym
        // 0: buttons (0=left, 1=middle, 2=right)
        // x, y: 1-based coordinates
        // M = press, m = release
        // Optimization: Time Complexity: O(N), Space Complexity: O(1)

        try
        {
            ReadOnlySpan<char> span = seq.AsSpan();

            if (span.Length < 6 || !span.StartsWith("[<")) return;

            ReadOnlySpan<char> clean = span.Slice(2);
            char lastChar = clean[^1];
            clean = clean.Slice(0, clean.Length - 1);

            int firstSemi = clean.IndexOf(';');
            if (firstSemi == -1) return;

            int secondSemi = clean.Slice(firstSemi + 1).IndexOf(';');
            if (secondSemi == -1) return;
            secondSemi += firstSemi + 1;

            if (int.TryParse(clean.Slice(0, firstSemi), out int btn) &&
                int.TryParse(clean.Slice(firstSemi + 1, secondSemi - firstSemi - 1), out int x) &&
                int.TryParse(clean.Slice(secondSemi + 1), out int y))
            {
                x -= 1;
                y -= 1;
                bool isDown = (lastChar == 'M');

                // Simple Left Click logic
                if (btn == 0)
                {
                    var routedEvent = isDown
                        ? UIElement.MouseDownEvent
                        : UIElement.MouseUpEvent;
                    _window.ProcessMouse(new MouseEventArgs(routedEvent)
                    {
                        GlobalX = x,
                        GlobalY = y
                    });
                }
            }
        }
        catch { }
    }
}
