namespace Tedd.TUI;

/// <summary>
/// A request to render a bitmap on top of the character grid at a specific cell rectangle.
/// Surfaces that report <see cref="SurfaceCapabilities.SupportsGraphics"/> = true allocate a
/// <see cref="VirtualBuffer.Graphics"/> list each frame; graphics-aware controls append their
/// placements during <c>Render</c>, and the surface renderer composites them after the text
/// cells are drawn.
/// </summary>
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
    /// Original source identifier (URL or file path). Optional. When set, the surface renderer
    /// may use it as a stable cache key or directly as an &lt;img src=...&gt; for HTTP URLs.
    /// </summary>
    public string? Source;
}
