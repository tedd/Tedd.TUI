using Microsoft.JSInterop;
using Tedd.TUI;

namespace Tedd.TUI.Platform.Blazor;

/// <summary>
/// <see cref="IClipboard"/> bridge to the browser clipboard via the async JS
/// <c>navigator.clipboard</c> API, registered by <see cref="BlazorRenderer"/> so TUI text
/// controls copy through the system clipboard.
/// </summary>
/// <remarks>
/// JS interop is asynchronous. <see cref="SetText"/> fires the write without blocking;
/// <see cref="GetText"/> returns <c>null</c> (a synchronous read isn't possible, and
/// <c>navigator.clipboard.readText</c> needs a user permission prompt), so the
/// <see cref="Clipboard"/> service falls back to its in-process buffer for in-app paste.
/// Writing requires a secure context (HTTPS or localhost).
/// </remarks>
public sealed class BlazorClipboard : IClipboard
{
    private readonly IJSRuntime _js;

    public BlazorClipboard(IJSRuntime js)
    {
        _js = js;
    }

    public string? GetText() => null;

    public void SetText(string text)
    {
        // Fire-and-forget: the Clipboard service already mirrored the text into its
        // in-process buffer, so in-app paste works even if the browser rejects the write.
        _ = _js.InvokeVoidAsync("navigator.clipboard.writeText", text ?? string.Empty);
    }
}
