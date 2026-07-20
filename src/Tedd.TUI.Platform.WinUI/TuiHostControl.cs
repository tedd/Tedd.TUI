using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using SkiaSharp;
using SkiaSharp.Views.Windows;
using Tedd.TUI.Surface.Skia;
using Windows.System;

// Inside the Tedd.TUI.* namespace, unqualified TUI type names win; WinUI types are
// imported through the usings above (which sit outside the namespace) or aliased.
using WinUIDependencyProperty = Microsoft.UI.Xaml.DependencyProperty;
using WinUIDependencyObject = Microsoft.UI.Xaml.DependencyObject;

namespace Tedd.TUI.Platform.WinUI;

/// <summary>
/// Hosts a Tedd.TUI window inside a WinUI 3 visual tree. The TUI renders into a
/// <see cref="VirtualBuffer"/> cell grid painted by the shared <see cref="SkiaCellSurface"/>
/// onto an <see cref="SKXamlCanvas"/>, so the output is identical to every other host.
/// </summary>
/// <remarks>
/// Content is provided via <see cref="Window"/> (an existing <c>TuiWindow</c>),
/// <see cref="Xaml"/> (inline markup) or <see cref="Source"/> (path to a XAML file),
/// in that precedence order. Event handlers in markup bind against <see cref="Controller"/>.
/// </remarks>
public class TuiHostControl : UserControl
{
    public static readonly WinUIDependencyProperty WindowProperty = WinUIDependencyProperty.Register(
        nameof(Window), typeof(TuiWindow), typeof(TuiHostControl), new PropertyMetadata(null, OnContentSourceChanged));

    public static readonly WinUIDependencyProperty XamlProperty = WinUIDependencyProperty.Register(
        nameof(Xaml), typeof(string), typeof(TuiHostControl), new PropertyMetadata(null, OnContentSourceChanged));

    public static readonly WinUIDependencyProperty SourceProperty = WinUIDependencyProperty.Register(
        nameof(Source), typeof(string), typeof(TuiHostControl), new PropertyMetadata(null, OnContentSourceChanged));

    public static readonly WinUIDependencyProperty ControllerProperty = WinUIDependencyProperty.Register(
        nameof(Controller), typeof(object), typeof(TuiHostControl), new PropertyMetadata(null, OnContentSourceChanged));

    public static readonly WinUIDependencyProperty MonoFontFamilyProperty = WinUIDependencyProperty.Register(
        nameof(MonoFontFamily), typeof(string), typeof(TuiHostControl), new PropertyMetadata(null, OnFontChanged));

    public static readonly WinUIDependencyProperty MonoFontSizeProperty = WinUIDependencyProperty.Register(
        nameof(MonoFontSize), typeof(double), typeof(TuiHostControl), new PropertyMetadata(16.0, OnFontChanged));

    /// <summary>An existing window to host. Takes precedence over <see cref="Xaml"/> and <see cref="Source"/>.</summary>
    public TuiWindow? Window
    {
        get => (TuiWindow?)GetValue(WindowProperty);
        set => SetValue(WindowProperty, value);
    }

    /// <summary>Inline TUI XAML markup.</summary>
    public string? Xaml
    {
        get => (string?)GetValue(XamlProperty);
        set => SetValue(XamlProperty, value);
    }

