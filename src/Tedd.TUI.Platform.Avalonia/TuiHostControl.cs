using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using SkiaSharp;
using Tedd.TUI.Surface.Skia;

// Inside the Tedd.TUI.* namespace, unqualified Size/Rect/KeyEventArgs resolve to the TUI
// types; the colliding Avalonia types are aliased explicitly.
using AvSize = Avalonia.Size;
using AvRect = Avalonia.Rect;
using AvKeyEventArgs = Avalonia.Input.KeyEventArgs;
using AvTextInputEventArgs = Avalonia.Input.TextInputEventArgs;
using AvControl = Avalonia.Controls.Control;

namespace Tedd.TUI.Platform.Avalonia;

/// <summary>
/// Hosts a Tedd.TUI window inside an Avalonia visual tree. The TUI renders into a
/// <see cref="VirtualBuffer"/> cell grid which is painted with the shared
/// <see cref="SkiaCellSurface"/> into a writeable bitmap, so the output is identical
/// to every other Tedd.TUI host.
/// </summary>
/// <remarks>
/// Content is provided via <see cref="Window"/> (an existing <c>TuiWindow</c>),
/// <see cref="Xaml"/> (inline markup) or <see cref="Source"/> (path to a XAML file),
/// in that precedence order. Event handlers in markup bind against <see cref="Controller"/>.
/// </remarks>
public class TuiHostControl : AvControl
{
    public static readonly StyledProperty<TuiWindow?> WindowProperty =
        AvaloniaProperty.Register<TuiHostControl, TuiWindow?>(nameof(Window));

    public static readonly StyledProperty<string?> XamlProperty =
        AvaloniaProperty.Register<TuiHostControl, string?>(nameof(Xaml));

    public static readonly StyledProperty<string?> SourceProperty =
        AvaloniaProperty.Register<TuiHostControl, string?>(nameof(Source));

    public static readonly StyledProperty<object?> ControllerProperty =
        AvaloniaProperty.Register<TuiHostControl, object?>(nameof(Controller));

    public static readonly StyledProperty<string?> FontFamilyProperty =
        AvaloniaProperty.Register<TuiHostControl, string?>(nameof(FontFamily));

    public static readonly StyledProperty<double> FontSizeProperty =
        AvaloniaProperty.Register<TuiHostControl, double>(nameof(FontSize), 16.0);

    /// <summary>An existing window to host. Takes precedence over <see cref="Xaml"/> and <see cref="Source"/>.</summary>
    public TuiWindow? Window
    {
        get => GetValue(WindowProperty);
        set => SetValue(WindowProperty, value);
    }

    /// <summary>Inline TUI XAML markup.</summary>
    public string? Xaml
    {
        get => GetValue(XamlProperty);
        set => SetValue(XamlProperty, value);
    }

    /// <summary>Path to a TUI XAML file.</summary>
    public string? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /// <summary>Object whose methods/fields bind to event attributes and x:Name fields in loaded markup.</summary>
    public object? Controller
    {
        get => GetValue(ControllerProperty);
        set => SetValue(ControllerProperty, value);
    }

    /// <summary>Preferred monospace font family (comma-separated fallback list allowed).</summary>
    public string? FontFamily
    {
        get => GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    /// <summary>Font size in pixels (at 100% scaling).</summary>
    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    /// <summary>The window currently being hosted (explicit, loaded, or implicit).</summary>
    public TuiWindow HostedWindow => _controller.Window;

    /// <summary>Current surface size in character cells.</summary>
    public int Columns { get; private set; }
    public int Rows { get; private set; }

    private readonly TuiSurfaceController _controller = new();
    private SkiaCellSurface? _surface;
    private WriteableBitmap? _bitmap;
    private double _lastScale = 1.0;
    private bool _invalidateQueued;

    public TuiHostControl()
    {
        Focusable = true;
        _controller.RenderRequested += OnTuiRenderRequested;
    }

    private void OnTuiRenderRequested()
    {
        if (_invalidateQueued)
            return;
        _invalidateQueued = true;
        Dispatcher.UIThread.Post(() =>
        {
            _invalidateQueued = false;
            InvalidateVisual();
        }, DispatcherPriority.Render);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == WindowProperty || change.Property == XamlProperty ||
            change.Property == SourceProperty || change.Property == ControllerProperty)
        {
            _controller.SetContent(Window, Xaml, Source, Controller);
            InvalidateVisual();
        }
        else if (change.Property == FontFamilyProperty || change.Property == FontSizeProperty)
        {
            _surface?.Dispose();
            _surface = null;
            InvalidateVisual();
        }
    }

    private SkiaCellSurface EnsureSurface(double scale)
    {
        if (_surface == null || Math.Abs(scale - _lastScale) > 0.001)
        {
            _surface?.Dispose();
            _surface = new SkiaCellSurface(FontFamily, (float)(FontSize * scale));
            _lastScale = scale;
        }
        return _surface;
    }

