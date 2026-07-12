# Avalonia Host

`Tedd.TUI.Platform.Avalonia` provides `TuiHostControl`, an Avalonia control that hosts a
`TuiWindow` on Windows, macOS and Linux. Frames render through the shared
`Tedd.TUI.Surface.Skia` cell painter into a writeable bitmap, so the output matches every
other Tedd.TUI host exactly.

![Skia-rendered output](../assets/host-skia.png)

## Usage

```xml
<Window …
        xmlns:tui="clr-namespace:Tedd.TUI.Platform.Avalonia;assembly=Tedd.TUI.Platform.Avalonia">
    <tui:TuiHostControl Name="TuiHost"
                        Source="app.xaml"
                        FontFamily="Cascadia Mono"
                        FontSize="16" />
</Window>
```

```csharp
TuiHost.Controller = new AppController();
// or: TuiHost.Window = myTuiWindow;
// runtime access: TuiHost.HostedWindow
```

## Properties

| Property | Meaning |
|---|---|
| `Window` | Host an existing `TuiWindow` (highest precedence) |
| `Xaml` | Inline TUI XAML markup |
| `Source` | Path to a TUI XAML file (absolute, else probed against the current and app base directories) |
| `Controller` | Event/`x:Name` binding target for loaded markup |
| `FontFamily` | Preferred monospace family, comma-separated fallbacks allowed; falls through common platform monospace fonts |
| `FontSize` | Cell font size in logical pixels (default 16) |
| `HostedWindow` | The active `TuiWindow` (read-only) |
| `Columns` / `Rows` | Current grid size in cells (read-only) |

## Input mapping

- **Pointer**: left-button press/release/move → TUI mouse events in cell coordinates,
  with pointer capture during drags and cell-change gating on moves. The control takes
  focus on click.
- **Keyboard**: navigation/editing/function keys and Ctrl/Alt chords map in `KeyDown`;
  printable characters arrive via Avalonia text input.

## Rendering & DPI

The control renders at the window's `RenderScaling`, creating the Skia surface at device
pixels for crisp glyphs on HiDPI displays. Bitmap graphics (`VirtualBuffer.Graphics`)
composite over the grid. Repaints are driven by `TuiWindow.VisualChanged`, coalesced onto
the UI thread. Load/render errors are drawn into the surface instead of throwing.

Because the painter uses its own SkiaSharp reference (not Avalonia's internal one), the
package stays independent of Avalonia's Skia version.
