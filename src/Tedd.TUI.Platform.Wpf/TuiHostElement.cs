using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

// The enclosing namespace makes unqualified Size/Rect/KeyEventArgs/... resolve to the
// Tedd.TUI types; the colliding WPF types are aliased explicitly.
using WpfDependencyProperty = System.Windows.DependencyProperty;
using WpfDependencyObject = System.Windows.DependencyObject;
using WpfSize = System.Windows.Size;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;
using WpfMouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using WpfTextCompositionEventArgs = System.Windows.Input.TextCompositionEventArgs;
using WpfKeyboard = System.Windows.Input.Keyboard;
using WpfMouseButton = System.Windows.Input.MouseButton;

namespace Tedd.TUI.Platform.Wpf;

/// <summary>
/// Hosts a Tedd.TUI window inside a WPF visual tree. The element runs the real TUI
/// pipeline — layout into a <see cref="VirtualBuffer"/> cell grid — and paints the grid
/// with a monospace typeface, so what WPF shows is cell-for-cell what a terminal shows.
/// </summary>
/// <remarks>
/// <para>Three ways to provide content, in precedence order:</para>
/// <list type="number">
///   <item><see cref="Window"/> — attach an existing <see cref="TuiWindow"/> built in code.</item>
///   <item><see cref="Xaml"/> — inline TUI XAML markup loaded via <c>XamlLoader</c>.</item>
///   <item><see cref="Source"/> — path to a TUI XAML file (absolute, or relative to the
///   current directory / application base directory).</item>
/// </list>
/// <para>Event handler attributes in loaded markup bind against <see cref="Controller"/>.
/// The element is designer-friendly: it renders in the Visual Studio XAML designer, and
/// load errors are painted into the surface instead of throwing.</para>
/// </remarks>
public class TuiHostElement : FrameworkElement
{
    public static readonly WpfDependencyProperty WindowProperty = WpfDependencyProperty.Register(
        nameof(Window), typeof(TuiWindow), typeof(TuiHostElement),
        new FrameworkPropertyMetadata(null, OnContentSourceChanged));

    public static readonly WpfDependencyProperty XamlProperty = WpfDependencyProperty.Register(
        nameof(Xaml), typeof(string), typeof(TuiHostElement),
        new FrameworkPropertyMetadata(null, OnContentSourceChanged));

    public static readonly WpfDependencyProperty SourceProperty = WpfDependencyProperty.Register(
        nameof(Source), typeof(string), typeof(TuiHostElement),
        new FrameworkPropertyMetadata(null, OnContentSourceChanged));

    public static readonly WpfDependencyProperty ControllerProperty = WpfDependencyProperty.Register(
        nameof(Controller), typeof(object), typeof(TuiHostElement),
        new FrameworkPropertyMetadata(null, OnContentSourceChanged));

    public static readonly WpfDependencyProperty FontFamilyProperty = WpfDependencyProperty.Register(
        nameof(FontFamily), typeof(FontFamily), typeof(TuiHostElement),
        new FrameworkPropertyMetadata(new FontFamily("Cascadia Mono, Consolas, Courier New"), OnFontChanged));

    public static readonly WpfDependencyProperty FontSizeProperty = WpfDependencyProperty.Register(
        nameof(FontSize), typeof(double), typeof(TuiHostElement),
        new FrameworkPropertyMetadata(16.0, OnFontChanged));

