using System;

namespace Tedd.TUI;

/// <summary>
/// Platform clipboard contract. Implementations bridge to the host's native clipboard
/// (Win32, OSC 52, WPF, …). All members are synchronous; hosts whose native clipboard
/// is async-only can simply not register a provider and rely on the built-in
/// in-process buffer in <see cref="Clipboard"/>.
/// </summary>
public interface IClipboard
{
    /// <summary>
    /// Returns the clipboard text, or <c>null</c> when the platform cannot read its
    /// clipboard (e.g. OSC 52 terminals, which are write-only). A <c>null</c> return
    /// makes <see cref="Clipboard.GetText"/> fall back to the in-process buffer.
    /// </summary>
    string? GetText();

    /// <summary>Places <paramref name="text"/> on the platform clipboard.</summary>
    void SetText(string text);
}

/// <summary>
/// Application-wide clipboard service used by text controls (copy / cut / paste).
/// </summary>
/// <remarks>
/// <para>Every <see cref="SetText"/> is mirrored into an in-process selection buffer
/// before being forwarded (best-effort) to the registered <see cref="Provider"/>.
/// <see cref="GetText"/> prefers the provider and falls back to the buffer when no
/// provider is registered, the provider cannot read (returns <c>null</c>), or it
/// throws. Copy/paste inside the application therefore always works, even on hosts
/// with no clipboard support at all.</para>
/// <para>Platform hosts register their provider during platform initialization; the
/// first registration wins so an outer host (e.g. WPF) is not overwritten by a nested
/// fallback platform.</para>
/// </remarks>
public static class Clipboard
{
    private static readonly object _sync = new();
    private static IClipboard? _provider;
    private static string _buffer = string.Empty;

    /// <summary>The active platform clipboard bridge, or <c>null</c> for buffer-only mode.</summary>
    public static IClipboard? Provider
    {
        get { lock (_sync) return _provider; }
        set { lock (_sync) _provider = value; }
    }

    /// <summary>
    /// Registers <paramref name="provider"/> only when no provider is set yet. Platform
    /// Initialize() paths use this so the most specific host keeps priority.
    /// </summary>
    public static void RegisterProvider(IClipboard provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        lock (_sync)
        {
            _provider ??= provider;
        }
    }

    /// <summary>Gets the clipboard text; never throws, never returns <c>null</c>.</summary>
    public static string GetText()
    {
        IClipboard? provider;
        lock (_sync) provider = _provider;

        if (provider != null)
        {
            try
            {
                var text = provider.GetText();
                if (text != null) return text;
            }
            catch
            {
                // Providers talk to OS facilities that can fail transiently (clipboard
                // locked by another process, terminal gone). Fall back to the buffer.
            }
        }

        lock (_sync) return _buffer;
    }

    /// <summary>Sets the clipboard text; the in-process buffer always receives it.</summary>
    public static void SetText(string text)
    {
        text ??= string.Empty;

        IClipboard? provider;
        lock (_sync)
        {
            _buffer = text;
            provider = _provider;
        }

        if (provider != null)
        {
            try
            {
                provider.SetText(text);
            }
            catch
            {
                // Best-effort: the buffer already holds the text, so in-app paste works.
            }
        }
    }
}
