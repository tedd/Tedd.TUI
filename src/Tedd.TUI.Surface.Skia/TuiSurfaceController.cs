using System;
using System.Collections.Generic;
using System.IO;

namespace Tedd.TUI.Surface.Skia;

/// <summary>
/// Framework-agnostic host logic shared by the Skia-based platform hosts (Avalonia,
/// WinUI, MAUI): resolves the hosted <see cref="TuiWindow"/> from an explicit window /
/// inline XAML / XAML file, requests repaints when the TUI invalidates, renders frames
/// into a <see cref="VirtualBuffer"/>, and forwards input in cell coordinates.
/// </summary>
public sealed class TuiSurfaceController
{
    private TuiWindow? _window;
    private bool _initialFocusDone;
    private int _lastMouseCellX = -1, _lastMouseCellY = -1;

    /// <summary>Explicit window to host; takes precedence over <see cref="Xaml"/> and <see cref="Source"/>.</summary>
    public TuiWindow? ExplicitWindow { get; private set; }

    /// <summary>Inline TUI XAML markup.</summary>
    public string? Xaml { get; private set; }

    /// <summary>Path to a TUI XAML file (absolute, or relative to the current/app base directory).</summary>
    public string? Source { get; private set; }

    /// <summary>Event/x:Name binding target for loaded markup.</summary>
    public object? Controller { get; private set; }

    /// <summary>Set when resolving the window content failed; hosts should display it.</summary>
    public string? LoadError { get; private set; }

    /// <summary>
    /// Raised (possibly from a non-UI thread) whenever the hosted TUI needs a repaint.
    /// Hosts marshal this to their UI thread and invalidate their surface. Invalidations
    /// are coalesced until the next <see cref="RenderFrame"/>.
    /// </summary>
    public event Action? RenderRequested;

    private bool _renderPending;

    /// <summary>The hosted window, resolving it on first access.</summary>
    public TuiWindow Window => EnsureWindow();

    /// <summary>Replaces the content configuration and detaches any current window.</summary>
    public void SetContent(TuiWindow? window = null, string? xaml = null, string? source = null, object? controller = null)
    {
        DetachWindow();
        ExplicitWindow = window;
        Xaml = xaml;
        Source = source;
        Controller = controller;
        LoadError = null;
        RequestRender();
    }

    private TuiWindow EnsureWindow()
    {
        if (_window != null)
            return _window;

        TuiWindow window;
        if (ExplicitWindow != null)
        {
            window = ExplicitWindow;
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
                LoadError = ex.Message;
            }

            window = root as TuiWindow ?? new TuiWindow();
            if (root != null && root is not TuiWindow)
                window.Content = root;
        }

        _window = window;
        _window.VisualChanged += OnVisualChanged;
        _initialFocusDone = false;
        return _window;
    }

    private void DetachWindow()
    {
        if (_window == null)
            return;
        _window.VisualChanged -= OnVisualChanged;
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

    private void OnVisualChanged(object? sender, EventArgs e) => RequestRender();

    private void RequestRender()
    {
        if (_renderPending)
            return;
        _renderPending = true;
        RenderRequested?.Invoke();
    }

    /// <summary>
    /// Runs one TUI frame: measure, arrange and render the window into a fresh buffer
    /// of <paramref name="columns"/> × <paramref name="rows"/> cells.
    /// </summary>
    public VirtualBuffer RenderFrame(int columns, int rows, SurfaceCapabilities capabilities)
    {
        _renderPending = false;

        var window = EnsureWindow();
        window.Capabilities = capabilities;
        window.Measure(new Size(columns, rows));
        window.Arrange(new Rect(0, 0, columns, rows));

        if (!_initialFocusDone)
        {
            _initialFocusDone = true;
            window.EnsureInitialFocus();
        }

        var buffer = new VirtualBuffer(columns, rows);
        if (capabilities.SupportsGraphics)
            buffer.Graphics = new List<GraphicPlacement>();
        window.Render(buffer, 0, 0);
        return buffer;
    }

    // ---------------------------------------------------------------- input forwarding

    public void ProcessKey(ConsoleKey key, char keyChar, ConsoleModifiers modifiers)
    {
        _window?.ProcessKey(new KeyEventArgs { Key = key, KeyChar = keyChar, Modifiers = modifiers });
    }

    public void MouseDown(int cellX, int cellY) => SendMouse(cellX, cellY, UIElement.MouseDownEvent);

    public void MouseUp(int cellX, int cellY) => SendMouse(cellX, cellY, UIElement.MouseUpEvent);

    /// <summary>Forwards a move only when the hovered cell changed, to avoid flooding the TUI.</summary>
    public void MouseMove(int cellX, int cellY)
    {
        if (cellX == _lastMouseCellX && cellY == _lastMouseCellY)
            return;
        _lastMouseCellX = cellX;
        _lastMouseCellY = cellY;
        SendMouse(cellX, cellY, UIElement.MouseMoveEvent);
    }

    private void SendMouse(int cellX, int cellY, RoutedEvent routedEvent)
    {
        _window?.ProcessMouse(new MouseEventArgs(routedEvent) { GlobalX = cellX, GlobalY = cellY });
    }
}
