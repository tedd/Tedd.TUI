using System;

namespace Tedd.TUI.Markdown;

/// <summary>
/// How an <see cref="Image"/> should choose its rendering path.
/// </summary>
public enum ImageRenderMode
{
    /// <summary>
    /// Use bitmap rendering when the surface supports graphics, otherwise fall back to ASCII.
    /// </summary>
    Auto,
    /// <summary>Always render as character-cell art via the configured <see cref="IAsciiArtRenderer"/>.</summary>
    Ascii,
    /// <summary>Always emit a <see cref="GraphicPlacement"/>. Falls back to ASCII if the surface is text-only.</summary>
    Graphic
}

/// <summary>
/// Markdown inline image control. Behaves like an HTML <c>&lt;img&gt;</c>: an inline element
/// whose box can be larger than one row. <see cref="Paragraph"/> already grows the current
/// row to fit the tallest child so multi-cell images flow naturally with surrounding text.
/// </summary>
/// <remarks>
/// Rendering paths:
/// <list type="bullet">
///   <item>Graphic — when the hosting surface reports <see cref="SurfaceCapabilities.SupportsGraphics"/>
///   and the current frame has a <see cref="VirtualBuffer.Graphics"/> list, the image emits a
///   <see cref="GraphicPlacement"/>. Cells are filled with spaces so the surface can draw the
///   real bitmap on top.</item>
///   <item>ASCII — otherwise, the configured <see cref="IAsciiArtRenderer"/> rasterizes the
///   decoded image into cells.</item>
///   <item>Fallback — when no <see cref="DefaultDecoder"/> / <see cref="DefaultResolver"/> is
///   configured, the image renders as <c>[AltText]</c> (one row), preserving the legacy behaviour.</item>
/// </list>
/// Decoder, resolver, and ASCII renderer are pluggable so the core Tedd.TUI assembly stays
/// free of image-codec dependencies. Set <see cref="DefaultDecoder"/> / <see cref="DefaultResolver"/>
/// at startup (e.g. via <c>Tedd.TUI.Imaging.TuiImaging.RegisterDefaults</c>).
/// </remarks>
public class Image : UIElement
{
    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.Register("Source", typeof(string), typeof(Image), string.Empty);

