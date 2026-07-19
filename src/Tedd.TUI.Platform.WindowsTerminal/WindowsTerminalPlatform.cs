using System;
using System.Runtime.InteropServices;
using System.Text;
using Tedd.TUI;
using Tedd.TUI.Platform.Console;

namespace Tedd.TUI.Platform.WindowsTerminal;

/// <summary>
/// <see cref="ITuiPlatform"/> implementation for modern Windows hosts that speak VT
/// (Windows Terminal 1.x, conhost on Windows 10 1809+, VS Code integrated terminal, …).
/// Enables <c>ENABLE_VIRTUAL_TERMINAL_PROCESSING</c> + <c>DISABLE_NEWLINE_AUTO_RETURN</c>
/// on the output handle and <c>ENABLE_VIRTUAL_TERMINAL_INPUT</c> on the input handle so
/// the shared <see cref="AnsiTrueColorRenderer"/> can emit raw escape sequences without
/// the legacy <c>System.Console</c> color quantization in the way.
/// </summary>
public sealed class WindowsTerminalPlatform : ITuiPlatform
{
    private AnsiTrueColorRenderer? _renderer;
    private ITuiInputManager? _input;
    private uint _previousOutputMode;
    private bool _outputModePatched;
    private Encoding? _previousOutputEncoding;
    private bool _outputEncodingPatched;

    /// <summary>Profile observed by <see cref="TerminalProbe"/> at construction time.</summary>
    public TerminalProfile Profile { get; }

    public SurfaceCapabilities Capabilities { get; }
    public IImageProtocolEncoder? ImageEncoder { get; }

    public WindowsTerminalPlatform()
        : this(TerminalProbe.Detect())
    {
    }

    public WindowsTerminalPlatform(TerminalProfile profile)
    {
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));

        // Windows Terminal 1.22+ ships Sixel; older builds (and conhost) don't, but the
        // encoder is itself a no-op when the protocol is unsupported.
        ImageEncoder = profile.ImageProtocol == TerminalImageProtocol.Sixel
            ? new SixelEncoder()
            : null;

        Capabilities = new SurfaceCapabilities
        {
            SupportsGraphics = ImageEncoder != null,
            CharPixelWidth = 10,
            CharPixelHeight = 20,
        };
    }

    public IRenderer CreateRenderer()
    {
        if (_renderer != null) return _renderer;
        _renderer = new AnsiTrueColorRenderer { ImageEncoder = ImageEncoder };
        return _renderer;
    }

    public ITuiInputManager? CreateInputManager(TuiWindow window)
    {
        // The legacy ConsoleInputManager already supports VT-style escape sequences and
        // the Win32 input record API. We reuse it here so the WindowsTerminal backend
        // gets full keyboard / mouse / resize support without rewriting the pipeline.
        _input ??= new LegacyConsolePlatform(Profile).CreateInputManager(window);
        return _input;
    }

    public void Initialize()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        Clipboard.RegisterProvider(new Win32Clipboard());

        try
        {
            _previousOutputEncoding = System.Console.OutputEncoding;
            System.Console.OutputEncoding = Encoding.UTF8;
            _outputEncodingPatched = true;
        }
        catch
        {
            // Some hosts (redirected stdout, certain test runners) refuse this.
            // We fall through; output will still render but heavy box-drawing
            // glyphs may be transcoded by the OEM codepage.
        }

        try
        {
            var outHandle = Win32.GetStdHandle(Win32.STD_OUTPUT_HANDLE);
            if (Win32.GetConsoleMode(outHandle, out _previousOutputMode))
            {
                uint mode = _previousOutputMode
                    | Win32.ENABLE_VIRTUAL_TERMINAL_PROCESSING
                    | Win32.DISABLE_NEWLINE_AUTO_RETURN;
                if (Win32.SetConsoleMode(outHandle, mode))
                {
                    _outputModePatched = true;
                }
            }

            // Input-mode configuration is intentionally NOT done here. The shared
            // ConsoleInputManager (created by CreateInputManager right after this)
            // owns the input mode: it requires raw INPUT_RECORDs (VT input disabled)
            // for mouse support, and it saves/restores the original mode itself.
            // Patching it here too made the two components fight over the same handle
            // and double-restore different "previous" modes at shutdown.
        }
        catch
        {
            // Best-effort: the renderer will still emit ANSI; without VT the output just
            // looks like garbage glyphs. We don't want platform setup to crash the host.
        }
    }

    public void Shutdown()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

        try
        {
            if (_outputEncodingPatched && _previousOutputEncoding != null)
            {
                System.Console.OutputEncoding = _previousOutputEncoding;
                _outputEncodingPatched = false;
            }
            if (_outputModePatched)
            {
                Win32.SetConsoleMode(Win32.GetStdHandle(Win32.STD_OUTPUT_HANDLE), _previousOutputMode);
                _outputModePatched = false;
            }
        }
        catch { }
    }

    public void Dispose() => Shutdown();
}
