using System;
using System.Runtime.InteropServices;
using Tedd.TUI;
using Tedd.TUI.Platform.Console;

namespace Tedd.TUI.Platform.LinuxTerminal;

/// <summary>
/// <see cref="ITuiPlatform"/> implementation for unix terminals (Linux + macOS). Owns
/// the termios raw-mode dance, the alt-screen switch, and the SIGWINCH watcher; defers
/// rendering to the shared <see cref="AnsiTrueColorRenderer"/> and image emission to
/// the per-protocol encoders selected via the supplied <see cref="TerminalProfile"/>.
/// </summary>
/// <remarks>
/// <para>The implementation is intentionally conservative: if raw-mode setup fails (e.g.
/// stdin isn't a tty because we're running under a CI harness) we skip the termios
/// flips and let the legacy buffered path take over — rendering still works, only
/// input fidelity degrades.</para>
/// </remarks>
public sealed class LinuxTerminalPlatform : ITuiPlatform
{
    private AnsiTrueColorRenderer? _renderer;
    private LinuxInputManager? _input;
    private Termios.LinuxTermios _originalTermios;
    private bool _termiosPatched;
    private bool _altScreenEntered;

    public TerminalProfile Profile { get; }
    public SurfaceCapabilities Capabilities { get; }
    public IImageProtocolEncoder? ImageEncoder { get; }

    public LinuxTerminalPlatform()
        : this(TerminalProbe.Detect())
    {
    }

    public LinuxTerminalPlatform(TerminalProfile profile)
    {
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));

        ImageEncoder = profile.ImageProtocol switch
        {
            TerminalImageProtocol.Kitty => new KittyGraphicsEncoder(),
            TerminalImageProtocol.ITerm2 => new ITerm2InlineEncoder(),
            TerminalImageProtocol.Sixel => new SixelEncoderAdapter(),
            _ => null,
        };

        Capabilities = new SurfaceCapabilities
        {
            SupportsGraphics = ImageEncoder != null,
            CharPixelWidth = 8,
            CharPixelHeight = 16,
        };
    }

    public IRenderer CreateRenderer()
    {
        if (_renderer != null) return _renderer;
        _renderer = new AnsiTrueColorRenderer { ImageEncoder = ImageEncoder };
        return _renderer;
    }

    public ITuiInputManager? CreateInputManager(TuiWindow window) => _input ??= new LinuxInputManager(window);

    public void Initialize()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
            !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return;
        }

        try
        {
            if (Termios.isatty(Termios.STDIN_FILENO) != 1) return;

            if (Termios.tcgetattr(Termios.STDIN_FILENO, out var original) == 0)
            {
                _originalTermios = original;
                var raw = original;

                // Standard "raw mode" recipe from termios(3): drop echo, canonical mode,
                // signal generation, extended input processing; clear input processing
                // bits that would mangle escape sequences.
                raw.c_lflag &= ~(Termios.ECHO | Termios.ICANON | Termios.ISIG | Termios.IEXTEN);
                raw.c_iflag &= ~(Termios.IXON | Termios.ICRNL | Termios.BRKINT | Termios.INPCK | Termios.ISTRIP);
                raw.c_oflag &= ~Termios.OPOST;

                if (Termios.tcsetattr(Termios.STDIN_FILENO, Termios.TCSANOW, in raw) == 0)
                {
                    _termiosPatched = true;
                }
            }
        }
        catch
        {
            // Non-tty / no libc / sandboxed: we leave the terminal alone.
        }

        try
        {
            // Switch to the alt screen, enable SGR mouse mode, hide the cursor.
            System.Console.Write("\x1b[?1049h\x1b[?1000h\x1b[?1006h\x1b[?25l");
            _altScreenEntered = true;
        }
        catch { }
    }

    public void Shutdown()
    {
        if (_altScreenEntered)
        {
            try { System.Console.Write("\x1b[?25h\x1b[?1006l\x1b[?1000l\x1b[?1049l"); } catch { }
            _altScreenEntered = false;
        }

        if (_termiosPatched)
        {
            try { Termios.tcsetattr(Termios.STDIN_FILENO, Termios.TCSANOW, in _originalTermios); } catch { }
            _termiosPatched = false;
        }

        try { _input?.Stop(); } catch { }
    }

    public void Dispose() => Shutdown();

    private sealed class SixelEncoderAdapter : IImageProtocolEncoder
    {
        public string Protocol => "sixel";
        public string Encode(GraphicPlacement placement) => SixelEncoderCore.Encode(placement);
    }
}
