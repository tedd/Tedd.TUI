using System;
using Avalonia.Input.Platform; // ClipboardExtensions (SetTextAsync)
using Avalonia.Threading;
using Tedd.TUI;

// Avalonia's own clipboard interface collides by name with Tedd.TUI.IClipboard; alias it.
using AvClipboard = Avalonia.Input.Platform.IClipboard;

namespace Tedd.TUI.Platform.Avalonia;

/// <summary>
/// <see cref="IClipboard"/> bridge to the Avalonia desktop clipboard, registered by
/// <see cref="TuiHostControl"/> once it is attached to a visual tree (a
/// <c>TopLevel</c> is required to reach the clipboard).
/// </summary>
/// <remarks>
/// Avalonia's clipboard API is asynchronous. <see cref="SetText"/> posts the write to the
/// UI thread and returns immediately, so the "Copy" affordance still reaches the OS
/// clipboard. <see cref="GetText"/> returns <c>null</c> (blocking on the async read from
/// the UI thread would deadlock), which makes the <see cref="Clipboard"/> service fall
/// back to its in-process buffer for in-app paste.
/// </remarks>
public sealed class AvaloniaClipboard : IClipboard
{
    private readonly Func<AvClipboard?> _resolveClipboard;

    public AvaloniaClipboard(Func<AvClipboard?> resolveClipboard)
    {
        _resolveClipboard = resolveClipboard;
    }

    public string? GetText() => null;

    public void SetText(string text)
    {
        string value = text ?? string.Empty;
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                _resolveClipboard()?.SetTextAsync(value);
            }
            catch
            {
                // Best-effort: the Clipboard service already mirrored the text into its
                // in-process buffer, so in-app paste keeps working.
            }
        });
    }
}
