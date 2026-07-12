# WinUI 3 Host

`Tedd.TUI.Platform.WinUI` provides `TuiHostControl`, a WinUI 3 (`Microsoft.WindowsAppSDK`)
control hosting a `TuiWindow`. Frames render through the shared `Tedd.TUI.Surface.Skia`
cell painter onto an `SKXamlCanvas`, so the output matches every other Tedd.TUI host.

## Usage

```xml
<Window …
        xmlns:tui="using:Tedd.TUI.Platform.WinUI">
    <tui:TuiHostControl x:Name="TuiHost"
                        Source="app.xaml"
                        MonoFontFamily="Cascadia Mono"
                        MonoFontSize="16" />
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
| `MonoFontFamily` | Preferred monospace family, comma-separated fallbacks allowed (named `Mono*` because WinUI's `Control.FontFamily` already exists) |
| `MonoFontSize` | Cell font size in logical pixels (default 16) |
| `HostedWindow` | The active `TuiWindow` (read-only) |
| `Columns` / `Rows` | Current grid size in cells (read-only) |

## Input mapping

- **Pointer**: left-button press/release/move → TUI mouse events in cell coordinates,
  with pointer capture during drags and cell-change gating on moves. The control takes
  focus on click (`IsTabStop = true`).
- **Keyboard**: navigation/editing/function keys and Ctrl/Alt chords map in `KeyDown`
  (`VirtualKey`); printable characters arrive via `CharacterReceived`.

## Rendering & DPI

The Skia canvas paints at device pixels; the cell font scales with
`XamlRoot.RasterizationScale`, so glyphs stay crisp on HiDPI displays. Bitmap graphics
(`VirtualBuffer.Graphics`) composite over the grid. Repaints are driven by
`TuiWindow.VisualChanged` and coalesced through the `DispatcherQueue`. Load/render errors
are drawn into the surface instead of throwing.

Note: WinUI 3 has no visual XAML designer in Visual Studio — for a design-time preview
workflow, edit the same TUI XAML file hosted in a WPF window with
[`TuiHostElement`](wpf.md); the file runs unchanged in WinUI.
