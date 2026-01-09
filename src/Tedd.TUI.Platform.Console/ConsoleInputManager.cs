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
        // Enable Mouse Tracking
        // CSI ? 1000 h  (Normal tracking)
        // CSI ? 1003 h  (All motion tracking)
        // CSI ? 1006 h  (SGR ext mode)
        System.Console.Write("\x1b[?1000h\x1b[?1006h");
    }

    public void ProcessInput()
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
                    // For now, ignore other sequences or re-dispatch Escape
                     _window.ProcessKey(ToKeyArgs(keyInfo));
                }
            }
            else
            {
                _window.ProcessKey(ToKeyArgs(keyInfo));
            }
        }
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
