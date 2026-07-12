# .NET MAUI Host

`Tedd.TUI.Platform.Maui` provides `TuiHostView`, an `SKCanvasView`-based MAUI view
hosting a `TuiWindow` on Android, iOS, Mac Catalyst and Windows. Frames render through
the shared `Tedd.TUI.Surface.Skia` cell painter, so the output matches every other
Tedd.TUI host.

## Setup

Register the SkiaSharp handlers in `MauiProgram.cs`:

```csharp
using Tedd.TUI.Platform.Maui;

builder
    .UseMauiApp<App>()
    .UseTeddTui();          // wraps UseSkiaSharp()
```

## Usage

```xml
<ContentPage …
             xmlns:tui="clr-namespace:Tedd.TUI.Platform.Maui;assembly=Tedd.TUI.Platform.Maui">
    <tui:TuiHostView x:Name="TuiHost"
                     Source="app.xaml"
                     FontFamily="Cascadia Mono"
                     FontSize="16" />
</ContentPage>
```

```csharp
TuiHost.Controller = new AppController();
// or: TuiHost.Window = myTuiWindow;
// runtime access: TuiHost.HostedWindow
```

## Properties

| Property | Meaning |
|---|---|
| `Window` | Host an existing `TuiWindow` (highest precedence; hides `VisualElement.Window`, which stays reachable through the base class) |
| `Xaml` | Inline TUI XAML markup |
| `Source` | Path to a TUI XAML file (absolute, else probed against the current and app base directories) |
| `Controller` | Event/`x:Name` binding target for loaded markup |
| `FontFamily` | Preferred monospace family, comma-separated fallbacks allowed |
| `FontSize` | Cell font size in device-independent units (default 16) |
| `HostedWindow` | The active `TuiWindow` (read-only) |
| `Columns` / `Rows` | Current grid size in cells (read-only) |

## Input

- **Touch / mouse**: press, move and release map to TUI mouse events in cell coordinates
  — tapping a button clicks it, dragging a slider drags it.
- **Keyboard**: MAUI exposes no cross-platform hardware-keyboard events, so keyboard
  input is *injected*:

```csharp
TuiHost.SendKey(ConsoleKey.Tab);                 // focus next
TuiHost.SendKey(ConsoleKey.Enter);               // activate
TuiHost.SendText("Hello");                       // type text
```

Wire these from whatever input source fits your app: a hidden `Entry` for soft-keyboard
text, platform key handlers (e.g. WinUI `KeyDown` on Windows, `UIKeyCommand` on iPadOS),
or on-screen buttons.

## Rendering & DPI

The Skia canvas paints at device pixels and the cell font scales with the canvas/DIU
ratio, so glyphs stay crisp at any density. Bitmap graphics (`VirtualBuffer.Graphics`)
composite over the grid. Repaints are driven by `TuiWindow.VisualChanged` and dispatched
to the UI thread. Load/render errors are drawn into the surface instead of throwing.
