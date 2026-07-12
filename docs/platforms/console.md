# Console Host

The original habitat. `Tedd.TUI.Platform.Console` hosts a `TuiWindow` in a terminal via
`TuiApp`; the companion backend packages provide truecolor rendering, mouse support and
inline-image protocols.

## Packages

| Package | Role |
|---|---|
| `Tedd.TUI.Platform.Console` | `TuiApp` run loop + auto-detecting `PlatformLoader`; legacy 16-color fallback renderer |
| `Tedd.TUI.Platform.WindowsTerminal` | Windows backend: VT truecolor, raw input, mouse |
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

## Behavior notes

- The run loop is event-driven: it sleeps until input arrives or a property change
  invalidates the visuals (`TuiWindow.VisualChanged`), then re-measures, re-arranges and
  re-renders with double-buffered diffing — only changed cells are written to the
  terminal.
- Terminal resize triggers a full re-layout.
- Mouse events arrive in cell coordinates; keyboard as `ConsoleKey` + char + modifiers.
- On exit the console state (cursor, alt screen, mouse tracking, input modes) is restored.
