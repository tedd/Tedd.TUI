# SDL2 Host

`Tedd.TUI.Platform.Sdl2` provides `TuiSdl2Host`, which puts a TUI in a native **SDL2**
window on Windows, Linux and macOS — no .NET GUI framework involved. Frames paint through
the standalone [Skia host](skia.md) into an SDL streaming texture, so the output matches
every other Tedd.TUI host exactly; SDL keyboard, text-input and mouse events feed the TUI
input pipeline.

Use it when SDL is your windowing layer:

- **A window in one call** — `Run()` opens an SDL2 window and drives the whole
  event/render loop; ship a desktop TUI app with no framework dependency beyond SDL.
- **Game engines & SDL apps** — attach to the SDL window + renderer you already own and
  composite the TUI into your existing loop (HUDs, in-game consoles, debug panels).
- **Kiosk / embedded** — SDL2 runs on KMS/DRM and framebuffer targets where no desktop
  environment exists.

Native SDL2 binaries ship via the referenced `ppy.SDL2-CS` binding package.

## Owned window (one call)

```csharp
using Tedd.TUI.Platform.Sdl2;

using var host = new TuiSdl2Host(fontFamily: "Cascadia Mono", fontSize: 16f);
host.SetContent(source: "app.xaml", controller: new AppController());
// or: host.SetContent(window: myTuiWindow);
// or: host.SetContent(xaml: "<TuiWindow>…</TuiWindow>");

host.Run(title: "My App", columns: 80, rows: 25);   // blocks until the window closes
```

`Run` initializes SDL video, opens a resizable HiDPI-aware window sized to the cell grid
and blocks in the event loop; call `Stop()` (thread-safe) to exit it. Call `Run` from the
main thread — macOS requires SDL event handling there.

## Attached to your own SDL loop

```csharp
host.Attach(window, renderer);          // SDL_Window* / SDL_Renderer* you created

while (running)
{
    while (SDL.SDL_PollEvent(out var ev) == 1)
    {
        if (!host.HandleEvent(in ev))
            HandleMyOwnEvent(in ev);    // events the TUI didn't consume
    }

    if (host.NeedsRender)               // or just render every frame of a game loop
        host.RenderFrame(present: false);
    DrawMyOverlay(renderer);
    SDL.SDL_RenderPresent(renderer);
}
```

The host never initializes, presents to, or destroys what it did not create.

## API

| Member | Meaning |
|---|---|
| `SetContent(window, xaml, source, controller)` | Content: existing `TuiWindow` (highest precedence), inline markup, or XAML file path; `controller` is the event/`x:Name` binding target |
| `Run(title, columns, rows)` / `Stop()` | Owned mode: open an SDL window and block in the event loop / request exit (thread-safe) |
| `Attach(window, renderer)` | Attached mode: target an SDL window + renderer you own |
| `HandleEvent(in ev)` | Translate one `SDL_Event` (returns true when consumed: quit/close, resize, keyboard, text input, left-button mouse) |
| `RenderFrame(present)` | Paint one frame into a streaming texture and copy it to the renderer; `present: false` lets you composite on top first |
| `NeedsRender` / `RenderRequested` | Pending-invalidation flag / repaint signal (coalesced, may fire off-thread) |
| `Skia` | The underlying `TuiSkiaHost` (also handy for headless PNG screenshots of the same content) |
| `Background` | `SKColor` painted behind/around the grid (default black) |
| `FontFamily` / `FontSize` / `SetFont(family, size)` | Monospace font (comma-separated fallbacks allowed) |
| `Window` / `LoadError` | The hosted `TuiWindow`; markup load failure (also drawn into the surface) |
| `Columns` / `Rows` | Grid size of the last rendered frame |
| `WindowHandle` / `RendererHandle` | The `SDL_Window*` / `SDL_Renderer*` in use |

## Notes

- Input follows the same split as the other GUI hosts: navigation/editing/function keys
  and Ctrl/Alt chords are delivered from `SDL_KEYDOWN`, printable characters from
  `SDL_TEXTINPUT` (`Sdl2KeyMapper` is public if you need the tables). Mouse coordinates
  are HiDPI-scaled from window points to renderer output pixels.
- Load and render errors are drawn into the surface instead of throwing, matching the
  other hosts.
- `Run` prefers an accelerated vsynced renderer and falls back to SDL's software renderer
  automatically (the host works headless under SDL's dummy video driver too).
- For hosting inside Avalonia, WinUI or MAUI use their dedicated host packages; if you
  only need an `SKCanvas` or PNG output with no window at all, use
  [`Tedd.TUI.Platform.Skia`](skia.md) directly.
