using System;
using System.Text;

namespace Tedd.TUI.Archive.Media;

/// <summary>
/// Shared DEC Sixel encoder used by both <c>Tedd.TUI.Platform.WindowsTerminal</c> and
/// <c>Tedd.TUI.Platform.LinuxTerminal</c>. Consumes the decoded RGBA pixel buffer
/// carried by a <see cref="GraphicPlacement"/> and emits a complete Sixel envelope
/// (<c>ESC P 0;0;0 q "1;1;W;H ... ESC \</c>) using a fixed 6×6×6 web-safe palette plus
/// a 24-step grayscale ramp. Designed to be allocation-light, dependency-free, and
/// deterministic enough that tests can assert on its output verbatim.
/// </summary>
/// <remarks>
/// <para>The Sixel transmission format encodes pixels in <c>vertical bands of six rows</c>:
/// each <c>?..~</c> byte represents six vertical pixels of a single column, with bit 0 =
/// top row and bit 5 = bottom row. For each band we walk the palette once, emit a
/// <c>#&lt;color&gt;</c> selector + the column data, return to the start of the band with
/// <c>$</c>, and repeat for the next color. A <c>-</c> separator moves to the next
/// band. We use run-length compression (<c>!&lt;count&gt;&lt;byte&gt;</c>) for runs of three or
/// more identical bytes — that's the spec's recommended threshold and keeps the output
/// well under the 4096-byte chunk size most terminals limit a Sixel payload to.</para>
/// <para>The palette is a fixed 6×6×6 = 216-color cube (matching the xterm 256-color
/// indices 16-231) plus a 24-step gray ramp (matching indices 232-255). This keeps
/// quantization branch-free and lets the encoder skip a separate median-cut pass; the
/// trade-off is slightly banded output for photographic images, which is acceptable
/// for the kind of icons / diagrams / screenshots terminal apps usually display.</para>
/// </remarks>
public static class SixelEncoderCore
{
    /// <summary>
    /// Encodes <paramref name="placement"/> into the Sixel escape envelope. Prefers the
    /// decoded <see cref="GraphicPlacement.Pixels"/> buffer; falls back to a tiny
    /// transparent placeholder when no pixels are attached (so the caller can still
    /// validate the round-trip without a decoder configured).
    /// </summary>
    public static string Encode(GraphicPlacement placement)
    {
        if (placement.Pixels != null &&
            placement.PixelWidth > 0 &&
            placement.PixelHeight > 0 &&
            placement.Pixels.Length >= placement.PixelWidth * placement.PixelHeight * 4)
        {
            return EncodePixels(placement.Pixels, placement.PixelWidth, placement.PixelHeight);
        }

        return EncodePlaceholder(Math.Max(1, placement.CharWidth) * 10, Math.Max(1, placement.CharHeight) * 20);
    }

