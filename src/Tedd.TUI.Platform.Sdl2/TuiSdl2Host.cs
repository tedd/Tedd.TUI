using System;
using System.Runtime.InteropServices;
using System.Threading;
using SDL2;
using SkiaSharp;
using Tedd.TUI.Platform.Skia;

namespace Tedd.TUI.Platform.Sdl2;

/// <summary>
/// Hosts a Tedd.TUI window inside an SDL2 window. Frames render through the standalone
/// <see cref="TuiSkiaHost"/> into an SDL streaming texture, so the output is identical to
/// every other Tedd.TUI host; SDL keyboard, text-input and mouse events are translated to
/// the TUI input pipeline.
/// </summary>
/// <remarks>
/// <para><b>Owned mode</b> — <see cref="Run"/> initializes SDL, opens a window sized to the
/// requested cell grid and blocks in the event loop until the window closes or
/// <see cref="Stop"/> is called (thread-safe).</para>
/// <para><b>Attached mode</b> — <see cref="Attach"/> targets a window and renderer you
/// already own. Feed events through <see cref="HandleEvent"/> from your own loop and call
/// <see cref="RenderFrame"/> when <see cref="NeedsRender"/> (or on every frame of a game
/// loop); pass <c>present: false</c> to composite the TUI under your own drawing before
/// you present.</para>
/// <para>Content is provided via <see cref="SetContent"/>: an existing <c>TuiWindow</c>,
/// inline XAML markup or a path to a XAML file, in that precedence order. Load and render
/// errors are drawn into the surface instead of throwing, matching the other hosts.</para>
/// </remarks>
public sealed class TuiSdl2Host : IDisposable
{
    private readonly TuiSkiaHost _skia;
    private nint _window;
    private nint _renderer;
    private nint _texture;
    private int _textureWidth, _textureHeight;
    private bool _ownsSdl;
    private volatile bool _running;
    private uint _wakeEventType;
    private int _renderPending = 1;

    /// <param name="fontFamily">
    /// Optional preferred monospace font family (comma-separated fallback list allowed);
    /// falls through common platform monospace fonts when unavailable.
    /// </param>
    /// <param name="fontSize">Cell font size in pixels (default 16).</param>
    public TuiSdl2Host(string? fontFamily = null, float fontSize = 16f)
    {
        _skia = new TuiSkiaHost(fontFamily, fontSize);
        _skia.RenderRequested += OnRenderRequested;
    }

    /// <summary>
    /// Raised (possibly from a non-UI thread) whenever the hosted TUI needs a repaint.
    /// <see cref="Run"/> handles this itself; attached embedders can use it — or poll
    /// <see cref="NeedsRender"/> — to schedule a <see cref="RenderFrame"/>.
    /// </summary>
    public event Action? RenderRequested;

    /// <summary>The underlying Skia host doing the painting (also useful for headless screenshots).</summary>
    public TuiSkiaHost Skia => _skia;

    /// <summary>The window currently being hosted (explicit, loaded, or implicit).</summary>
    public TuiWindow Window => _skia.Window;

    /// <summary>Set when resolving the window content failed; also drawn into the surface.</summary>
    public string? LoadError => _skia.LoadError;

    /// <summary>Color painted behind and around the cell grid (default black).</summary>
    public SKColor Background { get => _skia.Background; set => _skia.Background = value; }

    /// <summary>Preferred monospace font family currently in use.</summary>
    public string? FontFamily => _skia.FontFamily;

    /// <summary>Cell font size in pixels.</summary>
    public float FontSize => _skia.FontSize;

    /// <summary>Grid size in cells of the most recently rendered frame.</summary>
    public int Columns => _skia.Columns;
    public int Rows => _skia.Rows;

    /// <summary>The SDL_Window* being rendered into (zero until <see cref="Run"/> or <see cref="Attach"/>).</summary>
    public nint WindowHandle => _window;

    /// <summary>The SDL_Renderer* being rendered with (zero until <see cref="Run"/> or <see cref="Attach"/>).</summary>
    public nint RendererHandle => _renderer;

    /// <summary>True while the TUI has an unrendered invalidation pending.</summary>
    public bool NeedsRender => Volatile.Read(ref _renderPending) != 0;

