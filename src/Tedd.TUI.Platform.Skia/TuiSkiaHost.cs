using System;
using System.IO;
using SkiaSharp;
using Tedd.TUI.Surface.Skia;

namespace Tedd.TUI.Platform.Skia;

/// <summary>
/// Hosts a Tedd.TUI window on a bare SkiaSharp canvas — no GUI framework required.
/// The TUI renders into a <see cref="VirtualBuffer"/> cell grid painted by the shared
/// <see cref="SkiaCellSurface"/>, so the output is identical to every other Tedd.TUI host.
/// </summary>
/// <remarks>
/// <para>Use this host to embed a TUI wherever you already own an <see cref="SKCanvas"/>
/// (game engines, OpenTK/Silk.NET windows, custom compositors) or to render headless
/// screenshots via <see cref="RenderToImage"/> / <see cref="RenderToPng(string, int, int)"/>.</para>
/// <para>Content is provided via <see cref="SetContent"/>: an existing <c>TuiWindow</c>,
/// inline XAML markup or a path to a XAML file, in that precedence order. Event handlers
/// in markup bind against the supplied controller object.</para>
/// <para>Repaints are signalled through <see cref="RenderRequested"/> (may fire on any
/// thread); the embedder schedules a redraw and calls <see cref="Render"/> on its own
/// cadence. Input is forwarded with <see cref="MouseDown"/>/<see cref="MouseUp"/>/<see cref="MouseMove"/>
/// in pixel coordinates and <see cref="ProcessKey"/>/<see cref="SendText"/> for keys.</para>
/// </remarks>
public sealed class TuiSkiaHost : IDisposable
{
    private readonly TuiSurfaceController _controller = new();
    private SkiaCellSurface? _surface;
    private string? _fontFamily;
    private float _fontSize;

    /// <param name="fontFamily">
    /// Optional preferred monospace font family (comma-separated fallback list allowed);
    /// falls through common platform monospace fonts when unavailable.
    /// </param>
    /// <param name="fontSize">Cell font size in pixels (default 16).</param>
    public TuiSkiaHost(string? fontFamily = null, float fontSize = 16f)
    {
        _fontFamily = fontFamily;
        _fontSize = fontSize;
        _controller.RenderRequested += () => RenderRequested?.Invoke();
    }

    /// <summary>
    /// Raised (possibly from a non-UI thread) whenever the hosted TUI needs a repaint.
    /// Embedders schedule a redraw and call <see cref="Render"/>; invalidations are
    /// coalesced until the next rendered frame.
    /// </summary>
    public event Action? RenderRequested;

    /// <summary>Preferred monospace font family currently in use.</summary>
    public string? FontFamily => _fontFamily;

    /// <summary>Cell font size in pixels.</summary>
    public float FontSize => _fontSize;

    /// <summary>Color painted behind and around the cell grid (default black).</summary>
    public SKColor Background { get; set; } = SKColors.Black;

    /// <summary>The window currently being hosted (explicit, loaded, or implicit).</summary>
    public TuiWindow Window => _controller.Window;

    /// <summary>Set when resolving the window content failed; also drawn into the surface.</summary>
    public string? LoadError => _controller.LoadError;

    /// <summary>Grid size in cells of the most recently rendered frame.</summary>
    public int Columns { get; private set; }
    public int Rows { get; private set; }

    /// <summary>Width of one character cell in pixels.</summary>
    public float CellWidth => EnsureSurface().CellWidth;

    /// <summary>Height of one character cell in pixels.</summary>
    public float CellHeight => EnsureSurface().CellHeight;

    /// <summary>
    /// Replaces the hosted content: an existing <paramref name="window"/> (highest
    /// precedence), inline <paramref name="xaml"/> markup, or a path to a XAML file in
    /// <paramref name="source"/>. <paramref name="controller"/> is the event/<c>x:Name</c>
    /// binding target for loaded markup.
    /// </summary>
    public void SetContent(TuiWindow? window = null, string? xaml = null, string? source = null, object? controller = null) =>
        _controller.SetContent(window, xaml, source, controller);

    /// <summary>Changes the font, taking effect on the next rendered frame.</summary>
    public void SetFont(string? fontFamily, float fontSize)
    {
        _fontFamily = fontFamily;
        _fontSize = fontSize;
        _surface?.Dispose();
        _surface = null;
        RenderRequested?.Invoke();
    }

    /// <summary>Number of whole cells that fit in a pixel area.</summary>
    public (int Columns, int Rows) CellsForSize(float pixelWidth, float pixelHeight) =>
        EnsureSurface().CellsForSize(pixelWidth, pixelHeight);

    /// <summary>Pixel size of a cell grid.</summary>
    public (float Width, float Height) SizeForCells(int columns, int rows) =>
        EnsureSurface().SizeForCells(columns, rows);

    private SkiaCellSurface EnsureSurface() =>
        _surface ??= new SkiaCellSurface(_fontFamily, _fontSize);

    // ---------------------------------------------------------------- rendering