    public string Source
    {
        get => (string)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public static readonly DependencyProperty AltTextProperty =
        DependencyProperty.Register("AltText", typeof(string), typeof(Image), string.Empty);

    public string AltText
    {
        get => (string)GetValue(AltTextProperty);
        set => SetValue(AltTextProperty, value);
    }

    public new static readonly DependencyProperty ForegroundProperty = UIElement.ForegroundProperty;

    /// <summary>Maximum width in cells, or 0 for unconstrained.</summary>
    public int MaxCellWidth { get; set; } = 0;

    /// <summary>Maximum height in cells, or 0 for unconstrained.</summary>
    public int MaxCellHeight { get; set; } = 0;

    /// <summary>Which rendering path the image should pick. Defaults to <see cref="ImageRenderMode.Auto"/>.</summary>
    public ImageRenderMode RenderMode { get; set; } = ImageRenderMode.Auto;

    /// <summary>
    /// Optional ASCII renderer override. When null, falls back (in order) to <see cref="DefaultAsciiRenderer"/>
    /// and finally <see cref="HalfBlockAsciiRenderer.Instance"/>.
    /// </summary>
    public IAsciiArtRenderer? AsciiRenderer { get; set; }

    /// <summary>
    /// Base directory used to resolve relative <see cref="Source"/> values. Typically set by the
    /// hosting <see cref="MarkdownView"/> to the markdown document's directory.
    /// </summary>
    public string? BaseDirectory { get; set; }

    /// <summary>Process-wide default decoder. Without one set, the image falls back to <c>[AltText]</c>.</summary>
    public static IImageDecoder? DefaultDecoder { get; set; }

    /// <summary>Process-wide default resolver. Without one set, the image falls back to <c>[AltText]</c>.</summary>
    public static IImageResolver? DefaultResolver { get; set; }

    /// <summary>Process-wide default ASCII renderer. Defaults to <see cref="HalfBlockAsciiRenderer.Instance"/>.</summary>
    public static IAsciiArtRenderer? DefaultAsciiRenderer { get; set; } = HalfBlockAsciiRenderer.Instance;

    /// <summary>Process-wide fallback base directory for relative sources.</summary>
    public static string? DefaultBaseDirectory { get; set; }

    // --- Per-instance caches keyed by Source ---
    private string? _decodedSource;
    private RgbaImage _decodedImage;
    private bool _decodedValid;
    private byte[]? _resolvedBytes;
    private string? _resolvedMediaType;

    private string? _asciiKey;
    private Cell[]? _asciiCells;
    private int _asciiCellWidth;
    private int _asciiCellHeight;

    private bool TryEnsureResolvedAndDecoded(out RgbaImage image)
    {
        string source = Source ?? string.Empty;
        if (_decodedValid && _decodedSource == source)
        {
            image = _decodedImage;
            return true;
        }

        // Reset cache when the source changes.
        if (_decodedSource != source)
        {
            _decodedSource = source;
            _decodedValid = false;
            _decodedImage = default;
            _resolvedBytes = null;
            _resolvedMediaType = null;
            _asciiCells = null;
            _asciiKey = null;
        }

        var resolver = DefaultResolver;
        var decoder = DefaultDecoder;
        if (resolver == null || decoder == null || string.IsNullOrEmpty(source))
        {
            image = default;
            return false;
        }

        string? baseDir = BaseDirectory ?? DefaultBaseDirectory;
        if (!resolver.TryResolve(source, baseDir, out var bytes, out var mediaType))
        {
            image = default;
            return false;
        }

        _resolvedBytes = bytes;
        _resolvedMediaType = mediaType;

        if (!decoder.TryDecode(bytes, out var decoded))
        {
            image = default;
            return false;
        }

        _decodedImage = decoded;
        _decodedValid = true;
        image = decoded;
        return true;
    }

    private Size ComputeCellSize(int pixelWidth, int pixelHeight, Size availableSize)
    {
        var caps = GetCapabilities();
        int cpw = Math.Max(1, caps.CharPixelWidth);
        int cph = Math.Max(1, caps.CharPixelHeight);

        // Round up so we don't undersample edges.
        int cellsW = (pixelWidth + cpw - 1) / cpw;
        int cellsH = (pixelHeight + cph - 1) / cph;

        // Respect explicit caps on the control.
        if (MaxCellWidth > 0 && cellsW > MaxCellWidth)
        {
            cellsH = (int)Math.Round(cellsH * (double)MaxCellWidth / cellsW);
            cellsW = MaxCellWidth;
        }
        if (MaxCellHeight > 0 && cellsH > MaxCellHeight)
        {
            cellsW = (int)Math.Round(cellsW * (double)MaxCellHeight / cellsH);
            cellsH = MaxCellHeight;
        }

        // Clamp to layout-available size (but only when the constraint is sane).
        if (availableSize.Width > 0 && availableSize.Width < int.MaxValue && cellsW > availableSize.Width)
        {
            cellsH = (int)Math.Round(cellsH * (double)availableSize.Width / cellsW);
            cellsW = availableSize.Width;
        }

        if (cellsW < 1) cellsW = 1;
        if (cellsH < 1) cellsH = 1;
        return new Size(cellsW, cellsH);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (TryEnsureResolvedAndDecoded(out var img))
        {
            return ComputeCellSize(img.Width, img.Height, availableSize);
        }

        string text = BuildAltTextLabel();
        return new Size(text.Length, 1);
    }

    public override void Render(VirtualBuffer buffer, int offsetX, int offsetY)
    {
        int x = RenderSize.X + offsetX;
        int y = RenderSize.Y + offsetY;
        int w = RenderSize.Width;
        int h = RenderSize.Height;

        if (w <= 0 || h <= 0) return;

        // No decoder configured / source missing → legacy alt-text.
        if (!TryEnsureResolvedAndDecoded(out var img))
        {
            RenderAltText(buffer, x, y, w);
            return;
        }

        var caps = GetCapabilities();
        bool wantGraphic = RenderMode != ImageRenderMode.Ascii
                           && caps.SupportsGraphics
                           && buffer.Graphics != null
                           && (_resolvedBytes != null || (img.Pixels != null && img.Width > 0 && img.Height > 0));

        if (wantGraphic)
        {
            RenderAsGraphic(buffer, img, x, y, w, h);
            return;
        }

        RenderAsAscii(buffer, img, x, y, w, h);
    }

    private void RenderAltText(VirtualBuffer buffer, int x, int y, int width)
    {
        string text = BuildAltTextLabel();
        var bg = Background ?? buffer.GetPixel(x, y).Background;
        int max = Math.Min(text.Length, width);
        for (int i = 0; i < max; i++)
        {
            buffer.SetPixel(x + i, y, text[i], Foreground, bg);
        }
    }

    /// <summary>
    /// Builds the textual fallback shown when the image cannot be rendered. Prefers the
    /// supplied <see cref="AltText"/>, then a filename derived from <see cref="Source"/>
    /// (so HTTP URLs without alt text don't all collapse to "[Image]"), and finally
    /// the literal "[Image]" placeholder.
    /// </summary>
    private string BuildAltTextLabel()
    {
        if (!string.IsNullOrEmpty(AltText)) return $"[{AltText}]";

        string? source = Source;
        if (!string.IsNullOrEmpty(source))
        {
            int q = source.IndexOfAny(['?', '#']);
            string trimmed = q >= 0 ? source.Substring(0, q) : source;
            int slash = trimmed.LastIndexOfAny(['/', '\\']);
            string name = slash >= 0 ? trimmed.Substring(slash + 1) : trimmed;
            if (!string.IsNullOrEmpty(name)) return $"[{name}]";
        }

        return "[Image]";
    }

    private void RenderAsGraphic(VirtualBuffer buffer, RgbaImage img, int x, int y, int width, int height)
    {
        // Punch transparent / background-coloured cells under the placement so any text
        // that lives behind the image area gets cleared in surfaces that only honor the
        // text grid. The actual bitmap is composited on top by the surface renderer.
        var bg = Background ?? buffer.GetPixel(x, y).Background;
        for (int row = 0; row < height; row++)
        {
            for (int col = 0; col < width; col++)
            {
                buffer.SetPixel(x + col, y + row, ' ', Foreground, bg);
            }
        }

        // Carry both representations on the placement: surfaces that only need an
        // <img src="data:..."> (HTML DOM, Kitty PNG-mode, iTerm2) consume ImageData;
        // surfaces that need raw pixels (Sixel quantizer, Canvas pixel blitter) consume
        // the decoded RGBA buffer without re-running the codec.
        buffer.Graphics!.Add(new GraphicPlacement
        {
            CharX = x,
            CharY = y,
            CharWidth = width,
            CharHeight = height,
            ImageData = _resolvedBytes,
            MediaType = _resolvedMediaType,
            Pixels = img.Pixels,
            PixelWidth = img.Width,
            PixelHeight = img.Height,
            Source = Source
        });
    }

    private void RenderAsAscii(VirtualBuffer buffer, RgbaImage img, int x, int y, int width, int height)
    {
        var renderer = AsciiRenderer ?? DefaultAsciiRenderer ?? HalfBlockAsciiRenderer.Instance;
        var bg = Background ?? buffer.GetPixel(x, y).Background;

        string key = $"{Source}|{width}x{height}|{renderer.GetType().FullName}|{bg.Packed:X8}";
        if (_asciiCells == null
            || _asciiKey != key
            || _asciiCellWidth != width
            || _asciiCellHeight != height)
        {
            _asciiCells = renderer.Render(img, width, height, bg);
            _asciiCellWidth = width;
            _asciiCellHeight = height;
            _asciiKey = key;
        }

        var cells = _asciiCells;
        for (int row = 0; row < height; row++)
        {
            for (int col = 0; col < width; col++)
            {
                var c = cells[row * width + col];
                buffer.SetPixel(x + col, y + row, c.Character, c.Foreground, c.Background);
            }
        }
    }
}
