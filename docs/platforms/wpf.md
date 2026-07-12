# WPF Host

`Tedd.TUI.Platform.Wpf` provides `TuiHostElement`, a `FrameworkElement` that runs the
real TUI pipeline inside a WPF visual tree. The TUI lays out into its `VirtualBuffer`
cell grid and the element paints that grid with a monospace typeface — cell-for-cell what
a terminal would show.

![Tedd.TUI inside WPF](../assets/host-wpf.png)

## Usage

```xml
<Window …
        xmlns:tui="clr-namespace:Tedd.TUI.Platform.Wpf;assembly=Tedd.TUI.Platform.Wpf">
    <tui:TuiHostElement x:Name="TuiHost"
                        Source="app.xaml"
                        FontFamily="Cascadia Mono"
                        FontSize="16" />
</Window>
```

```csharp
TuiHost.Controller = new AppController();

// Programmatic content instead of XAML:
TuiHost.Window = myTuiWindow;

// Interact with the hosted window at runtime:
TuiHost.HostedWindow.Content = …;
```

## Properties

| Property | Meaning |
|---|---|
| `Window` | Host an existing `TuiWindow` (highest precedence) |
| `Xaml` | Inline TUI XAML markup |
| `Source` | Path to a TUI XAML file (absolute, else probed against the current and app base directories) |
| `Controller` | Event/`x:Name` binding target for loaded markup |
| `FontFamily` | Monospace font (default `Cascadia Mono, Consolas, Courier New`) |
| `FontSize` | Cell font size (default 16) |
| `Background` | Fill behind/around the grid (default black) |
| `HostedWindow` | The active `TuiWindow` (read-only) |
| `Columns` / `Rows` | Current grid size in cells (read-only) |

The element fills whatever space it is given; the cell grid is
`floor(size / cell size)`. Unconstrained, it asks for a classic 80×25 surface.

## Designer / live-preview workflow

`TuiHostElement` renders in the Visual Studio XAML designer: open the hosting WPF window
in the designer and the element executes the real loader + layout + renderer, so the
preview is exactly what runs. Edit the TUI XAML file, rebuild (or just reopen the
designer view) and the preview updates.

Failures never break the designer — load or render errors are painted into the surface
as text instead of throwing.

## Input mapping

- **Mouse**: left button down/up/move → TUI mouse events in cell coordinates. The element
  takes keyboard focus on click and captures the mouse during drags. Moves are forwarded
  only when the hovered cell changes.
- **Keyboard**: navigation/editing/function keys and Ctrl/Alt chords are mapped in
  `KeyDown`; printable characters arrive through WPF text input with the correctly
  translated character (dead keys and layouts included).

## Rendering & DPI

Cells are drawn as background runs plus foreground text runs via `DrawingContext`; bitmap
graphics (`VirtualBuffer.Graphics`) composite over the grid, with
`SurfaceCapabilities.SupportsGraphics = true` and real cell pixel sizes reported to the
TUI. Per-monitor DPI changes re-measure the cell metrics automatically. Repaints are
driven by `TuiWindow.VisualChanged` and coalesced onto the dispatcher.