    /// <summary>
    /// Replaces the hosted content: an existing <paramref name="window"/> (highest
    /// precedence), inline <paramref name="xaml"/> markup, or a path to a XAML file in
    /// <paramref name="source"/>. <paramref name="controller"/> is the event/<c>x:Name</c>
    /// binding target for loaded markup.
    /// </summary>
    public void SetContent(TuiWindow? window = null, string? xaml = null, string? source = null, object? controller = null) =>
        _skia.SetContent(window, xaml, source, controller);

    /// <summary>Changes the font, taking effect on the next rendered frame.</summary>
    public void SetFont(string? fontFamily, float fontSize) => _skia.SetFont(fontFamily, fontSize);

    private void OnRenderRequested()
    {
        Interlocked.Exchange(ref _renderPending, 1);
        RenderRequested?.Invoke();

        // Wake a blocked SDL_WaitEvent so background invalidations repaint promptly.
        if (_running && _wakeEventType != 0)
        {
            var ev = new SDL.SDL_Event { type = (SDL.SDL_EventType)_wakeEventType };
            SDL.SDL_PushEvent(ref ev);
        }
    }

    // ---------------------------------------------------------------- owned mode

    /// <summary>
    /// Initializes SDL video, opens a resizable window sized to <paramref name="columns"/> ×
    /// <paramref name="rows"/> cells and runs the event loop until the window closes or
    /// <see cref="Stop"/> is called. Blocks the calling thread; call it from your main thread
    /// (macOS requires SDL event handling on the main thread).
    /// </summary>
    public void Run(string title = "Tedd.TUI", int columns = 80, int rows = 25)
    {
        if (_window != 0)
            throw new InvalidOperationException("Host is already attached to an SDL window.");
        if (columns < 1) throw new ArgumentOutOfRangeException(nameof(columns));
        if (rows < 1) throw new ArgumentOutOfRangeException(nameof(rows));

        if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) != 0)
            throw new InvalidOperationException($"SDL_Init failed: {SDL.SDL_GetError()}");
        _ownsSdl = true;

        try
        {
            var (width, height) = _skia.SizeForCells(columns, rows);
            _window = SDL.SDL_CreateWindow(title,
                SDL.SDL_WINDOWPOS_CENTERED, SDL.SDL_WINDOWPOS_CENTERED,
                (int)MathF.Ceiling(width), (int)MathF.Ceiling(height),
                SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE |
                SDL.SDL_WindowFlags.SDL_WINDOW_ALLOW_HIGHDPI |
                SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);
            if (_window == 0)
                throw new InvalidOperationException($"SDL_CreateWindow failed: {SDL.SDL_GetError()}");

            _renderer = SDL.SDL_CreateRenderer(_window, -1,
                SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED |
                SDL.SDL_RendererFlags.SDL_RENDERER_PRESENTVSYNC);
            if (_renderer == 0)
                _renderer = SDL.SDL_CreateRenderer(_window, -1, SDL.SDL_RendererFlags.SDL_RENDERER_SOFTWARE);
            if (_renderer == 0)
                throw new InvalidOperationException($"SDL_CreateRenderer failed: {SDL.SDL_GetError()}");

            _wakeEventType = SDL.SDL_RegisterEvents(1);
            SDL.SDL_StartTextInput();
            Clipboard.RegisterProvider(new Sdl2Clipboard());

            _running = true;
            RenderFrame();

            while (_running)
            {
                if (SDL.SDL_WaitEvent(out var ev) == 0)
                    continue;
                HandleEvent(in ev);
                while (_running && SDL.SDL_PollEvent(out ev) == 1)
                    HandleEvent(in ev);
                if (_running && Interlocked.Exchange(ref _renderPending, 0) != 0)
                    RenderFrame();
            }
        }
        finally
        {
            ShutdownOwned();
        }
    }

    /// <summary>Requests the <see cref="Run"/> loop to exit. Safe to call from any thread.</summary>
    public void Stop()
    {
        _running = false;
        if (_wakeEventType != 0)
        {
            var ev = new SDL.SDL_Event { type = (SDL.SDL_EventType)_wakeEventType };
            SDL.SDL_PushEvent(ref ev);
        }
    }

    // ---------------------------------------------------------------- attached mode

    /// <summary>
    /// Targets an SDL window and renderer you already own (SDL must be initialized). Pump
    /// events to <see cref="HandleEvent"/> and call <see cref="RenderFrame"/> yourself;
    /// the host never initializes, presents to, or destroys what it did not create.
    /// Enables SDL text input so printable keys arrive as SDL_TEXTINPUT.
    /// </summary>
    public void Attach(nint window, nint renderer)
    {
        if (window == 0) throw new ArgumentException("Window handle is zero.", nameof(window));
        if (renderer == 0) throw new ArgumentException("Renderer handle is zero.", nameof(renderer));
        if (_window != 0)
            throw new InvalidOperationException("Host is already attached to an SDL window.");

        _window = window;
        _renderer = renderer;
        _ownsSdl = false;
        SDL.SDL_StartTextInput();
        Clipboard.RegisterProvider(new Sdl2Clipboard());
        Interlocked.Exchange(ref _renderPending, 1);
    }

    // ---------------------------------------------------------------- rendering

    /// <summary>
    /// Renders one TUI frame into the SDL renderer: paints through Skia into a streaming
    /// texture sized to the renderer output, copies it to the render target and (by
    /// default) presents. Clears <see cref="NeedsRender"/>.
    /// </summary>
    /// <param name="present">Pass false to composite more on top before presenting yourself.</param>
    public void RenderFrame(bool present = true)
    {
        if (_renderer == 0)
            throw new InvalidOperationException("No SDL renderer; call Run or Attach first.");

        Interlocked.Exchange(ref _renderPending, 0);

        if (SDL.SDL_GetRendererOutputSize(_renderer, out int pixelWidth, out int pixelHeight) != 0 ||
            pixelWidth < 1 || pixelHeight < 1)
            return;

        EnsureTexture(pixelWidth, pixelHeight);

        if (SDL.SDL_LockTexture(_texture, 0, out nint pixels, out int pitch) != 0)
            throw new InvalidOperationException($"SDL_LockTexture failed: {SDL.SDL_GetError()}");
        try
        {
            var info = new SKImageInfo(pixelWidth, pixelHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
            using var surface = SKSurface.Create(info, pixels, pitch);
            if (surface == null)
                return;
            _skia.Render(surface.Canvas, pixelWidth, pixelHeight);
            surface.Canvas.Flush();
        }
        finally
        {
            SDL.SDL_UnlockTexture(_texture);
        }

        SDL.SDL_RenderCopy(_renderer, _texture, 0, 0);
        if (present)
            SDL.SDL_RenderPresent(_renderer);
    }

    private void EnsureTexture(int width, int height)
    {
        if (_texture != 0 && _textureWidth == width && _textureHeight == height)
            return;
        if (_texture != 0)
            SDL.SDL_DestroyTexture(_texture);

        // BGRA bytes from Skia are SDL's little-endian ARGB8888 packed format.
        _texture = SDL.SDL_CreateTexture(_renderer, SDL.SDL_PIXELFORMAT_ARGB8888,
            (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING, width, height);
        if (_texture == 0)
            throw new InvalidOperationException($"SDL_CreateTexture failed: {SDL.SDL_GetError()}");
        _textureWidth = width;
        _textureHeight = height;
    }

    // ---------------------------------------------------------------- events

    /// <summary>
    /// Translates one SDL event into TUI input / host actions. Returns true when the event
    /// was consumed (quit/close, window invalidation, keyboard, text input, left-button mouse).
    /// </summary>
    public bool HandleEvent(in SDL.SDL_Event ev)
    {
        switch (ev.type)
        {
            case SDL.SDL_EventType.SDL_QUIT:
                _running = false;
                return true;

            case SDL.SDL_EventType.SDL_WINDOWEVENT:
                switch (ev.window.windowEvent)
                {
                    case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE:
                        _running = false;
                        return true;
                    case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_SIZE_CHANGED:
                    case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_EXPOSED:
                        Interlocked.Exchange(ref _renderPending, 1);
                        return true;
                }
                return false;

            case SDL.SDL_EventType.SDL_KEYDOWN:
                OnKeyDown(in ev.key);
                return true;

            case SDL.SDL_EventType.SDL_TEXTINPUT:
                OnTextInput(in ev.text);
                return true;

            case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                if (ev.button.button != SDL.SDL_BUTTON_LEFT)
                    return false;
                {
                    var (px, py) = ScaleMouse(ev.button.x, ev.button.y);
                    _skia.MouseDown(px, py);
                }
                return true;

            case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                if (ev.button.button != SDL.SDL_BUTTON_LEFT)
                    return false;
                {
                    var (px, py) = ScaleMouse(ev.button.x, ev.button.y);
                    _skia.MouseUp(px, py);
                }
                return true;

            case SDL.SDL_EventType.SDL_MOUSEMOTION:
                {
                    var (px, py) = ScaleMouse(ev.motion.x, ev.motion.y);
                    _skia.MouseMove(px, py);
                }
                return true;

            default:
                return _wakeEventType != 0 && ev.type == (SDL.SDL_EventType)_wakeEventType;
        }
    }

    private void OnKeyDown(in SDL.SDL_KeyboardEvent e)
    {
        var modifiers = Sdl2KeyMapper.MapModifiers(e.keysym.mod);

        ConsoleKey? mapped;
        if (Sdl2KeyMapper.IsControlKey(e.keysym.sym))
        {
            mapped = Sdl2KeyMapper.Map(e.keysym.sym);
        }
        else if ((modifiers & (ConsoleModifiers.Control | ConsoleModifiers.Alt)) != 0)
        {
            // Ctrl/Alt chords never produce usable SDL_TEXTINPUT; deliver them from KEYDOWN.
            mapped = Sdl2KeyMapper.Map(e.keysym.sym);
        }
        else
        {
            // Plain printable keys arrive via SDL_TEXTINPUT with the correctly translated character.
            return;
        }

        if (mapped == null)
            return;

        _skia.ProcessKey(mapped.Value, '\0', modifiers);
    }

    private unsafe void OnTextInput(in SDL.SDL_TextInputEvent e)
    {
        string? text;
        var copy = e;
        text = Marshal.PtrToStringUTF8((nint)copy.text);
        if (string.IsNullOrEmpty(text))
            return;

        var modifiers = Sdl2KeyMapper.MapModifiers(SDL.SDL_GetModState());
        foreach (var c in text)
        {
            // Control characters (Enter, Tab, Backspace, …) were already delivered
            // through KEYDOWN; only genuine printable input flows through here.
            if (c < ' ' || c == '\x7f')
                continue;
            _skia.ProcessKey(Sdl2KeyMapper.MapChar(c), c, modifiers);
        }
    }

    /// <summary>
    /// SDL mouse coordinates are in window points; the renderer output may be larger on
    /// high-DPI displays. Scales to the pixel space the frame was rendered in.
    /// </summary>
    private (float X, float Y) ScaleMouse(int x, int y)
    {
        if (_window == 0 || _renderer == 0)
            return (x, y);
        SDL.SDL_GetWindowSize(_window, out int windowWidth, out int windowHeight);
        SDL.SDL_GetRendererOutputSize(_renderer, out int outputWidth, out int outputHeight);
        float scaleX = windowWidth > 0 ? (float)outputWidth / windowWidth : 1f;
        float scaleY = windowHeight > 0 ? (float)outputHeight / windowHeight : 1f;
        return (x * scaleX, y * scaleY);
    }

    // ---------------------------------------------------------------- teardown

    private void ShutdownOwned()
    {
        if (_texture != 0)
        {
            SDL.SDL_DestroyTexture(_texture);
            _texture = 0;
            _textureWidth = _textureHeight = 0;
        }

        if (_ownsSdl)
        {
            SDL.SDL_StopTextInput();
            if (_renderer != 0) SDL.SDL_DestroyRenderer(_renderer);
            if (_window != 0) SDL.SDL_DestroyWindow(_window);
            SDL.SDL_Quit();
            _ownsSdl = false;
        }

        _renderer = 0;
        _window = 0;
        _wakeEventType = 0;
        _running = false;
    }

    public void Dispose()
    {
        ShutdownOwned();
        _skia.Dispose();
    }
}