    /// <summary>Path to a TUI XAML file.</summary>
    public string? Source
    {
        get => (string?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /// <summary>Object whose methods/fields bind to event attributes and x:Name fields in loaded markup.</summary>
    public object? Controller
    {
        get => GetValue(ControllerProperty);
        set => SetValue(ControllerProperty, value);
    }

    /// <summary>Preferred monospace font family (comma-separated fallback list allowed).</summary>
    public string? MonoFontFamily
    {
        get => (string?)GetValue(MonoFontFamilyProperty);
        set => SetValue(MonoFontFamilyProperty, value);
    }

    /// <summary>Font size in logical pixels.</summary>
    public double MonoFontSize
    {
        get => (double)GetValue(MonoFontSizeProperty);
        set => SetValue(MonoFontSizeProperty, value);
    }

    /// <summary>The window currently being hosted (explicit, loaded, or implicit).</summary>
    public TuiWindow HostedWindow => _controller.Window;

    /// <summary>Current surface size in character cells.</summary>
    public int Columns { get; private set; }
    public int Rows { get; private set; }

    private readonly TuiSurfaceController _controller = new();
    private readonly SKXamlCanvas _canvas;
    private SkiaCellSurface? _surface;
    private double _lastScale = 1.0;
    private bool _invalidateQueued;

    public TuiHostControl()
    {
        IsTabStop = true;
        Clipboard.RegisterProvider(new WinUIClipboard());

        _canvas = new SKXamlCanvas
        {
            HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch,
            VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Stretch
        };
        _canvas.PaintSurface += OnPaintSurface;
        Content = _canvas;

        _controller.RenderRequested += OnTuiRenderRequested;
        CharacterReceived += OnCharacterReceived;
        Unloaded += (_, _) => CharacterReceived -= OnCharacterReceived;
    }

    private static void OnContentSourceChanged(WinUIDependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var host = (TuiHostControl)d;
        host._controller.SetContent(host.Window, host.Xaml, host.Source, host.Controller);
        host._canvas.Invalidate();
    }

    private static void OnFontChanged(WinUIDependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var host = (TuiHostControl)d;
        host._surface?.Dispose();
        host._surface = null;
        host._canvas.Invalidate();
    }

    private void OnTuiRenderRequested()
    {
        if (_invalidateQueued)
            return;
        _invalidateQueued = true;
        if (!DispatcherQueue.TryEnqueue(() =>
            {
                _invalidateQueued = false;
                _canvas.Invalidate();
            }))
        {
            _invalidateQueued = false;
        }
    }

    private SkiaCellSurface EnsureSurface(double scale)
    {
        if (_surface == null || Math.Abs(scale - _lastScale) > 0.001)
        {
            _surface?.Dispose();
            _surface = new SkiaCellSurface(MonoFontFamily, (float)(MonoFontSize * scale));
            _lastScale = scale;
        }
        return _surface;
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Black);

        double scale = XamlRoot?.RasterizationScale ?? 1.0;
        var surface = EnsureSurface(scale);

        (Columns, Rows) = surface.CellsForSize(e.Info.Width, e.Info.Height);

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

    // Fractional cell coordinates (clamped so the integer cell stays within the grid);
    // sub-cell precision feeds fine-grained drags such as scrollbar thumbs.
    private (double X, double Y) ToCell(Windows.Foundation.Point position)
    {
        var surface = EnsureSurface(_lastScale);
        double cx = Math.Clamp(position.X * _lastScale / surface.CellWidth, 0.0, Math.Max(0, Columns - 1) + 0.999);
        double cy = Math.Clamp(position.Y * _lastScale / surface.CellHeight, 0.0, Math.Max(0, Rows - 1) + 0.999);
        return (cx, cy);
    }

    protected override void OnPointerPressed(PointerRoutedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus(FocusState.Pointer);
        var point = e.GetCurrentPoint(this);
        if (!point.Properties.IsLeftButtonPressed)
            return;
        CapturePointer(e.Pointer);
        var (cx, cy) = ToCell(point.Position);
        _controller.MouseDown(cx, cy);
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerRoutedEventArgs e)
    {
        base.OnPointerReleased(e);
        ReleasePointerCapture(e.Pointer);
        var (cx, cy) = ToCell(e.GetCurrentPoint(this).Position);
        _controller.MouseUp(cx, cy);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerRoutedEventArgs e)
    {
        base.OnPointerMoved(e);
        var (cx, cy) = ToCell(e.GetCurrentPoint(this).Position);
        _controller.MouseMove(cx, cy);
    }

    protected override void OnPointerWheelChanged(PointerRoutedEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        var point = e.GetCurrentPoint(this);
        int delta = point.Properties.MouseWheelDelta; // ±120 per notch
        if (delta == 0)
            return;
        var (cx, cy) = ToCell(point.Position);
        _controller.MouseWheel(cx, cy, delta);
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyRoutedEventArgs e)
    {
        base.OnKeyDown(e);

        var modifiers = WinUIKeyMapper.GetCurrentModifiers();

        ConsoleKey? mapped;
        if (WinUIKeyMapper.IsControlKey(e.Key))
            mapped = WinUIKeyMapper.Map(e.Key);
        else if ((modifiers & (ConsoleModifiers.Control | ConsoleModifiers.Alt)) != 0)
            mapped = WinUIKeyMapper.Map(e.Key);
        else
            return; // plain printable input arrives via CharacterReceived

        if (mapped == null)
            return;

        _controller.ProcessKey(mapped.Value, '\0', modifiers);
        e.Handled = true;
    }

    private void OnCharacterReceived(Microsoft.UI.Xaml.UIElement sender, CharacterReceivedRoutedEventArgs e)
    {
        char c = e.Character;
        if (c < ' ' || c == '\x7f')
            return;
        _controller.ProcessKey(WinUIKeyMapper.MapChar(c), c, WinUIKeyMapper.GetCurrentModifiers());
        e.Handled = true;
    }
}
