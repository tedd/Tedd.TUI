using Tedd.TUI;

// Same namespace-collision rule as TuiHostElement: the enclosing namespace makes the
// unqualified Clipboard resolve to Tedd.TUI.Clipboard; WPF's is aliased explicitly.
using SysClipboard = System.Windows.Clipboard;

namespace Tedd.TUI.Platform.Wpf;

/// <summary>
/// <see cref="IClipboard"/> bridge to the WPF/Win32 clipboard, registered by
/// <see cref="TuiHostElement"/> so TUI text controls copy/paste through the desktop
/// clipboard. Must be used from the STA UI thread, which is where all TUI input runs
/// in this host.
/// </summary>
public sealed class WpfClipboard : IClipboard
{
    public string? GetText()
    {
        // Empty string when the clipboard holds no text is authoritative (prevents the
        // fallback buffer from pasting stale text); null is reserved for access failure,
        // which SysClipboard signals by throwing (handled by the Clipboard service).
        return SysClipboard.ContainsText() ? SysClipboard.GetText() : string.Empty;
    }

    public void SetText(string text) => SysClipboard.SetText(text);
}
