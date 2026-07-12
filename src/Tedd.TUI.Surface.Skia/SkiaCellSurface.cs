using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using SkiaSharp;

namespace Tedd.TUI.Surface.Skia;

/// <summary>
/// Paints a flattened <see cref="VirtualBuffer"/> cell grid onto an <see cref="SKCanvas"/>.
/// This is the single shared "display driver" used by the Avalonia, WinUI and MAUI hosts:
/// each host only supplies a canvas and input mapping, so the TUI looks identical everywhere.
/// </summary>
public sealed class SkiaCellSurface : IDisposable
{
    private static readonly string[] DefaultFontCandidates =
    {
        "Cascadia Mono", "Consolas", "Menlo", "SF Mono", "DejaVu Sans Mono",
        "Ubuntu Mono", "Liberation Mono", "Courier New", "monospace"
    };

    private readonly SKTypeface _typeface;
    private readonly SKFont _font;
    private readonly SKPaint _textPaint;
    private readonly SKPaint _fillPaint;
    private readonly float _baselineOffset;
    private readonly StringBuilder _runBuilder = new();
    private static readonly ConditionalWeakTable<byte[], SKImage> _imageCache = new();

    /// <summary>Width of one character cell in pixels.</summary>
    public float CellWidth { get; }

    /// <summary>Height of one character cell in pixels.</summary>
    public float CellHeight { get; }

    public float FontSize => _font.Size;

    /// <param name="fontFamily">
    /// Optional preferred monospace font family (or comma-separated list). Falls back
    /// through common platform monospace fonts when unavailable.
    /// </param>
    /// <param name="fontSize">Font size in pixels.</param>
    public SkiaCellSurface(string? fontFamily = null, float fontSize = 16f)
    {
        _typeface = ResolveTypeface(fontFamily);
        _font = new SKFont(_typeface, fontSize) { Subpixel = true };
        _textPaint = new SKPaint { IsAntialias = true };
        _fillPaint = new SKPaint { IsAntialias = false };

        var metrics = _font.Metrics;
        CellWidth = MathF.Max(1f, _font.MeasureText("W"));
        CellHeight = MathF.Max(1f, MathF.Ceiling(metrics.Descent - metrics.Ascent));
        _baselineOffset = -metrics.Ascent;
    }

    private static SKTypeface ResolveTypeface(string? fontFamily)
    {
        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(fontFamily))
        {
            foreach (var part in fontFamily.Split(','))
                candidates.Add(part.Trim());
        }
        candidates.AddRange(DefaultFontCandidates);

        foreach (var name in candidates)
        {
            var tf = SKTypeface.FromFamilyName(name);
            // FromFamilyName falls back to the default family; only accept real matches.
            if (tf != null && tf.FamilyName.Equals(name, StringComparison.OrdinalIgnoreCase))
                return tf;
            tf?.Dispose();
        }