    public static readonly WpfDependencyProperty BackgroundProperty = WpfDependencyProperty.Register(
        nameof(Background), typeof(Brush), typeof(TuiHostElement),
        new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender));

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

    /// <summary>Monospace font used for the cell grid.</summary>
    public FontFamily FontFamily
    {
        get => (FontFamily)GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    /// <summary>Fill behind/around the cell grid. Also makes the whole element hit-testable.</summary>
    public Brush Background
    {
        get => (Brush)GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    /// <summary>The window currently being hosted (explicit, loaded, or implicit).</summary>
    public TuiWindow HostedWindow => EnsureWindow();

    /// <summary>Current surface size in character cells.</summary>
    public int Columns { get; private set; }
    public int Rows { get; private set; }

    private TuiWindow? _window;
    private string? _loadError;
    private bool _renderQueued;
    private bool _initialFocusDone;
    private int _lastMouseCellX = -1, _lastMouseCellY = -1;

    private Typeface? _typeface;
    private double _cellWidth = 8, _cellHeight = 16;
    private double _pixelsPerDip = 1.0;

    private readonly Dictionary<uint, SolidColorBrush> _brushCache = new();
    private static readonly ConditionalWeakTable<byte[], BitmapSource> _imageCache = new();

    public TuiHostElement()
    {
        Focusable = true;
        FocusVisualStyle = null;
        SnapsToDevicePixels = true;
        SizeChanged += (_, _) => InvalidateVisual();
        Loaded += (_, _) => { _pixelsPerDip = System.Windows.Media.VisualTreeHelper.GetDpi(this).PixelsPerDip; MeasureCell(); InvalidateVisual(); };
        Unloaded += (_, _) => DetachWindow();
    }

    private static void OnContentSourceChanged(WpfDependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var host = (TuiHostElement)d;
        host.DetachWindow();
        host._loadError = null;
        host.InvalidateVisual();
    }

    private static void OnFontChanged(WpfDependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var host = (TuiHostElement)d;
        host._typeface = null;
        host.MeasureCell();
        host.InvalidateVisual();
    }

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        _pixelsPerDip = newDpi.PixelsPerDip;
        MeasureCell();
        InvalidateVisual();
    }

    // ---------------------------------------------------------------- window lifecycle

    private TuiWindow EnsureWindow()
    {
        if (_window != null)
            return _window;

        TuiWindow window;
        if (Window != null)
        {
            window = Window;
        }
        else
        {
            UIElement? root = null;
            try
            {
                var markup = Xaml ?? (Source != null ? ReadSourceFile(Source) : null);
                if (markup != null)
                    root = XamlLoader.Load(markup, Controller);
            }
            catch (Exception ex)
            {
                _loadError = ex.Message;
            }

            window = root as TuiWindow ?? new TuiWindow();
            if (root != null && root is not TuiWindow)
                window.Content = root;
        }

        _window = window;
        _window.VisualChanged += OnTuiVisualChanged;
        _initialFocusDone = false;
        return _window;
    }

    private void DetachWindow()
    {
        if (_window == null)
            return;
        _window.VisualChanged -= OnTuiVisualChanged;
        _window = null;
    }

    private static string ReadSourceFile(string source)
    {
        if (File.Exists(source))
            return File.ReadAllText(source);

        var baseCandidate = Path.Combine(AppContext.BaseDirectory, source);
        if (File.Exists(baseCandidate))
            return File.ReadAllText(baseCandidate);

        throw new FileNotFoundException($"TUI XAML file not found: '{source}' (also probed application base directory).", source);
    }

    private void OnTuiVisualChanged(object? sender, EventArgs e)
    {
        // Coalesce invalidations; VisualChanged can fire from any thread.
        if (_renderQueued)
            return;
        _renderQueued = true;
        Dispatcher.BeginInvoke(DispatcherPriority.Render, () =>
        {
            _renderQueued = false;
            InvalidateVisual();
        });
    }

    // ---------------------------------------------------------------- layout & painting

    private void MeasureCell()
    {
        _typeface = new Typeface(FontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        var probe = new FormattedText(
            "W", CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            _typeface, FontSize, Brushes.White, _pixelsPerDip);
        _cellWidth = Math.Max(1.0, probe.WidthIncludingTrailingWhitespace);
        _cellHeight = Math.Max(1.0, probe.Height);
    }

    protected override WpfSize MeasureOverride(WpfSize availableSize)
    {
        if (_typeface == null)
            MeasureCell();

        // Fill whatever space is given; fall back to a classic 80x25 surface when unconstrained.
        double w = double.IsInfinity(availableSize.Width) ? 80 * _cellWidth : availableSize.Width;
        double h = double.IsInfinity(availableSize.Height) ? 25 * _cellHeight : availableSize.Height;
        return new WpfSize(w, h);
    }

    protected override void OnRender(DrawingContext dc)
    {
        if (_typeface == null)
            MeasureCell();

        double width = ActualWidth, height = ActualHeight;
        dc.DrawRectangle(Background, null, new System.Windows.Rect(0, 0, width, height));
        if (width < 1 || height < 1)
            return;

        Columns = Math.Max(1, (int)(width / _cellWidth));
        Rows = Math.Max(1, (int)(height / _cellHeight));

        var window = EnsureWindow();

        if (_loadError != null)
        {
            DrawErrorText(dc, _loadError, width);
            return;
        }

        VirtualBuffer buffer;
        try
        {
            window.Capabilities = new SurfaceCapabilities
            {
                SupportsGraphics = true,
                CharPixelWidth = Math.Max(1, (int)Math.Round(_cellWidth * _pixelsPerDip)),
                CharPixelHeight = Math.Max(1, (int)Math.Round(_cellHeight * _pixelsPerDip))
            };

            window.Measure(new Size(Columns, Rows));
            window.Arrange(new Rect(0, 0, Columns, Rows));

            if (!_initialFocusDone)
            {
                _initialFocusDone = true;
                window.EnsureInitialFocus();
            }

            buffer = new VirtualBuffer(Columns, Rows) { Graphics = new List<GraphicPlacement>() };
            window.Render(buffer, 0, 0);
        }
        catch (Exception ex)
        {
            // Never take down the WPF visual tree (or the XAML designer) on a TUI error.
            DrawErrorText(dc, ex.ToString(), width);
            return;
        }

        DrawBuffer(dc, buffer);
    }

    private void DrawBuffer(DrawingContext dc, VirtualBuffer buffer)
    {
        var cells = buffer.Cells;
        int cols = buffer.Width, rows = buffer.Height;

        // Pass 1: background runs (adjacent cells with the same background collapse into one rect).
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
                    dc.DrawRectangle(GetBrush(bg), null, new System.Windows.Rect(
                        runStart * _cellWidth, y * _cellHeight,
                        (x - runStart) * _cellWidth, _cellHeight));
                }
            }
        }

        // Pass 2: text runs (adjacent cells with the same foreground collapse into one FormattedText).
        var sb = new System.Text.StringBuilder(cols);
        for (int y = 0; y < rows; y++)
        {
            int rowStart = y * cols;
            int x = 0;
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
                sb.Clear();
                while (x < cols)
                {
                    var c = cells[rowStart + x];
                    if (c.Foreground.Packed != fg.Packed || c.Character == '\0')
                        break;
                    sb.Append(c.Character);
                    x++;
                }

                var text = new FormattedText(
                    sb.ToString(), CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                    _typeface!, FontSize, GetBrush(fg), _pixelsPerDip);
                dc.DrawText(text, new System.Windows.Point(runStart * _cellWidth, y * _cellHeight));
            }
        }

        // Pass 3: bitmap graphics composited over the grid.
        if (buffer.Graphics is { Count: > 0 })
        {
            foreach (var placement in buffer.Graphics)
            {
                var bitmap = ResolveBitmap(placement);
                if (bitmap == null)
                    continue;
                dc.DrawImage(bitmap, new System.Windows.Rect(
                    placement.CharX * _cellWidth, placement.CharY * _cellHeight,
                    placement.CharWidth * _cellWidth, placement.CharHeight * _cellHeight));
            }
        }
    }

    private void DrawErrorText(DrawingContext dc, string message, double width)
    {
        var text = new FormattedText(
            message, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
            _typeface!, FontSize, Brushes.OrangeRed, _pixelsPerDip)
        {
            MaxTextWidth = Math.Max(50, width - 8)
        };
        dc.DrawText(text, new System.Windows.Point(4, 4));
    }

    private SolidColorBrush GetBrush(TuiColor color)
    {
        if (_brushCache.TryGetValue(color.Packed, out var brush))
            return brush;
        brush = new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
        brush.Freeze();
        _brushCache[color.Packed] = brush;
        return brush;
    }

    private static BitmapSource? ResolveBitmap(GraphicPlacement placement)
    {
        if (placement.ImageData is { Length: > 0 } encoded)
        {
            if (_imageCache.TryGetValue(encoded, out var cached))
                return cached;
            try
            {
                var image = new BitmapImage();
                using var stream = new MemoryStream(encoded, writable: false);
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
                image.Freeze();
                _imageCache.Add(encoded, image);
                return image;
            }
            catch
            {
                return null;
            }
        }

        if (placement.Pixels is { Length: > 0 } rgba && placement.PixelWidth > 0 && placement.PixelHeight > 0)
        {
            if (_imageCache.TryGetValue(rgba, out var cached))
                return cached;
            // VirtualBuffer graphics carry RGBA; WPF wants BGRA.
            var bgra = new byte[rgba.Length];
            for (int i = 0; i + 3 < rgba.Length; i += 4)
            {
                bgra[i] = rgba[i + 2];
                bgra[i + 1] = rgba[i + 1];
                bgra[i + 2] = rgba[i];
                bgra[i + 3] = rgba[i + 3];
            }
            var bmp = BitmapSource.Create(
                placement.PixelWidth, placement.PixelHeight, 96, 96,
                PixelFormats.Bgra32, null, bgra, placement.PixelWidth * 4);
            bmp.Freeze();
            _imageCache.Add(rgba, bmp);
            return bmp;
        }

        return null;
    }

    // ---------------------------------------------------------------- input

    protected override void OnMouseDown(WpfMouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        WpfKeyboard.Focus(this);
        if (e.ChangedButton != WpfMouseButton.Left)
            return;
        CaptureMouse();
        SendMouse(e, UIElement.MouseDownEvent);
        e.Handled = true;
    }

    protected override void OnMouseUp(WpfMouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.ChangedButton != WpfMouseButton.Left)
            return;
        ReleaseMouseCapture();
        SendMouse(e, UIElement.MouseUpEvent);
        e.Handled = true;
    }

    protected override void OnMouseMove(WpfMouseEventArgs e)
    {
        base.OnMouseMove(e);
        var (cx, cy) = ToCell(e);
        // Mouse-move only matters at cell granularity; gate on cell change to avoid
        // flooding the TUI with sub-cell movements.
        if (cx == _lastMouseCellX && cy == _lastMouseCellY)
            return;
        _lastMouseCellX = cx;
        _lastMouseCellY = cy;
        SendMouseCell(cx, cy, UIElement.MouseMoveEvent);
    }

    private (int X, int Y) ToCell(WpfMouseEventArgs e)
    {
        var pos = e.GetPosition(this);
        int cx = Math.Clamp((int)(pos.X / _cellWidth), 0, Math.Max(0, Columns - 1));
        int cy = Math.Clamp((int)(pos.Y / _cellHeight), 0, Math.Max(0, Rows - 1));
        return (cx, cy);
    }

    private void SendMouse(WpfMouseEventArgs e, RoutedEvent routedEvent)
    {
        var (cx, cy) = ToCell(e);
        SendMouseCell(cx, cy, routedEvent);
    }

    private void SendMouseCell(int cx, int cy, RoutedEvent routedEvent)
    {
        if (_window == null)
            return;
        _window.ProcessMouse(new MouseEventArgs(routedEvent) { GlobalX = cx, GlobalY = cy });
    }

    protected override void OnKeyDown(WpfKeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (_window == null)
            return;

        var modifiers = WpfKeyMapper.MapModifiers(WpfKeyboard.Modifiers);
        var key = e.Key == System.Windows.Input.Key.System ? e.SystemKey : e.Key;

        ConsoleKey? mapped;
        if (WpfKeyMapper.IsControlKey(key))
        {
            mapped = WpfKeyMapper.Map(key);
        }
        else if ((modifiers & (ConsoleModifiers.Control | ConsoleModifiers.Alt)) != 0)
        {
            // Ctrl/Alt chords never produce usable TextInput; deliver them from KeyDown.
            mapped = WpfKeyMapper.Map(key);
        }
        else
        {
            // Plain printable keys arrive via OnTextInput with the correctly translated character.
            return;
        }

        if (mapped == null)
            return;

        _window.ProcessKey(new KeyEventArgs
        {
            Key = mapped.Value,
            KeyChar = '\0',
            Modifiers = modifiers
        });
        e.Handled = true;
    }

    protected override void OnTextInput(WpfTextCompositionEventArgs e)
    {
        base.OnTextInput(e);
        if (_window == null || string.IsNullOrEmpty(e.Text))
            return;

        foreach (var c in e.Text)
        {
            // Control characters (Enter=\r, Tab=\t, Backspace=\b, …) were already delivered
            // through OnKeyDown; only genuine printable input flows through here.
            if (c < ' ' || c == '\x7f')
                continue;

            _window.ProcessKey(new KeyEventArgs
            {
                Key = WpfKeyMapper.MapChar(c),
                KeyChar = c,
                Modifiers = WpfKeyMapper.MapModifiers(WpfKeyboard.Modifiers)
            });
        }
        e.Handled = true;
    }
}
