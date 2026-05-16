using System;

namespace Tedd.TUI;

/// <summary>
/// Abstraction over a host (Windows Terminal, Linux/macOS terminal, Blazor surface, …)
/// that a <c>TuiApp</c> renders into. A platform owns the input pipeline, the output
/// renderer, the capability profile, and any optional bitmap encoder used for Sixel /
/// Kitty / iTerm2 images.
/// </summary>
/// <remarks>
/// <para>The interface is intentionally tiny: anything more elaborate (mouse / focus /
/// resize wiring) lives on the implementations because each backend negotiates these
/// differently. The auto-detecting <c>PlatformLoader</c> in
/// <c>Tedd.TUI.Platform.Console</c> resolves the best available implementation at
/// startup and falls back to the legacy 16-color <c>ConsoleRenderer</c> when no
/// truecolor backend is referenced.</para>
/// </remarks>
public interface ITuiPlatform : IDisposable
{
    /// <summary>Capabilities advertised by the platform (truecolor, image protocols, …).</summary>
    SurfaceCapabilities Capabilities { get; }

    /// <summary>
    /// Creates (or returns the cached) <see cref="IRenderer"/> responsible for emitting
    /// flattened <see cref="VirtualBuffer"/> frames to the host surface.
    /// </summary>
    IRenderer CreateRenderer();

    /// <summary>
    /// Creates the platform's input pipeline bound to <paramref name="window"/>. Platforms
    /// that have no input concept (e.g. headless test harnesses) may return <c>null</c>.
    /// </summary>
    ITuiInputManager? CreateInputManager(TuiWindow window);

    /// <summary>
    /// Optional bitmap encoder for Sixel / Kitty / iTerm2 inline images. <c>null</c> when
    /// the host can't render bitmaps; image-aware controls fall back to ASCII art.
    /// </summary>
    IImageProtocolEncoder? ImageEncoder { get; }

    /// <summary>One-time setup: enables VT mode, raw input, alt screen, etc.</summary>
    void Initialize();

    /// <summary>Reverses <see cref="Initialize"/>: restores the terminal to its original state.</summary>
    void Shutdown();
}

/// <summary>
/// Minimal input pipeline contract used by <c>TuiApp</c>. Each platform implementation
/// (Windows Terminal, Linux terminal, …) is responsible for translating raw key / mouse
/// bytes into <c>InputEvent</c>s and dispatching them to the bound window.
/// </summary>
public interface ITuiInputManager : IDisposable
{
    /// <summary>Begins reading input. Implementations typically spin up a background thread.</summary>
    void Start();

    /// <summary>Stops reading input and tears down any background workers.</summary>
    void Stop();

    /// <summary>
    /// Raised when the host surface resizes. Consumers (the render loop) re-measure and
    /// re-render in response.
    /// </summary>
    event EventHandler? WindowResized;
}

/// <summary>
/// Bitmap encoder contract used by <c>VirtualBuffer.Graphics</c> consumers. Implementations
/// translate <c>GraphicPlacement</c> values into the host's native escape sequence
/// (Sixel, Kitty, iTerm2 inline images, …).
/// </summary>
public interface IImageProtocolEncoder
{
    /// <summary>Short identifier for diagnostics (<c>"sixel"</c>, <c>"kitty"</c>, <c>"iterm2"</c>).</summary>
    string Protocol { get; }

    /// <summary>
    /// Encodes <paramref name="placement"/> into the protocol's escape sequence, ready
    /// to be written to stdout at the placement's character-cell position.
    /// </summary>
    string Encode(GraphicPlacement placement);
}
