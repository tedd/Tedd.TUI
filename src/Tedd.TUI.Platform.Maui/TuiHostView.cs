using System;
using Microsoft.Maui.Controls;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using Tedd.TUI.Surface.Skia;

namespace Tedd.TUI.Platform.Maui;

/// <summary>
/// Hosts a Tedd.TUI window inside a .NET MAUI page. The TUI renders into a
/// <see cref="VirtualBuffer"/> cell grid painted by the shared <see cref="SkiaCellSurface"/>
/// onto this <see cref="SKCanvasView"/>, so the output is identical to every other host.
/// </summary>
/// <remarks>
/// <para>Content is provided via <see cref="Window"/> (an existing <c>TuiWindow</c>),
/// <see cref="Xaml"/> (inline markup) or <see cref="Source"/> (path to a XAML file),
/// in that precedence order. Event handlers in markup bind against <see cref="Controller"/>.</para>
/// <para>Touch/mouse input maps to TUI mouse events. MAUI has no cross-platform keyboard
/// event surface, so hardware keyboard input must be injected via <see cref="SendKey"/> /
/// <see cref="SendText"/> (e.g. from a platform effect or a hidden Entry).</para>
/// <para>Remember to call <c>.UseTeddTui()</c> (or <c>.UseSkiaSharp()</c>) on the
/// <c>MauiAppBuilder</c>.</para>
/// </remarks>
public class TuiHostView : SKCanvasView
{
    public static readonly BindableProperty WindowProperty = BindableProperty.Create(
        nameof(Window), typeof(TuiWindow), typeof(TuiHostView), null, propertyChanged: OnContentSourceChanged);

    public static readonly BindableProperty XamlProperty = BindableProperty.Create(
        nameof(Xaml), typeof(string), typeof(TuiHostView), null, propertyChanged: OnContentSourceChanged);

    public static readonly BindableProperty SourceProperty = BindableProperty.Create(
        nameof(Source), typeof(string), typeof(TuiHostView), null, propertyChanged: OnContentSourceChanged);

    public static readonly BindableProperty ControllerProperty = BindableProperty.Create(
        nameof(Controller), typeof(object), typeof(TuiHostView), null, propertyChanged: OnContentSourceChanged);

    public static readonly BindableProperty FontFamilyProperty = BindableProperty.Create(
        nameof(FontFamily), typeof(string), typeof(TuiHostView), null, propertyChanged: OnFontChanged);

    public static readonly BindableProperty FontSizeProperty = BindableProperty.Create(
        nameof(FontSize), typeof(double), typeof(TuiHostView), 16.0, propertyChanged: OnFontChanged);

    /// <summary>An existing window to host. Takes precedence over <see cref="Xaml"/> and <see cref="Source"/>.
    /// Hides <c>VisualElement.Window</c> (the MAUI window remains reachable through the base class).</summary>
    public new TuiWindow? Window
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
    public string? FontFamily
    {
        get => (string?)GetValue(FontFamilyProperty);
        set => SetValue(FontFamilyProperty, value);
    }

    /// <summary>Font size in device-independent units.</summary>
    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    /// <summary>The window currently being hosted (explicit, loaded, or implicit).</summary>
    public TuiWindow HostedWindow => _controller.Window;

    /// <summary>Current surface size in character cells.</summary>
    public int Columns { get; private set; }
    public int Rows { get; private set; }

    private readonly TuiSurfaceController _controller = new();
    private SkiaCellSurface? _surface;
    private double _lastScale = 1.0;
    private bool _invalidateQueued;

    public TuiHostView()
    {
        EnableTouchEvents = true;
        _controller.RenderRequested += OnTuiRenderRequested;
    }

    private static void OnContentSourceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var host = (TuiHostView)bindable;
        host._controller.SetContent(host.Window, host.Xaml, host.Source, host.Controller);
        host.InvalidateSurface();
    }

    private static void OnFontChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var host = (TuiHostView)bindable;
        host._surface?.Dispose();
        host._surface = null;
        host.InvalidateSurface();
    }

    private void OnTuiRenderRequested()
    {
        if (_invalidateQueued)
            return;
        _invalidateQueued = true;
        Dispatcher.Dispatch(() =>
        {
            _invalidateQueued = false;
            InvalidateSurface();
        });
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

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        base.OnPaintSurface(e);

        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Black);

        // Canvas pixels per device-independent unit.
        double scale = Width > 0 ? e.Info.Width / Width : 1.0;
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

    protected override void OnTouch(SKTouchEventArgs e)
    {
        base.OnTouch(e);

        var surface = EnsureSurface(_lastScale);
        // SKTouchEventArgs locations are already in canvas pixels.
        int cx = Math.Clamp((int)(e.Location.X / surface.CellWidth), 0, Math.Max(0, Columns - 1));
        int cy = Math.Clamp((int)(e.Location.Y / surface.CellHeight), 0, Math.Max(0, Rows - 1));

        switch (e.ActionType)
        {
            case SKTouchAction.Pressed:
                _controller.MouseDown(cx, cy);
                break;
            case SKTouchAction.Moved:
                _controller.MouseMove(cx, cy);
                break;
            case SKTouchAction.Released:
                _controller.MouseUp(cx, cy);
                break;
            default:
                return;
        }

        e.Handled = true;
    }

    /// <summary>Injects a key press into the TUI (MAUI exposes no cross-platform key events).</summary>
    public void SendKey(ConsoleKey key, char keyChar = '\0', ConsoleModifiers modifiers = 0)
        => _controller.ProcessKey(key, keyChar, modifiers);

    /// <summary>Injects typed text into the TUI, one key event per character.</summary>
    public void SendText(string text)
    {
        foreach (var c in text)
        {
            var key = c switch
            {
                >= 'a' and <= 'z' => ConsoleKey.A + (c - 'a'),
                >= 'A' and <= 'Z' => ConsoleKey.A + (c - 'A'),
                >= '0' and <= '9' => ConsoleKey.D0 + (c - '0'),
                ' ' => ConsoleKey.Spacebar,
                '\n' or '\r' => ConsoleKey.Enter,
                '\t' => ConsoleKey.Tab,
                _ => ConsoleKey.Packet
            };
            _controller.ProcessKey(key, c is '\n' or '\r' or '\t' ? '\0' : c, 0);
        }
    }
}