        return SKTypeface.CreateDefault();
    }

    /// <summary>Capability profile hosts should assign to <c>TuiWindow.Capabilities</c>.</summary>
    public SurfaceCapabilities CreateCapabilities() => new()
    {
        SupportsGraphics = true,
        CharPixelWidth = Math.Max(1, (int)MathF.Round(CellWidth)),
        CharPixelHeight = Math.Max(1, (int)MathF.Round(CellHeight))
    };

    /// <summary>Number of whole cells that fit in a pixel area.</summary>
    public (int Columns, int Rows) CellsForSize(float pixelWidth, float pixelHeight) =>
        (Math.Max(1, (int)(pixelWidth / CellWidth)), Math.Max(1, (int)(pixelHeight / CellHeight)));

    /// <summary>Pixel size of a cell grid.</summary>
    public (float Width, float Height) SizeForCells(int columns, int rows) =>
        (columns * CellWidth, rows * CellHeight);

    /// <summary>
    /// Draws <paramref name="buffer"/> (text cells and bitmap graphics) at the given pixel offset.
    /// The caller is responsible for clearing the canvas around the grid.
    /// </summary>
    public void Draw(VirtualBuffer buffer, SKCanvas canvas, float offsetX = 0f, float offsetY = 0f)
    {
        var cells = buffer.Cells;
        int cols = buffer.Width, rows = buffer.Height;

        // Pass 1: background runs.
        for (int y = 0; y < rows; y++)
        {
            int rowStart = y * cols;
            int x = 0;
            while (x < cols)
            {
                var bg = cells[rowStart + x].Background;
                int runStart = x;
                while (x < cols && cells[rowStart + x].Background.Packed == bg.Packed)
                    x++;
                if (bg.A > 0)
                {
                    _fillPaint.Color = ToSkColor(bg);
                    canvas.DrawRect(
                        offsetX + runStart * CellWidth, offsetY + y * CellHeight,
                        (x - runStart) * CellWidth, CellHeight, _fillPaint);
                }
            }
        }

        // Pass 2: text runs with identical foreground.
        for (int y = 0; y < rows; y++)
        {
            int rowStart = y * cols;
            int x = 0;
            float baseline = offsetY + y * CellHeight + _baselineOffset;
            while (x < cols)
            {
                var cell = cells[rowStart + x];
                if (cell.Character == ' ' || cell.Character == '\0' || cell.Foreground.A == 0)
                {
                    x++;
                    continue;
                }

                var fg = cell.Foreground;
                int runStart = x;
                _runBuilder.Clear();
                while (x < cols)
                {
                    var c = cells[rowStart + x];
                    if (c.Foreground.Packed != fg.Packed || c.Character == '\0')
                        break;
                    _runBuilder.Append(c.Character);
                    x++;
                }

                _textPaint.Color = ToSkColor(fg);
                // Glyphs are positioned per cell so non-monospace fallback glyphs cannot
                // drift the rest of the run out of the grid.
                DrawRun(canvas, _runBuilder, offsetX + runStart * CellWidth, baseline);
            }
        }

        // Pass 3: bitmap graphics over the grid.
        if (buffer.Graphics is { Count: > 0 })
        {
            foreach (var placement in buffer.Graphics)
            {
                var image = ResolveImage(placement);
                if (image == null)
                    continue;
                var dest = new SKRect(
                    offsetX + placement.CharX * CellWidth,
                    offsetY + placement.CharY * CellHeight,
                    offsetX + (placement.CharX + placement.CharWidth) * CellWidth,
                    offsetY + (placement.CharY + placement.CharHeight) * CellHeight);
                canvas.DrawImage(image, dest, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
            }
        }
    }

    private void DrawRun(SKCanvas canvas, StringBuilder run, float startX, float baseline)
    {
        for (int i = 0; i < run.Length; i++)
        {
            canvas.DrawText(run[i].ToString(), startX + i * CellWidth, baseline, SKTextAlign.Left, _font, _textPaint);
        }
    }

    private static SKImage? ResolveImage(GraphicPlacement placement)
    {
        if (placement.ImageData is { Length: > 0 } encoded)
        {
            if (_imageCache.TryGetValue(encoded, out var cached))
                return cached;
            var decoded = SKImage.FromEncodedData(encoded);
            if (decoded != null)
                _imageCache.Add(encoded, decoded);
            return decoded;
        }

        if (placement.Pixels is { Length: > 0 } rgba && placement.PixelWidth > 0 && placement.PixelHeight > 0)
        {
            if (_imageCache.TryGetValue(rgba, out var cached))
                return cached;
            var info = new SKImageInfo(placement.PixelWidth, placement.PixelHeight, SKColorType.Rgba8888, SKAlphaType.Unpremul);
            var image = SKImage.FromPixelCopy(info, rgba, placement.PixelWidth * 4);
            if (image != null)
                _imageCache.Add(rgba, image);
            return image;
        }

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static SKColor ToSkColor(TuiColor color) => new(color.R, color.G, color.B, color.A);

    public void Dispose()
    {
        _textPaint.Dispose();
        _fillPaint.Dispose();
        _font.Dispose();
        _typeface.Dispose();
    }
}
