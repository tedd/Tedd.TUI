using Tedd.TUI;

using MauiApplicationModel = Microsoft.Maui.ApplicationModel;
using MauiSystemClipboard = Microsoft.Maui.ApplicationModel.DataTransfer.Clipboard;

namespace Tedd.TUI.Platform.Maui;

/// <summary>
/// <see cref="IClipboard"/> bridge to the .NET MAUI Essentials clipboard, registered by
/// <see cref="TuiHostView"/> so TUI text controls copy through the platform clipboard on
/// every MAUI target (Android, iOS, Windows, Mac Catalyst).
/// </summary>
/// <remarks>
/// MAUI's clipboard API is asynchronous. <see cref="SetText"/> marshals to the main thread
/// and fires the write without blocking; <see cref="GetText"/> returns <c>null</c> so the
/// <see cref="Clipboard"/> service falls back to its in-process buffer for in-app paste.
/// </remarks>
public sealed class MauiClipboard : IClipboard
{
    public string? GetText() => null;

    public void SetText(string text)
    {
        string value = text ?? string.Empty;
        MauiApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                _ = MauiSystemClipboard.Default.SetTextAsync(value);
            }
            catch
            {
                // Best-effort: the Clipboard service already holds the text in its
                // in-process buffer, so in-app paste still works.
            }
        });
    }
}
