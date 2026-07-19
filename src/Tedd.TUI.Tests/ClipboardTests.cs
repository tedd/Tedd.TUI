using System;
using System.IO;
using System.Text;
using Xunit;
using Tedd.TUI;
using Tedd.TUI.Platform.LinuxTerminal;

namespace Tedd.TUI.Tests;

/// <summary>
/// The clipboard service contract: the in-process buffer always works, platform
/// providers are best-effort, and provider failures never surface to callers.
/// Shares the "ClipboardState" collection because <see cref="Clipboard"/> is static.
/// </summary>
[Collection("ClipboardState")]
public class ClipboardTests : IDisposable
{
    public ClipboardTests()
    {
        Clipboard.Provider = null;
        Clipboard.SetText(string.Empty);
    }

    public void Dispose()
    {
        Clipboard.Provider = null;
        Clipboard.SetText(string.Empty);
    }

    private sealed class RecordingClipboard : IClipboard
    {
        public string? StoredText;
        public bool ReadReturnsNull;
        public bool ThrowOnAccess;

        public string? GetText()
        {
            if (ThrowOnAccess) throw new InvalidOperationException("clipboard unavailable");
            return ReadReturnsNull ? null : StoredText;
        }

        public void SetText(string text)
        {
            if (ThrowOnAccess) throw new InvalidOperationException("clipboard unavailable");
            StoredText = text;
        }
    }

    [Fact]
    public void RoundTrip_WithoutProvider_UsesInProcessBuffer()
    {
        Clipboard.SetText("buffered text");
        Assert.Equal("buffered text", Clipboard.GetText());
    }

    [Fact]
    public void SetText_ForwardsToProviderAndBuffer()
    {
        var provider = new RecordingClipboard();
        Clipboard.Provider = provider;

        Clipboard.SetText("shared");

        Assert.Equal("shared", provider.StoredText);
        Assert.Equal("shared", Clipboard.GetText());
    }

    [Fact]
    public void GetText_WriteOnlyProvider_FallsBackToBuffer()
    {
        // OSC 52 style: SetText works, GetText cannot read the host clipboard.
        var provider = new RecordingClipboard { ReadReturnsNull = true };
        Clipboard.Provider = provider;

        Clipboard.SetText("copied in-app");

        Assert.Equal("copied in-app", Clipboard.GetText());
    }

    [Fact]
    public void ProviderThrows_BufferStillWorks()
    {
        Clipboard.SetText("kept");
        Clipboard.Provider = new RecordingClipboard { ThrowOnAccess = true };

        // Neither call may throw; both fall back to the buffer.
        Clipboard.SetText("updated");
        Assert.Equal("updated", Clipboard.GetText());
    }

    [Fact]
    public void GetText_ProviderReturnsEmpty_IsAuthoritative()
    {
        // An empty (but readable) OS clipboard must not be replaced by stale buffer
        // content: pasting after another app cleared the clipboard pastes nothing.
        Clipboard.SetText("stale");
        Clipboard.Provider = new RecordingClipboard { StoredText = string.Empty };

        Assert.Equal(string.Empty, Clipboard.GetText());
    }

    [Fact]
    public void RegisterProvider_FirstRegistrationWins()
    {
        var first = new RecordingClipboard();
        var second = new RecordingClipboard();

        Clipboard.RegisterProvider(first);
        Clipboard.RegisterProvider(second);

        Assert.Same(first, Clipboard.Provider);
    }

    [Fact]
    public void Osc52Clipboard_WritesEscapeSequenceAndIsWriteOnly()
    {
        var clipboard = new Osc52Clipboard();
        var original = Console.Out;
        var capture = new StringWriter();
        try
        {
            Console.SetOut(capture);
            clipboard.SetText("Hello");
        }
        finally
        {
            Console.SetOut(original);
        }

        var expectedPayload = Convert.ToBase64String(Encoding.UTF8.GetBytes("Hello"));
        Assert.Equal($"\x1b]52;c;{expectedPayload}\x07", capture.ToString());
        Assert.Null(clipboard.GetText());
    }
}
