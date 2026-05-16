namespace Tedd.TUI.Markdown;

/// <summary>
/// A decoded raster image in 8-bit RGBA format. Pixels are row-major, top to bottom,
/// left to right. Each pixel occupies 4 consecutive bytes: R, G, B, A.
/// </summary>
public struct RgbaImage
{
    /// <summary>Image width in pixels.</summary>
    public int Width;

    /// <summary>Image height in pixels.</summary>
    public int Height;

    /// <summary>
    /// Row-major RGBA8 pixel data. Length must equal <c>Width * Height * 4</c>.
    /// Index of pixel (x, y) red channel = <c>(y * Width + x) * 4</c>.
    /// </summary>
    public byte[] Pixels;
}
