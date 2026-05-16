using System;

namespace Tedd.TUI.Markdown;

/// <summary>
/// Default <see cref="IAsciiArtRenderer"/>. Renders the image as a grid of
/// Unicode upper-half blocks (U+2580 "▀"). Each cell encodes two stacked
/// pixels: the upper pixel as the foreground color, the lower pixel as the
/// background. This effectively doubles the vertical resolution at the cost
/// of using both color channels.
/// </summary>
/// <remarks>
/// Algorithm:
/// 1. Bilinear-resample the source image to (cellWidth, cellHeight * 2) pixels.
/// 2. For each cell (x, y), pick the top sample at (x, 2y) and the bottom at (x, 2y+1).
/// 3. Emit the sample's full 24-bit RGB as a <see cref="TuiColor"/>; the active renderer
///    decides whether to send truecolor SGR or quantize to the 16-color palette.
/// 4. Fully-transparent samples (alpha &lt; 16) fall back to the supplied background.
/// </remarks>
public sealed class HalfBlockAsciiRenderer : IAsciiArtRenderer
{
    /// <summary>Process-wide singleton used as the default renderer.</summary>
    public static readonly HalfBlockAsciiRenderer Instance = new HalfBlockAsciiRenderer();

    private const char UpperHalfBlock = '\u2580';

    public Cell[] Render(RgbaImage image, int cellWidth, int cellHeight, TuiColor fallbackBackground)
    {
        if (cellWidth <= 0 || cellHeight <= 0)
            return Array.Empty<Cell>();

        if (image.Pixels == null || image.Width <= 0 || image.Height <= 0)
        {
            return FilledWith(cellWidth, cellHeight, fallbackBackground);
        }

        int targetW = cellWidth;
        int targetH = cellHeight * 2;

        // Resample source into a (targetW, targetH) RGBA8 buffer using bilinear sampling.
        var resampled = ImageResampler.Bilinear(image, targetW, targetH);

        var cells = new Cell[cellWidth * cellHeight];
        for (int y = 0; y < cellHeight; y++)
        {
            int rowTop = (y * 2) * targetW;
            int rowBot = (y * 2 + 1) * targetW;
            for (int x = 0; x < cellWidth; x++)
            {
                int topIdx = (rowTop + x) * 4;
                int botIdx = (rowBot + x) * 4;

                byte tr = resampled[topIdx];
                byte tg = resampled[topIdx + 1];
                byte tb = resampled[topIdx + 2];
                byte ta = resampled[topIdx + 3];

                byte br = resampled[botIdx];
                byte bg = resampled[botIdx + 1];
                byte bb = resampled[botIdx + 2];
                byte ba = resampled[botIdx + 3];

                TuiColor topColor = ta < 16 ? fallbackBackground : new TuiColor(tr, tg, tb);
                TuiColor botColor = ba < 16 ? fallbackBackground : new TuiColor(br, bg, bb);

                cells[y * cellWidth + x] = new Cell(UpperHalfBlock, topColor, botColor);
            }
        }
        return cells;
    }

    private static Cell[] FilledWith(int cellWidth, int cellHeight, TuiColor bg)
    {
        var cells = new Cell[cellWidth * cellHeight];
        for (int i = 0; i < cells.Length; i++)
            cells[i] = new Cell(' ', TuiColor.White, bg);
        return cells;
    }
}
