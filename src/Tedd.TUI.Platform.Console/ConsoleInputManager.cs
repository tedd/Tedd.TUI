using System;
using System.Text;
using System.Threading;
using Tedd.TUI;

namespace Tedd.TUI.Platform.Console;

public class ConsoleInputManager
{
    private readonly TuiWindow _window;

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
        System.Console.Write("\x1b[?1000h\x1b[?1006h");
    }

    public IntPtr InputHandle { get; private set; }

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

    private void ProcessWindowsInput()
    {
        var handle = NativeMethods.GetStdHandle(NativeMethods.STD_INPUT_HANDLE);
        uint eventsRead;
        // Check if anything is available
        NativeMethods.GetNumberOfConsoleInputEvents(handle, out uint numEvents);
        if (numEvents == 0) return;

        var buffer = new NativeMethods.INPUT_RECORD[numEvents];
        NativeMethods.ReadConsoleInput(handle, buffer, numEvents, out eventsRead);

        for (int i = 0; i < eventsRead; i++)
        {
            var record = buffer[i];
            if (record.EventType == NativeMethods.KEY_EVENT)
            {
                if (record.KeyEvent.bKeyDown != 0) // bKeyDown is int (BOOL)
                {
                    // Map to KeyEventArgs
                    var args = new KeyEventArgs
                    {
                        Key = (ConsoleKey)record.KeyEvent.wVirtualKeyCode,
                        KeyChar = record.KeyEvent.UnicodeChar,
                        Modifiers = GetModifiers(record.KeyEvent.dwControlKeyState)
                    };
                    _window.ProcessKey(args);
                }
            }
            else if (record.EventType == NativeMethods.MOUSE_EVENT)
            {
                // Simple click handling
                // Check for Left Button Press
                // dwButtonState: lowest bit is Left Button
                bool leftDown = (record.MouseEvent.dwButtonState & 0x01) != 0;
                
                // We need to track state to know if it's Up or Down.
                // The record tells us the CURRENT state of buttons.
                // If we want MOUSE_DOWN / MOUSE_UP, we might need a state machine or just fire MOUSE_DOWN on every event where it is down?
                // Standard TUI usually wants clicks. 
                // Let's implement simple "Hit Test" interaction
                
                // Note: Mouse coordinates are 0-based in current console window
                int x = record.MouseEvent.dwMousePosition.X;
                int y = record.MouseEvent.dwMousePosition.Y;
                
                var hit = _window.InputHitTest(x, y);
                if (hit != null)
                {
                     // Ideally we track _lastMouseState to detect edges
                     var args = new MouseEventArgs { X = hit.LocalX, Y = hit.LocalY };
                     // For now, if button is down, treat as MouseDown. 
                     // This repeats while dragging.
                     if (leftDown) hit.Element.OnMouseDown(args);
                     // If we want MouseUp, we need to know previous state.
                     // But let's start with basic Click support via MouseDown.
                }
            }
        }
    }

    private ConsoleModifiers GetModifiers(uint dwControlKeyState)
    {
        ConsoleModifiers mod = 0;
        const uint LEFT_ALT_PRESSED = 0x0002;
        const uint RIGHT_ALT_PRESSED = 0x0001; // wait, check MSDN
        const uint SHIFT_PRESSED = 0x0010;
        const uint LEFT_CTRL_PRESSED = 0x0008;
        const uint RIGHT_CTRL_PRESSED = 0x0004;

        if ((dwControlKeyState & (LEFT_ALT_PRESSED | 0x0001)) != 0) mod |= ConsoleModifiers.Alt;
        if ((dwControlKeyState & SHIFT_PRESSED) != 0) mod |= ConsoleModifiers.Shift;
        if ((dwControlKeyState & (LEFT_CTRL_PRESSED | RIGHT_CTRL_PRESSED)) != 0) mod |= ConsoleModifiers.Control;
        return mod;
    }

    private void ProcessUnixInput()
    {
        while (System.Console.KeyAvailable)
        {
            // Read first key
            var keyInfo = System.Console.ReadKey(true);

            // Check for Escape Sequence
            if (keyInfo.Key == ConsoleKey.Escape && System.Console.KeyAvailable)
            {
                // Likely a sequence
                var seq = ReadSequence();
                if (seq.StartsWith("[<"))
                {
                    ParseMouseSGR(seq);
                }
                else
                {
                    // Treat as normal Escape if not recognized or handle other VT keys
                     _window.ProcessKey(ToKeyArgs(keyInfo));
                }
            }
            else
            {
                _window.ProcessKey(ToKeyArgs(keyInfo));
            }
        }
    }

    private void SetupWindowsConsole()
    {
        try
        {
            var iStdIn = NativeMethods.GetStdHandle(NativeMethods.STD_INPUT_HANDLE);
            InputHandle = iStdIn;
            if (NativeMethods.GetConsoleMode(iStdIn, out uint inMode))
            {
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
        catch {}
    }

    private KeyEventArgs ToKeyArgs(ConsoleKeyInfo info)
    {
        return new KeyEventArgs
        {
            Key = info.Key,
            KeyChar = info.KeyChar,
            Modifiers = info.Modifiers
        };
    }

    private string ReadSequence()
    {
        // Simple synchronous read of available chars
        // A sequence usually comes in fast.
        var sb = new StringBuilder();
        // We already consumed ESC

        // Loop reading until end of sequence char or timeout?
        // SGR mouse ends with 'm' or 'M'.
        // Let's just read what's there for a simplified approach.
        // Or read char by char.

        while (System.Console.KeyAvailable)
        {
             var k = System.Console.ReadKey(true);
             sb.Append(k.KeyChar);
             if (IsSequenceTerminator(k.KeyChar)) break;
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

        try
        {
            var clean = seq.Substring(2); // Remove [<
            var lastChar = clean[clean.Length - 1];
            clean = clean.Substring(0, clean.Length - 1);

            var parts = clean.Split(';');
            if (parts.Length >= 3)
            {
                int btn = int.Parse(parts[0]);
                int x = int.Parse(parts[1]) - 1;
                int y = int.Parse(parts[2]) - 1;

                bool isDown = (lastChar == 'M');

                // Simple Left Click logic
                if (btn == 0)
                {
                    // HitTest returns result with local coordinates now
                    var result = _window.InputHitTest(x, y);
                    if (result != null)
                    {
                        var args = new MouseEventArgs { X = result.LocalX, Y = result.LocalY };
                        if (isDown) result.Element.OnMouseDown(args);
                        else result.Element.OnMouseUp(args);
                    }
                }
            }
        }
        catch {}
    }
}