    protected override AvSize MeasureOverride(AvSize availableSize)
    {
        var surface = EnsureSurface(_lastScale <= 0 ? 1.0 : _lastScale);
        double w = double.IsInfinity(availableSize.Width) ? 80 * surface.CellWidth / _lastScale : availableSize.Width;
        double h = double.IsInfinity(availableSize.Height) ? 25 * surface.CellHeight / _lastScale : availableSize.Height;
        return new AvSize(w, h);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = Bounds;
        context.FillRectangle(Brushes.Black, new AvRect(0, 0, bounds.Width, bounds.Height));
        if (bounds.Width < 1 || bounds.Height < 1)
            return;

        double scale = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
        var surface = EnsureSurface(scale);

        int pixelWidth = Math.Max(1, (int)Math.Ceiling(bounds.Width * scale));
        int pixelHeight = Math.Max(1, (int)Math.Ceiling(bounds.Height * scale));
        (Columns, Rows) = surface.CellsForSize(pixelWidth, pixelHeight);

        if (_controller.LoadError is { } error)
        {
            DrawError(context, error, bounds);
            return;
        }

        try
        {
            var buffer = _controller.RenderFrame(Columns, Rows, surface.CreateCapabilities());

            if (_bitmap == null || _bitmap.PixelSize.Width != pixelWidth || _bitmap.PixelSize.Height != pixelHeight)
            {
                _bitmap?.Dispose();
                _bitmap = new WriteableBitmap(
                    new PixelSize(pixelWidth, pixelHeight),
                    new Vector(96 * scale, 96 * scale),
                    PixelFormat.Bgra8888, AlphaFormat.Premul);
            }

            using (var fb = _bitmap.Lock())
            {
                var info = new SKImageInfo(pixelWidth, pixelHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
                using var skSurface = SKSurface.Create(info, fb.Address, fb.RowBytes);
                skSurface.Canvas.Clear(SKColors.Black);
                surface.Draw(buffer, skSurface.Canvas);
                skSurface.Canvas.Flush();
            }

            context.DrawImage(_bitmap,
                new AvRect(0, 0, pixelWidth, pixelHeight),
                new AvRect(0, 0, pixelWidth / scale, pixelHeight / scale));
        }
        catch (Exception ex)
        {
            DrawError(context, ex.Message, bounds);
        }
    }

    private void DrawError(DrawingContext context, string message, AvRect bounds)
    {
        var text = new FormattedText(
            message, System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            new Typeface(FontFamily ?? "monospace"), FontSize, Brushes.OrangeRed)
        {
            MaxTextWidth = Math.Max(50, bounds.Width - 8)
        };
        context.DrawText(text, new global::Avalonia.Point(4, 4));
    }

    // ---------------------------------------------------------------- input

    private (int X, int Y) ToCell(global::Avalonia.Point position)
    {
        double scale = _lastScale <= 0 ? 1.0 : _lastScale;
        var surface = EnsureSurface(scale);
        int cx = Math.Clamp((int)(position.X * scale / surface.CellWidth), 0, Math.Max(0, Columns - 1));
        int cy = Math.Clamp((int)(position.Y * scale / surface.CellHeight), 0, Math.Max(0, Rows - 1));
        return (cx, cy);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();
        var point = e.GetCurrentPoint(this);
        if (!point.Properties.IsLeftButtonPressed)
            return;
        e.Pointer.Capture(this);
        var (cx, cy) = ToCell(point.Position);
        _controller.MouseDown(cx, cy);
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (e.InitialPressMouseButton != MouseButton.Left)
            return;
        e.Pointer.Capture(null);
        var (cx, cy) = ToCell(e.GetPosition(this));
        _controller.MouseUp(cx, cy);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var (cx, cy) = ToCell(e.GetPosition(this));
        _controller.MouseMove(cx, cy);
    }

    protected override void OnKeyDown(AvKeyEventArgs e)
    {
        base.OnKeyDown(e);

        var modifiers = AvaloniaKeyMapper.MapModifiers(e.KeyModifiers);

        ConsoleKey? mapped;
        if (AvaloniaKeyMapper.IsControlKey(e.Key))
            mapped = AvaloniaKeyMapper.Map(e.Key);
        else if ((modifiers & (ConsoleModifiers.Control | ConsoleModifiers.Alt)) != 0)
            mapped = AvaloniaKeyMapper.Map(e.Key);
        else
            return; // plain printable input arrives via OnTextInput

        if (mapped == null)
            return;

        _controller.ProcessKey(mapped.Value, '\0', modifiers);
        e.Handled = true;
    }

    protected override void OnTextInput(AvTextInputEventArgs e)
    {
        base.OnTextInput(e);
        if (string.IsNullOrEmpty(e.Text))
            return;

        foreach (var c in e.Text)
        {
            if (c < ' ' || c == '\x7f')
                continue;
            _controller.ProcessKey(AvaloniaKeyMapper.MapChar(c), c, ConsoleModifiers.None);
        }
        e.Handled = true;
    }
}
