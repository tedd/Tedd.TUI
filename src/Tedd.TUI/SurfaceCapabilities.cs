namespace Tedd.TUI;

/// <summary>
/// Describes the rendering capabilities of the surface a <see cref="TuiWindow"/> is hosted on.
/// Controls (e.g. images) read this to decide between text-only rendering and richer rendering
/// such as bitmap overlays. The default is <see cref="TextOnly"/>; renderers that can do more
/// (e.g. Blazor DOM) set their own instance on the window.
/// </summary>
public sealed class SurfaceCapabilities
{
    /// <summary>
    /// When true the surface can accept bitmap graphics overlaid on top of the character grid
    /// via <see cref="VirtualBuffer.Graphics"/>. When false the surface is text-only and graphics-
    /// aware controls must fall back to text/ASCII rendering.
    /// </summary>
    public bool SupportsGraphics { get; init; }

    /// <summary>
    /// Approximate width of a single character cell in pixels on the target surface.
    /// Used by graphics-aware controls to translate pixel dimensions into character cells.
    /// </summary>
    public int CharPixelWidth { get; init; } = 8;

    /// <summary>
    /// Approximate height of a single character cell in pixels on the target surface.
    /// Used by graphics-aware controls to translate pixel dimensions into character cells.
    /// </summary>
    public int CharPixelHeight { get; init; } = 16;

    /// <summary>
    /// The default capability profile: text only. Returned by <see cref="UIElement.GetCapabilities"/>
    /// when no root window is reachable or no explicit capability has been set on the window.
    /// </summary>
    public static SurfaceCapabilities TextOnly { get; } = new SurfaceCapabilities();
}
