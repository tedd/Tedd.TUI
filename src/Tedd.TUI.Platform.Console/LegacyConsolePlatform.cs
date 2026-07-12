using System;
using Tedd.TUI;

namespace Tedd.TUI.Platform.Console;

/// <summary>
/// Legacy 16-color <see cref="ITuiPlatform"/> implementation backed by
/// <see cref="ConsoleRenderer"/> and <see cref="ConsoleInputManager"/>. Used as the
/// fallback when no truecolor backend (Windows Terminal, Linux terminal) is referenced
/// by the host application.
/// </summary>
public sealed class LegacyConsolePlatform : ITuiPlatform
{
    private ConsoleRenderer? _renderer;
    private LegacyInputAdapter? _input;

    public SurfaceCapabilities Capabilities { get; }

    /// <summary>The static terminal capability snapshot used to build <see cref="Capabilities"/>.</summary>
    public TerminalProfile Profile { get; }

    public IImageProtocolEncoder? ImageEncoder => null;

    public LegacyConsolePlatform()
        : this(TerminalProbe.Detect())
    {
    }

    public LegacyConsolePlatform(TerminalProfile profile)
    {
        Profile = profile ?? throw new ArgumentNullException(nameof(profile));
        Capabilities = new SurfaceCapabilities
        {
            // The legacy path can't display bitmaps regardless of the host's advertised
            // protocol; that is the job of the WindowsTerminal / LinuxTerminal backends.
            SupportsGraphics = false,
        };
    }

    public IRenderer CreateRenderer() => _renderer ??= new ConsoleRenderer();

    public ITuiInputManager? CreateInputManager(TuiWindow window)
    {
        _input ??= new LegacyInputAdapter(new ConsoleInputManager(window));
        return _input;
    }

    public void Initialize() { /* ConsoleRenderer / ConsoleInputManager handle setup eagerly */ }

    public void Shutdown()
    {
        try { _input?.Stop(); } catch { }
    }

    public void Dispose() => Shutdown();
}

/// <summary>
/// Adapts the original <see cref="ConsoleInputManager"/> (which predates
/// <see cref="ITuiInputManager"/>) to the new platform contract without changing its
/// existing public surface.
/// </summary>
internal sealed class LegacyInputAdapter : ITuiInputManager
{
    public ConsoleInputManager Inner { get; }

    public event EventHandler? WindowResized
    {
        add { Inner.WindowResized += value; }
        remove { Inner.WindowResized -= value; }
    }

    public LegacyInputAdapter(ConsoleInputManager inner)
    {
        Inner = inner;
    }

    public void Start() => Inner.Start();
    public void Stop() => Inner.Stop();
    public void Dispose() => Stop();
}
