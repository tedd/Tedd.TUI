using System;

namespace Tedd.TUI.Markdown;

/// <summary>
/// Classic "image to ASCII" <see cref="IAsciiArtRenderer"/>: each cell renders as a
/// single character picked from a brightness ramp, optionally colored with the nearest
/// 16-color <see cref="ConsoleColor"/> match.
/// </summary>
/// <remarks>
/// Algorithm:
/// <list type="number">
///   <item>Bilinear-resample the source image to <c>cellWidth × cellHeight</c> pixels (one pixel per cell).</item>
///   <item>For each cell, compute Rec. 601 luminance <c>Y = 0.299R + 0.587G + 0.114B</c>.</item>
///   <item>
///     Map <c>Y</c> to an index in <see cref="Ramp"/>. The character at the lowest index is used
///     for the darkest pixels, the character at the highest index for the brightest. Set
///     <see cref="Inverted"/> = <c>true</c> to flip the mapping (useful for dark-on-light terminals).
///   </item>
///   <item>
///     When <see cref="UseColor"/> is true, the foreground is the nearest <see cref="ConsoleColor"/>
///     to the pixel's RGB value; otherwise <see cref="Foreground"/> is used as a flat color.
///   </item>
///   <item>Fully transparent pixels render as a space with the fallback background.</item>
/// </list>
/// Several preset ramps are exposed as constants. The default <see cref="ColorRamp"/> omits the
/// space character so every non-transparent pixel keeps a visible glyph; that matters when colors
/// carry the picture and the background is meant to show through.
/// </remarks>
public sealed class LuminanceRampAsciiRenderer : IAsciiArtRenderer
{
    /// <summary>
    /// Short ramp without leading space — every non-transparent pixel always emits a visible
    /// character so the colored fg always shows. Best default for color rendering.
    /// </summary>
    public const string ColorRamp = ".:-=+*#%@";

    /// <summary>
    /// Classic 10-step ramp from space to '@'. Best for monochrome ASCII art on a dark background.
    /// </summary>
    public const string ShortRamp = " .:-=+*#%@";

    /// <summary>
    /// Paul Bourke's 70-character ramp ordered from light to dark visual weight. Pre-reversed
    /// here so index 0 = lightest, index n-1 = darkest, matching the "brightness → index" mapping
    /// (use <see cref="Inverted"/> = true if you prefer the opposite).
    /// </summary>
    public const string BourkeRamp =
        " .'`^\",:;Il!i><~+_-?][}{1)(|\\/tfjrxnuvczXYUJCLQ0OZmwqpdbkhao*#MW&8%B@$";

    /// <summary>Process-wide singleton with default settings (color enabled, <see cref="ColorRamp"/>).</summary>
    public static readonly LuminanceRampAsciiRenderer Instance = new LuminanceRampAsciiRenderer();

    /// <summary>
    /// Ramp ordered from darkest visual weight (index 0) to brightest (last index).
    /// Defaults to <see cref="ColorRamp"/>.
    /// </summary>
    public string Ramp { get; init; } = ColorRamp;

    /// <summary>When true the luminance-to-ramp mapping is flipped (bright pixels → first char).</summary>
    public bool Inverted { get; init; }

    /// <summary>When true the foreground color tracks the pixel; when false <see cref="Foreground"/> is used.</summary>
    public bool UseColor { get; init; } = true;

    /// <summary>Foreground color used when <see cref="UseColor"/> is false.</summary>
    public TuiColor Foreground { get; init; } = TuiColor.Gray;

    /// <summary>Alpha threshold below which the pixel renders as a transparent space (0–255). Defaults to 16.</summary>
    public int AlphaThreshold { get; init; } = 16;

    /// <summary>Default-constructed instance with <see cref="ColorRamp"/> and color enabled.</summary>
    public LuminanceRampAsciiRenderer() { }

    /// <summary>Construct with an explicit ramp string. The ramp must contain at least one character.</summary>
    public LuminanceRampAsciiRenderer(string ramp, bool inverted = false, bool useColor = true)
    {
        if (string.IsNullOrEmpty(ramp))
            throw new ArgumentException("Ramp must contain at least one character.", nameof(ramp));
        Ramp = ramp;
        Inverted = inverted;
        UseColor = useColor;
    }

    public Cell[] Render(RgbaImage image, int cellWidth, int cellHeight, TuiColor fallbackBackground)
    {
        if (cellWidth <= 0 || cellHeight <= 0)
            return Array.Empty<Cell>();

        string ramp = Ramp;
        if (string.IsNullOrEmpty(ramp))
            ramp = ColorRamp;

        var cells = new Cell[cellWidth * cellHeight];

        if (image.Pixels == null || image.Width <= 0 || image.Height <= 0)
        {
            for (int i = 0; i < cells.Length; i++)
                cells[i] = new Cell(' ', Foreground, fallbackBackground);
            return cells;
        }

        var resampled = ImageResampler.Bilinear(image, cellWidth, cellHeight);

        int rampLen = ramp.Length;
        int alphaCut = AlphaThreshold;

        for (int y = 0; y < cellHeight; y++)
        {
            for (int x = 0; x < cellWidth; x++)
            {
                int idx = (y * cellWidth + x) * 4;
                byte r = resampled[idx];
                byte g = resampled[idx + 1];
                byte b = resampled[idx + 2];
                byte a = resampled[idx + 3];

                if (a < alphaCut)
                {
                    cells[y * cellWidth + x] = new Cell(' ', Foreground, fallbackBackground);
                    continue;
                }

                // Rec. 601 luminance — perceptually closer to how dim a pixel "looks".
                float luminance = 0.299f * r + 0.587f * g + 0.114f * b;
                int rampIdx = (int)(luminance / 255f * (rampLen - 1) + 0.5f);
                if (rampIdx < 0) rampIdx = 0;
                else if (rampIdx >= rampLen) rampIdx = rampLen - 1;
                if (Inverted) rampIdx = rampLen - 1 - rampIdx;

                char glyph = ramp[rampIdx];
                TuiColor fg = UseColor ? new TuiColor(r, g, b) : Foreground;

                cells[y * cellWidth + x] = new Cell(glyph, fg, fallbackBackground);
            }
        }

        return cells;
    }
}
