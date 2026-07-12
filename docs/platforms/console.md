# Console Host

The original habitat. `Tedd.TUI.Platform.Console` hosts a `TuiWindow` in a terminal via
`TuiApp`; the companion backend packages provide truecolor rendering, mouse support and
inline-image protocols.

## Packages

| Package | Role |
|---|---|
| `Tedd.TUI.Platform.Console` | `TuiApp` run loop + auto-detecting `PlatformLoader`; legacy 16-color fallback renderer |
| `Tedd.TUI.Platform.WindowsTerminal` | Windows backend: VT truecolor, raw input, mouse, Sixel inline images (Windows Terminal 1.22+) |
| `Tedd.TUI.Platform.LinuxTerminal` | Linux/macOS backend: VT truecolor, raw input, mouse, Sixel/Kitty/iTerm2 inline images |
| `Tedd.TUI.Imaging` | Optional bitmap decoding (Magick.NET) for image-aware controls |

Reference `Platform.Console` plus the backend(s) for your target OS. `PlatformLoader`
picks the best available backend at startup and falls back to the legacy 16-color
renderer when no truecolor backend is referenced.

## Usage

```csharp
using Tedd.TUI;
using Tedd.TUI.Platform.Console;

var controller = new AppController();
var window = (TuiWindow)XamlLoader.Load(File.ReadAllText("app.xaml"), controller);

var app = new TuiApp(window);   // or: new TuiApp(window, explicitPlatform)
app.Run();                      // blocks until app.Stop()
```

- `app.Run()` owns the UI thread: layout, rendering and input dispatch all run there.
- `app.Stop()` is thread-safe and idempotent (safe from `Console.CancelKeyPress`).
- `app.Capabilities` exposes what the active backend supports (truecolor, images, cell
  pixel size).

## Rich content

Markdown and syntax-highlighted code need nothing extra — `MarkdownView` and
`CodeDocument` live in the core package and render styled cells on any backend
(see the [feature tour](../README.md#rich-content-in-the-console)).

### Inline images

Reference `Tedd.TUI.Imaging` and register the decoder once at startup:

```csharp
using Tedd.TUI.Imaging;

TuiImaging.RegisterDefaults();          // Magick.NET decoder + file/HTTP resolvers
```

`Image` controls — and markdown `![alt](source)` references — then render real bitmaps
using the best protocol the active terminal supports, auto-detected at startup:

| Protocol | Terminals |
|---|---|
| Sixel | Windows Terminal 1.22+, xterm/mlterm and other VT340-style terminals |
| Kitty graphics | Kitty |
| iTerm2 inline images | iTerm2, WezTerm, Ghostty |
| Half-block fallback | everything else — truecolor `▀` cells, two pixels per cell |

The half-block fallback needs no protocol support at all, so images degrade gracefully
on plain terminals and over SSH.

## Behavior notes

- The run loop is event-driven: it sleeps until input arrives or a property change
  invalidates the visuals (`TuiWindow.VisualChanged`), then re-measures, re-arranges and
  re-renders with double-buffered diffing — only changed cells are written to the
  terminal.
- Terminal resize triggers a full re-layout.
- Mouse events arrive in cell coordinates; keyboard as `ConsoleKey` + char + modifiers.
- On exit the console state (cursor, alt screen, mouse tracking, input modes) is restored.
