using System;

namespace Tedd.TUI.Markdown;

/// <summary>
/// Converts a decoded <see cref="RgbaImage"/> into a grid of <see cref="Cell"/>s
/// fit for blitting onto a <see cref="VirtualBuffer"/>. This is the pluggable
/// extension point that lets users swap the default half-block 16-color renderer
/// for alternatives (greyscale ramp, dithered, sixel, Kitty graphics protocol, etc.).
/// </summary>
public interface IAsciiArtRenderer
{
    /// <summary>
    /// Produces a row-major grid of <see cref="Cell"/>s of exactly
    /// <paramref name="cellWidth"/> * <paramref name="cellHeight"/> entries.
    /// Index for cell at column x, row y is <c>y * cellWidth + x</c>.
    /// </summary>
    /// <param name="image">Source image to render.</param>
    /// <param name="cellWidth">Target width in character cells.</param>
    /// <param name="cellHeight">Target height in character cells.</param>
    /// <param name="fallbackBackground">Background color used for fully transparent pixels.</param>
    Cell[] Render(RgbaImage image, int cellWidth, int cellHeight, TuiColor fallbackBackground);
}
