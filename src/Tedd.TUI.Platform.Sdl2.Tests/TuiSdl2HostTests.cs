using System.Text;
using SDL2;
using Tedd.TUI;
using Tedd.TUI.Platform.Sdl2;

namespace Tedd.TUI.Platform.Sdl2.Tests;

/// <summary>
/// Host tests that need no SDL video device: content resolution delegates to the inner
/// Skia host, and <see cref="TuiSdl2Host.HandleEvent"/> translation runs on plain event
/// structs (mouse scaling is a pass-through while unattached).
/// </summary>
public class TuiSdl2HostTests
{
    private static SDL.SDL_Event KeyDown(SDL.SDL_Keycode key, SDL.SDL_Keymod mod = SDL.SDL_Keymod.KMOD_NONE)
    {
        var ev = new SDL.SDL_Event { type = SDL.SDL_EventType.SDL_KEYDOWN };
        ev.key.keysym.sym = key;
        ev.key.keysym.mod = mod;
        return ev;
    }

    private static SDL.SDL_Event MouseButton(SDL.SDL_EventType type, int x, int y, uint button = SDL.SDL_BUTTON_LEFT)
    {
        var ev = new SDL.SDL_Event { type = type };
        ev.button.button = (byte)button;
        ev.button.x = x;
        ev.button.y = y;
        return ev;
    }

    [Fact]
    public void XamlContent_LoadsWindow()
    {
        using var host = new TuiSdl2Host();
        host.SetContent(xaml: "<TuiWindow><TextBlock Text=\"hi\"/></TuiWindow>");

        Assert.NotNull(host.Window);
        Assert.Null(host.LoadError);
    }

    [Fact]
    public void InvalidXaml_SetsLoadError()
    {
        using var host = new TuiSdl2Host();
        host.SetContent(xaml: "<Not-Valid-Xaml<");

        using var _ = host.Skia.RenderToImage(30, 4); // draws the error, must not throw
        Assert.NotNull(host.LoadError);
    }

    [Fact]
    public void RenderRequested_FiresWhenWindowInvalidates()
    {
        using var host = new TuiSdl2Host();
        var text = new TextBlock { Text = "before" };
        host.SetContent(new TuiWindow { Content = text });
        using var _ = host.Skia.RenderToImage(20, 5); // resets the coalescing gate

        bool requested = false;
        host.RenderRequested += () => requested = true;
        text.Text = "after";

        Assert.True(requested);
        Assert.True(host.NeedsRender);
    }

    [Fact]
    public void RenderFrame_WithoutWindow_Throws()
    {
        using var host = new TuiSdl2Host();
        Assert.Throws<InvalidOperationException>(() => host.RenderFrame());
    }

    [Fact]
    public void HandleEvent_Quit_IsConsumed()
    {
        using var host = new TuiSdl2Host();
        var ev = new SDL.SDL_Event { type = SDL.SDL_EventType.SDL_QUIT };
        Assert.True(host.HandleEvent(in ev));
    }

    [Fact]
    public void HandleEvent_WindowSizeChanged_MarksNeedsRender()
    {
        using var host = new TuiSdl2Host();
        host.SetContent(new TuiWindow());
        using var _ = host.Skia.RenderToImage(10, 4);

        var ev = new SDL.SDL_Event { type = SDL.SDL_EventType.SDL_WINDOWEVENT };
        ev.window.windowEvent = SDL.SDL_WindowEventID.SDL_WINDOWEVENT_SIZE_CHANGED;

        Assert.True(host.HandleEvent(in ev));
        Assert.True(host.NeedsRender);
    }

    [Fact]
    public void HandleEvent_MouseClick_RaisesButtonClick()
    {
        using var host = new TuiSdl2Host();
        bool clicked = false;
        var button = new Button { Content = "OK" };
        button.Click += (_, _) => clicked = true;
        host.SetContent(new TuiWindow { Content = button });

        using var _ = host.Skia.RenderToImage(20, 5); // arrange the tree so hit testing works

        // Button sizes to its content and sits left-aligned (not stretched to fill the
        // window), so it occupies only the first few columns — click inside cell (1, 1)
        // rather than the grid's center, which now falls outside it.
        var (cellWidth, cellHeight) = host.Skia.SizeForCells(1, 1);
        int x = (int)(cellWidth * 1.5f), y = (int)(cellHeight * 1.5f);

        Assert.True(host.HandleEvent(MouseButton(SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN, x, y)));
        Assert.True(host.HandleEvent(MouseButton(SDL.SDL_EventType.SDL_MOUSEBUTTONUP, x, y)));

        Assert.True(clicked);
    }

    [Fact]
    public void HandleEvent_NonLeftMouseButton_IsNotConsumed()
    {
        using var host = new TuiSdl2Host();
        host.SetContent(new TuiWindow());
        using var _ = host.Skia.RenderToImage(20, 5);

        var ev = MouseButton(SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN, 5, 5, SDL.SDL_BUTTON_RIGHT);
        Assert.False(host.HandleEvent(in ev));
    }

