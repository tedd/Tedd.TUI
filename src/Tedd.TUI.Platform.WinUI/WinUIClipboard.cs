using Tedd.TUI;

using WinUIDataPackage = Windows.ApplicationModel.DataTransfer.DataPackage;
using WinUISystemClipboard = Windows.ApplicationModel.DataTransfer.Clipboard;

namespace Tedd.TUI.Platform.WinUI;

/// <summary>
/// <see cref="IClipboard"/> bridge to the Windows system clipboard
/// (<c>Windows.ApplicationModel.DataTransfer.Clipboard</c>), registered by
/// <see cref="TuiHostControl"/> so TUI text controls copy through the desktop clipboard.
/// </summary>
/// <remarks>
/// <see cref="SetText"/> is synchronous. <see cref="GetText"/> returns <c>null</c> -- the
/// WinRT read path is async-only and blocking it on the UI thread would deadlock -- so the
/// <see cref="Clipboard"/> service falls back to its in-process buffer for in-app paste.
/// </remarks>
public sealed class WinUIClipboard : IClipboard
{
    public string? GetText() => null;

    public void SetText(string text)
    {
        var package = new WinUIDataPackage();
        package.SetText(text ?? string.Empty);
        WinUISystemClipboard.SetContent(package);
    }
}
