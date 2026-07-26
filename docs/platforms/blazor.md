# Blazor Host

`Tedd.TUI.Platform.Blazor` renders TUIs in the browser — Blazor WebAssembly, Blazor
Server, and MAUI Blazor hybrid all work. You can author the UI three ways: XAML
(`TuiXamlView`), Razor components (`TuiView` + `Tui*` wrappers), or a prebuilt
`TuiWindow`.

## Surfaces: Canvas vs DOM

Both `TuiView` and `TuiXamlView` take a `Mode` parameter (`TuiRenderMode`):

| Mode | Surface | Traits |
|---|---|---|
| `Canvas` (default) | `<canvas>` | Fastest; pixel-stable grid |
| `Dom` | Styled DOM grid | Selectable text, DevTools-inspectable, bitmap graphics via positioned `<img>` |

The surface measures real character metrics from the browser and reports them through
`SurfaceCapabilities`, so image-aware controls size correctly.

## XAML: `TuiXamlView`

```razor
@using Tedd.TUI.Platform.Blazor
@using Tedd.TUI.Platform.Blazor.Components

<TuiXamlView Source="tui/app.xaml" Controller="@_controller"
             Width="80" Height="25" Mode="TuiRenderMode.Canvas" />
```

Parameters:

- `Source` — file path, absolute URL, or app-base-relative path (e.g. a static asset
  under `wwwroot`). Resolution order: physical file → app base directory → HTTP via the
  registered `HttpClient` (relative paths combine with its `BaseAddress`, falling back to
  the `NavigationManager` base URI).
- `Xaml` — inline markup; takes precedence over `Source`.
- `Controller` — event/`x:Name` binding target (see the [XAML guide](../xaml.md)).
- `Width`/`Height`/`Mode` — surface configuration (standalone mode).

Standalone vs nested:

- **Standalone** (shown above): the component creates its own `TuiView` surface.
- **Nested** inside an existing `<TuiView>`: the loaded element becomes content of the
  surrounding view's window; a `TuiWindow` root is unwrapped automatically.

Blazor WASM tip: `Source` fetches over HTTP, so keep the default template's
`builder.Services.AddScoped(sp => new HttpClient { BaseAddress = … })` registration.

## Razor components: `TuiView` + wrappers

```razor
<TuiView Width="80" Height="25" Mode="TuiRenderMode.Dom">
    <TuiStackPanel>
        <TuiLabel Text="Hello!" />
        <TuiButton Text="Submit" OnClick="OnSubmit" />
    </TuiStackPanel>
</TuiView>
```

Wrappers exist for the common controls (`TuiButton`, `TuiTextBox`, `TuiListBox`,
`TuiTable`, `TuiTabControl`, …). Raw `UIElement`s without a wrapper embed via
`<TuiHost Component="@element" />`.

## Programmatic

```razor
<TuiView Width="80" Height="25" Window="@_window" />
```

## Input & resize

Keyboard and mouse events are captured on the surface `<div>` (it takes focus on click),
translated to cell coordinates and queued into the TUI event loop. Browser resize
callbacks recompute the cell grid and re-render.

### Mouse wheel

Wheel events are forwarded as `MouseWheelEventArgs`, so `ScrollViewer`, `ScrollBar`,
`ListBox` and anything else that handles `OnMouseWheel` scroll under the pointer with no
extra wiring:

```razor
<TuiView Width="100" Height="34" Mode="TuiRenderMode.Dom">
    <TuiHost Component="@_scroller" />
</TuiView>

@code {
    private readonly ScrollViewer _scroller = new()
    {
        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
        Content = BuildLongContent()
    };
}
```

Browser `deltaY` values are normalized to the same convention every other host uses —
±`MouseWheelEventArgs.WheelNotch` (120) per physical notch, positive when scrolling away
from the user. Each `deltaMode` is scaled by what one notch means in that unit (Chrome
reports pixels, Firefox commonly reports lines), and fractional results are preserved so
trackpads accumulate into smooth scrolling instead of being truncated to zero. Set how far
one notch scrolls with `ScrollViewer.WheelScrollLines` (default 3).

### Character metrics must match the renderer

The host measures real character metrics from the browser and derives the column/row count
from them. If JavaScript and the renderer disagree about cell size, the grid that gets
drawn is not the grid that was requested: too few columns leaves dead space on the right,
and too many pushes the right-hand scrollbar column outside the viewport, where it can be
neither seen nor clicked. `TuiView` therefore passes the renderer's resolved metrics into
`tuiInterop.listenForResize`, and `measureDom` caches what it measured. If you call
`listenForResize` yourself, pass the same cell size your renderer draws with:

```js
tuiInterop.listenForResize(dotNetRef, canvasId, cellWidth, cellHeight);
```

## DOM mode performance

`TuiRenderMode.Dom` rebuilds a grid of styled `<span>` runs per frame, so cost scales with
grid *area* — a large window plus interpreted (Debug) WASM is the worst case. Two things
keep it in hand:

- **Row-level caching.** `TuiDomGrid` hands Blazor the same markup string for a row whose
  cells did not change, so the diff skips it and only genuinely changed rows are patched
  into the DOM.
- **Coalesced drag input.** Pointer moves during a drag are throttled to one call per
  animation frame. Forwarding every raw mouse event (60–120 Hz) queues renders faster than
  they complete, which pins the main thread; `requestAnimationFrame` also supplies natural
  backpressure, since a busy main thread produces no frames.

For very large grids prefer `TuiRenderMode.Canvas`, which paints in one call instead of
building DOM nodes.