    /// <summary>
    /// Renders one TUI frame onto <paramref name="canvas"/>: fills <see cref="Background"/>,
    /// fits as many whole cells as the pixel area holds, then measures, arranges and paints
    /// the window. Load/render errors are drawn into the canvas instead of throwing.
    /// </summary>
    public void Render(SKCanvas canvas, float pixelWidth, float pixelHeight)
    {
        var surface = EnsureSurface();
        canvas.Clear(Background);
        if (pixelWidth < 1 || pixelHeight < 1)
            return;

        (Columns, Rows) = surface.CellsForSize(pixelWidth, pixelHeight);

        if (_controller.LoadError is { } error)
        {
            DrawError(canvas, surface, error);
            return;
        }

        try
        {
            var buffer = _controller.RenderFrame(Columns, Rows, surface.CreateCapabilities());
            surface.Draw(buffer, canvas);
        }
        catch (Exception ex)
        {
            DrawError(canvas, surface, ex.Message);
        }
    }

    /// <summary>
    /// Renders one frame of exactly <paramref name="columns"/> × <paramref name="rows"/>
    /// cells into a new raster image sized to fit the grid.
    /// </summary>
    public SKImage RenderToImage(int columns, int rows)
    {
        if (columns < 1) throw new ArgumentOutOfRangeException(nameof(columns));
        if (rows < 1) throw new ArgumentOutOfRangeException(nameof(rows));

        var surface = EnsureSurface();
        var (width, height) = surface.SizeForCells(columns, rows);
        int pixelWidth = Math.Max(1, (int)MathF.Ceiling(width));
        int pixelHeight = Math.Max(1, (int)MathF.Ceiling(height));

        using var skSurface = SKSurface.Create(new SKImageInfo(pixelWidth, pixelHeight, SKColorType.Bgra8888, SKAlphaType.Premul));
        Render(skSurface.Canvas, pixelWidth, pixelHeight);
        skSurface.Canvas.Flush();
        return skSurface.Snapshot();
    }

    /// <summary>Renders a <paramref name="columns"/> × <paramref name="rows"/> frame as PNG into <paramref name="stream"/>.</summary>
    public void RenderToPng(Stream stream, int columns, int rows)
    {
        using var image = RenderToImage(columns, rows);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        data.SaveTo(stream);
    }

    /// <summary>Renders a <paramref name="columns"/> × <paramref name="rows"/> frame as a PNG file at <paramref name="path"/>.</summary>
    public void RenderToPng(string path, int columns, int rows)
    {
        using var stream = File.Create(path);
        RenderToPng(stream, columns, rows);
    }

    private static void DrawError(SKCanvas canvas, SkiaCellSurface surface, string message)
    {
        using var paint = new SKPaint { Color = new SKColor(0xFF, 0x45, 0x00), IsAntialias = true };
        using var font = new SKFont(SKTypeface.CreateDefault(), surface.FontSize);
        float y = surface.CellHeight;
        foreach (var line in message.Split('\n'))
        {
            canvas.DrawText(line.TrimEnd(), 4, y, SKTextAlign.Left, font, paint);
            y += surface.CellHeight;
        }
    }

    // ---------------------------------------------------------------- input

    /// <summary>Converts a pixel position on the rendered surface to cell coordinates.</summary>
    public (int X, int Y) ToCell(float pixelX, float pixelY)
    {
        var surface = EnsureSurface();
        int cx = Math.Clamp((int)(pixelX / surface.CellWidth), 0, Math.Max(0, Columns - 1));
        int cy = Math.Clamp((int)(pixelY / surface.CellHeight), 0, Math.Max(0, Rows - 1));
        return (cx, cy);
    }

    /// <summary>Forwards a left-button press at a pixel position.</summary>
    public void MouseDown(float pixelX, float pixelY)
    {
        var (cx, cy) = ToCell(pixelX, pixelY);
        _controller.MouseDown(cx, cy);
    }

    /// <summary>Forwards a left-button release at a pixel position.</summary>
    public void MouseUp(float pixelX, float pixelY)
    {
        var (cx, cy) = ToCell(pixelX, pixelY);
        _controller.MouseUp(cx, cy);
    }

    /// <summary>Forwards a pointer move at a pixel position (gated to cell changes).</summary>
    public void MouseMove(float pixelX, float pixelY)
    {
        var (cx, cy) = ToCell(pixelX, pixelY);
        _controller.MouseMove(cx, cy);
    }

    /// <summary>Forwards a key event to the hosted window.</summary>
    public void ProcessKey(ConsoleKey key, char keyChar = '\0', ConsoleModifiers modifiers = ConsoleModifiers.None) =>
        _controller.ProcessKey(key, keyChar, modifiers);

    /// <summary>Types printable text into the hosted window, character by character.</summary>
    public void SendText(string text)
    {
        foreach (var c in text)
        {
            if (c < ' ' || c == '\x7f')
                continue;
            _controller.ProcessKey(MapChar(c), c, ConsoleModifiers.None);
        }
    }

    private static ConsoleKey MapChar(char c)
    {
        if (c >= 'a' && c <= 'z') return ConsoleKey.A + (c - 'a');
        if (c >= 'A' && c <= 'Z') return ConsoleKey.A + (c - 'A');
        if (c >= '0' && c <= '9') return ConsoleKey.D0 + (c - '0');
        if (c == ' ') return ConsoleKey.Spacebar;
        return ConsoleKey.Packet;
    }

    public void Dispose()
    {
        _surface?.Dispose();
        _surface = null;
    }
}