    /// <summary>
    /// Encodes a raw RGBA buffer (row-major, 4 bytes per pixel) into a full DEC Sixel
    /// payload. Exposed so callers that don't have a <see cref="GraphicPlacement"/>
    /// handy (e.g. unit tests) can drive the encoder directly.
    /// </summary>
    public static string EncodePixels(byte[] pixels, int width, int height)
    {
        if (pixels == null) throw new ArgumentNullException(nameof(pixels));
        if (width <= 0 || height <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (pixels.Length < width * height * 4)
            throw new ArgumentException("Pixel buffer too small for the declared dimensions.", nameof(pixels));

        // Quantize every pixel to a palette index once so the per-band loop below is
        // a tight integer scan. Index 0xFF marks a fully transparent pixel.
        const byte TransparentIndex = 0xFF;
        var quantized = new byte[width * height];
        for (int i = 0, p = 0; p < quantized.Length; i += 4, p++)
        {
            byte r = pixels[i];
            byte g = pixels[i + 1];
            byte b = pixels[i + 2];
            byte a = pixels[i + 3];
            quantized[p] = a < 16 ? TransparentIndex : Quantize(r, g, b);
        }

        var sb = new StringBuilder(width * height / 6 + 256);
        sb.Append("\x1bP0;1;0q"); // P2=1 → background pixels remain transparent.

        // Raster attributes: pan=1, pad=1 (square pixels), actual w / h.
        sb.Append('"').Append(1).Append(';').Append(1).Append(';').Append(width).Append(';').Append(height);

        // Palette definitions. We emit only the colors actually used so trivially-small
        // images stay compact and the terminal's palette table doesn't churn.
        var used = new bool[PaletteSize];
        for (int p = 0; p < quantized.Length; p++)
        {
            byte idx = quantized[p];
            if (idx != TransparentIndex) used[idx] = true;
        }
        for (int i = 0; i < PaletteSize; i++)
        {
            if (!used[i]) continue;
            (byte r, byte g, byte b) = PaletteRgb(i);
            // Sixel color values are 0..100 (percent), not 0..255.
            int sr = (r * 100 + 127) / 255;
            int sg = (g * 100 + 127) / 255;
            int sb_ = (b * 100 + 127) / 255;
            sb.Append('#').Append(i).Append(";2;").Append(sr).Append(';').Append(sg).Append(';').Append(sb_);
        }

        // Emit pixel data, six rows at a time.
        var bandBits = new byte[width]; // accumulates one column's 6-row bitmask for the active color
        for (int bandStart = 0; bandStart < height; bandStart += 6)
        {
            int bandHeight = Math.Min(6, height - bandStart);
            bool wroteAnything = false;

            // For each palette color present in this band, emit its column bitmasks.
            // We re-scan the band per color so we can use $ (carriage return) to chain
            // color planes without leaving a leading cursor offset.
            for (int color = 0; color < PaletteSize; color++)
            {
                if (!used[color]) continue;

                bool colorTouchesBand = false;
                Array.Clear(bandBits, 0, bandBits.Length);
                for (int row = 0; row < bandHeight; row++)
                {
                    int srcRow = bandStart + row;
                    int srcOffset = srcRow * width;
                    byte mask = (byte)(1 << row);
                    for (int x = 0; x < width; x++)
                    {
                        if (quantized[srcOffset + x] == color)
                        {
                            bandBits[x] |= mask;
                            colorTouchesBand = true;
                        }
                    }
                }

                if (!colorTouchesBand) continue;

                sb.Append('#').Append(color);
                AppendBandRunLength(sb, bandBits, width);

                // Carriage-return so the next color writes from the same band origin.
                sb.Append('$');
                wroteAnything = true;
            }

            // Advance to the next band. Even empty bands need a separator so cursor stays aligned.
            if (bandStart + 6 < height)
            {
                if (!wroteAnything)
                {
                    // No content emitted; still consume a newline.
                }
                sb.Append('-');
            }
        }

        sb.Append("\x1b\\");
        return sb.ToString();
    }

    /// <summary>
    /// Writes one band of column bitmasks to <paramref name="sb"/> with run-length
    /// compression. Each byte is offset by <c>0x3F</c> ('?') so the result is always
    /// printable. Runs of 3+ identical bytes collapse to <c>!&lt;n&gt;&lt;byte&gt;</c>.
    /// </summary>
    private static void AppendBandRunLength(StringBuilder sb, byte[] bandBits, int width)
    {
        int i = 0;
        while (i < width)
        {
            byte cur = bandBits[i];
            int runEnd = i + 1;
            while (runEnd < width && bandBits[runEnd] == cur) runEnd++;
            int runLength = runEnd - i;
            char glyph = (char)(0x3F + cur);

            if (runLength >= 3)
            {
                sb.Append('!').Append(runLength).Append(glyph);
            }
            else
            {
                for (int k = 0; k < runLength; k++) sb.Append(glyph);
            }
            i = runEnd;
        }
    }

    // --- 216-color web-safe cube + 24-step gray ramp = 240 palette entries. ---
    private const int CubeLevels = 6;
    private const int CubeSize = CubeLevels * CubeLevels * CubeLevels; // 216
    private const int GraySteps = 24;
    private const int PaletteSize = CubeSize + GraySteps; // 240

    private static readonly byte[] CubeLevelValues = new byte[CubeLevels]
    {
        0, 51, 102, 153, 204, 255
    };

    /// <summary>
    /// Maps <c>(r,g,b)</c> ∈ [0,255] to a palette index. The cube is uniform so we can
    /// quantize each channel independently with one multiply per channel.
    /// </summary>
    private static byte Quantize(byte r, byte g, byte b)
    {
        // For nearly-gray inputs, prefer the gray ramp; it gives 24 steps versus the
        // cube's 6 along the diagonal, which avoids the obvious banding on photos.
        int maxC = Math.Max(r, Math.Max(g, b));
        int minC = Math.Min(r, Math.Min(g, b));
        if (maxC - minC <= 8)
        {
            int gray = (r + g + b) / 3;
            int step = (gray * (GraySteps - 1) + 127) / 255;
            return (byte)(CubeSize + step);
        }

        int ir = QuantizeChannel(r);
        int ig = QuantizeChannel(g);
        int ib = QuantizeChannel(b);
        return (byte)(ir * CubeLevels * CubeLevels + ig * CubeLevels + ib);
    }

    private static int QuantizeChannel(byte v)
    {
        // Each level is 51 wide, biased so 0..25 → 0, 26..76 → 1, ...
        int idx = (v + 25) / 51;
        if (idx > CubeLevels - 1) idx = CubeLevels - 1;
        return idx;
    }

    private static (byte r, byte g, byte b) PaletteRgb(int index)
    {
        if (index < CubeSize)
        {
            int ib = index % CubeLevels;
            int ig = (index / CubeLevels) % CubeLevels;
            int ir = index / (CubeLevels * CubeLevels);
            return (CubeLevelValues[ir], CubeLevelValues[ig], CubeLevelValues[ib]);
        }
        int step = index - CubeSize;
        byte v = (byte)((step * 255 + (GraySteps - 1) / 2) / (GraySteps - 1));
        return (v, v, v);
    }

    /// <summary>
    /// Fallback used when no decoded pixels are available: emits a black filled
    /// rectangle of the requested pixel size so the round-trip can still be validated.
    /// </summary>
    private static string EncodePlaceholder(int pxW, int pxH)
    {
        var sb = new StringBuilder(64 + pxW);
        sb.Append("\x1bP0;0;0q");
        sb.Append('"').Append(1).Append(';').Append(1).Append(';').Append(pxW).Append(';').Append(pxH);
        sb.Append("#0;2;0;0;0");
        sb.Append('#').Append(0);
        for (int i = 0; i < pxW; i++) sb.Append('?');
        sb.Append("\x1b\\");
        return sb.ToString();
    }
}
