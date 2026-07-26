namespace Tedd.TUI.Media;

/// <summary>
/// A request to render a bitmap on top of the character grid at a specific cell rectangle.
/// Surfaces that report <see cref="SurfaceCapabilities.SupportsGraphics"/> = true allocate a
/// <see cref="VirtualBuffer.Graphics"/> list each frame; graphics-aware controls append their
/// placements during <c>Render</c>, and the surface renderer composites them after the text
/// cells are drawn.
/// </summary>
/// <remarks>
/// <para>A placement carries the same logical bitmap in two complementary forms so each
/// surface can consume the cheapest representation:
/// <list type="bullet">
///   <item><see cref="ImageData"/> is the original encoded payload (PNG, JPEG, …). HTML
///   surfaces and the iTerm2 / Kitty terminal protocols embed it verbatim via base64.</item>
///   <item><see cref="Pixels"/> + <see cref="PixelWidth"/> + <see cref="PixelHeight"/>
///   carry the decoded 8-bit RGBA buffer. Surfaces that need raw pixels — Sixel
///   (which quantizes to its own palette) and the HTML canvas overlay (when scaling
///   needs precise control) — consume this form to skip an extra decode round-trip.</item>
/// </list>
/// Both forms are optional; image-producing controls populate whichever ones they can.
/// Surfaces gracefully fall back to whatever is supplied.</para>
/// </remarks>
public struct GraphicPlacement
{
    /// <summary>Cell column of the top-left corner.</summary>
    public int CharX;
    /// <summary>Cell row of the top-left corner.</summary>
    public int CharY;
    /// <summary>Width in character cells.</summary>
    public int CharWidth;
    /// <summary>Height in character cells.</summary>
    public int CharHeight;

    /// <summary>Raw encoded image bytes (e.g. PNG, JPEG). May be null when only <see cref="Source"/> is meaningful.</summary>
    public byte[]? ImageData;

    /// <summary>MIME type for <see cref="ImageData"/>, e.g. "image/png".</summary>
    public string? MediaType;

    /// <summary>
    /// Optional decoded RGBA pixel buffer (row-major, 8 bits per channel, length =
    /// <see cref="PixelWidth"/> × <see cref="PixelHeight"/> × 4). Populated by image
    /// controls when a decoder is available so surfaces that need raw pixels (notably
    /// Sixel and any future GPU-blitter) don't have to re-run the codec.
    /// </summary>
    public byte[]? Pixels;

    /// <summary>Pixel width of <see cref="Pixels"/>. Ignored when <see cref="Pixels"/> is null.</summary>
    public int PixelWidth;

    /// <summary>Pixel height of <see cref="Pixels"/>. Ignored when <see cref="Pixels"/> is null.</summary>
    public int PixelHeight;

    /// <summary>
    /// Original source identifier (URL or file path). Optional. When set, the surface renderer
    /// may use it as a stable cache key or directly as an &lt;img src=...&gt; for HTTP URLs.
    /// </summary>
    public string? Source;

    /// <summary>
    /// True when only part of the placement is visible and the surface must crop it to
    /// <see cref="ClipCharX"/>..<see cref="ClipCharHeight"/>. False means draw it whole.
    /// </summary>
    /// <remarks>
    /// <para>Set by <see cref="VirtualBuffer.AddGraphic"/> when the active clip cuts the
    /// placement — a scrolled image partly outside its viewport being the usual case. The
    /// <see cref="CharX"/>/<see cref="CharY"/>/<see cref="CharWidth"/>/<see cref="CharHeight"/>
    /// rectangle still describes the <em>whole</em> image, so the bitmap keeps its size and
    /// aspect ratio and the clip only decides how much of it shows.</para>
    /// <para>Leaving these fields alone therefore reproduces the historical behaviour exactly:
    /// a surface that ignores them draws the full placement as it always did.</para>
    /// </remarks>
    public bool IsClipped;

    /// <summary>Left edge of the visible region, in cells. Meaningful only when <see cref="IsClipped"/>.</summary>
    public int ClipCharX;
    /// <summary>Top edge of the visible region, in cells. Meaningful only when <see cref="IsClipped"/>.</summary>
    public int ClipCharY;
    /// <summary>Width of the visible region, in cells. Meaningful only when <see cref="IsClipped"/>.</summary>
    public int ClipCharWidth;
    /// <summary>Height of the visible region, in cells. Meaningful only when <see cref="IsClipped"/>.</summary>
    public int ClipCharHeight;
}
