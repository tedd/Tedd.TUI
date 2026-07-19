using System;
using System.Text;
using Tedd.TUI;

namespace Tedd.TUI.Platform.LinuxTerminal;

/// <summary>
/// <see cref="IClipboard"/> bridge for unix terminals using the OSC 52 escape sequence
/// (<c>ESC ] 52 ; c ; base64 BEL</c>). Copying pushes the text to the clipboard of the
/// terminal emulator the app runs in — supported by xterm, kitty, WezTerm, iTerm2,
/// Windows Terminal, and tmux (when configured) — and works across SSH sessions.
/// </summary>
/// <remarks>
/// OSC 52 is write-only in practice: most emulators disable clipboard *reads* for
/// security. <see cref="GetText"/> therefore returns <c>null</c>, which makes
/// <see cref="Clipboard.GetText"/> fall back to the in-process buffer, so paste still
/// returns whatever the application copied last.
/// </remarks>
public sealed class Osc52Clipboard : IClipboard
{
    // Terminals cap OSC 52 payloads (xterm's default limit allows ~74k of base64).
    // Truncate instead of emitting a sequence the terminal will discard entirely.
    private const int MaxTextLength = 50_000;

    public string? GetText() => null;

    public void SetText(string text)
    {
        text ??= string.Empty;
        if (text.Length > MaxTextLength) text = text[..MaxTextLength];

        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
        System.Console.Write($"\x1b]52;c;{payload}\x07");
        System.Console.Out.Flush();
    }
}