    [Fact]
    public void HandleEvent_ControlKey_ReachesHostedWindow()
    {
        using var host = new TuiSdl2Host();
        var textBox = new TextBox { Width = 15, Text = "abc" };
        host.SetContent(new TuiWindow { Content = textBox });
        using var _ = host.Skia.RenderToImage(20, 5); // initial focus lands on the TextBox

        Assert.True(host.HandleEvent(KeyDown(SDL.SDL_Keycode.SDLK_END)));
        Assert.True(host.HandleEvent(KeyDown(SDL.SDL_Keycode.SDLK_BACKSPACE)));

        Assert.Equal("ab", textBox.Text);
    }

    [Fact]
    public void HandleEvent_PlainPrintableKeyDown_IsLeftToTextInput()
    {
        using var host = new TuiSdl2Host();
        var textBox = new TextBox { Width = 15 };
        host.SetContent(new TuiWindow { Content = textBox });
        using var _ = host.Skia.RenderToImage(20, 5);

        // Consumed but not delivered: the translated character arrives via SDL_TEXTINPUT.
        Assert.True(host.HandleEvent(KeyDown(SDL.SDL_Keycode.SDLK_a)));
        Assert.Equal("", textBox.Text ?? "");
    }
}

/// <summary>
/// Tests that exercise real SDL through its dummy video driver: attach to a hidden
/// window + software renderer, render frames into the streaming texture and feed text
/// input. Skipped silently when SDL cannot initialize in this environment.
/// </summary>
[Collection("sdl-dummy")]
public class TuiSdl2HostSdlTests
{
    private readonly SdlDummyFixture _sdl;

    public TuiSdl2HostSdlTests(SdlDummyFixture sdl) => _sdl = sdl;

    [Fact]
    public void AttachAndRenderFrame_SizesGridToRendererOutput()
    {
        if (!_sdl.Available) return;

        using var host = new TuiSdl2Host();
        host.SetContent(new TuiWindow { Background = TuiColor.FromRgb(0, 0, 128) });
        var (width, height) = host.Skia.SizeForCells(40, 12);

        if (!_sdl.TryCreateWindowRenderer((int)MathF.Ceiling(width), (int)MathF.Ceiling(height),
                out var window, out var renderer))
            return;
        try
        {
            host.Attach(window, renderer);
            Assert.True(host.NeedsRender);

            host.RenderFrame(present: false);

            Assert.Equal(40, host.Columns);
            Assert.Equal(12, host.Rows);
            Assert.False(host.NeedsRender);
        }
        finally
        {
            SDL.SDL_DestroyRenderer(renderer);
            SDL.SDL_DestroyWindow(window);
        }
    }

    [Fact]
    public unsafe void HandleEvent_TextInput_TypesIntoFocusedTextBox()
    {
        if (!_sdl.Available) return;

        using var host = new TuiSdl2Host();
        var textBox = new TextBox { Width = 15 };
        host.SetContent(new TuiWindow { Content = textBox });
        using var _ = host.Skia.RenderToImage(20, 5); // initial focus lands on the TextBox

        var ev = new SDL.SDL_Event { type = SDL.SDL_EventType.SDL_TEXTINPUT };
        byte[] utf8 = Encoding.UTF8.GetBytes("Hi 5");
        for (int i = 0; i < utf8.Length; i++)
            ev.text.text[i] = utf8[i];
        ev.text.text[utf8.Length] = 0;

        Assert.True(host.HandleEvent(in ev));
        Assert.Equal("Hi 5", textBox.Text);
    }
}

[CollectionDefinition("sdl-dummy")]
public class SdlDummyCollection : ICollectionFixture<SdlDummyFixture>;

/// <summary>Initializes SDL once with the dummy video driver; tests no-op when unavailable.</summary>
public sealed class SdlDummyFixture : IDisposable
{
    public bool Available { get; }

    public SdlDummyFixture()
    {
        Environment.SetEnvironmentVariable("SDL_VIDEODRIVER", "dummy");
        try
        {
            Available = SDL.SDL_Init(SDL.SDL_INIT_VIDEO) == 0;
        }
        catch (DllNotFoundException)
        {
            Available = false;
        }
    }

    public bool TryCreateWindowRenderer(int width, int height, out nint window, out nint renderer)
    {
        renderer = 0;
        window = SDL.SDL_CreateWindow("test", 0, 0, width, height, SDL.SDL_WindowFlags.SDL_WINDOW_HIDDEN);
        if (window == 0)
            return false;

        renderer = SDL.SDL_CreateRenderer(window, -1, SDL.SDL_RendererFlags.SDL_RENDERER_SOFTWARE);
        if (renderer == 0)
        {
            SDL.SDL_DestroyWindow(window);
            window = 0;
            return false;
        }
        return true;
    }

    public void Dispose()
    {
        if (Available)
            SDL.SDL_Quit();
    }
}
