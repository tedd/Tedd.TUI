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
| `Dom` | Styled DOM grid | Whole scrolled content in the DOM (find-in-page, prerendering), DevTools-inspectable, bitmap graphics via positioned `<img>` |

The surface measures real character metrics from the browser and reports them through
`SurfaceCapabilities`, so image-aware controls size correctly.

## Pre-rendered scroll regions (DOM mode)

In DOM mode a scrollable region emits its **whole** content, not just the rows that fit. The
viewport becomes an `overflow: hidden` box and the content sits inside it in a block that
CSS translates by whole cells, so scrolling moves an already-built subtree instead of
forcing a re-render:

```html
<div class="tui-scroll-pane"   style="… width: 700px; height: 72px; overflow: hidden;">
  <div class="tui-scroll-content" style="… height: 3600px; transform: translate(0px, -126px);">
    <div class="tui-row">…</div>   <!-- every row, not just the visible ones -->
```

Because the translate is a whole number of cells, it lands on exact row boundaries and
reproduces the same line-by-line and page steps the TUI applies in text mode. The TUI
remains the source of truth for the scroll offset — the browser paints, it does not scroll.

What this buys you:

- **Find-in-page and text extraction see everything**, not the current viewport.
- **Scrolling is a transform**, not a round trip through the event loop and a DOM patch.
- **Prerendered HTML carries the full content** (see below).

It applies to `ScrollViewer` and `Border`, and so to everything built on them —
`DialogBox`, `Table`, `TreeView`, `DataGrid`, `MarkdownCodeBlock`, and the `ComboBox` /
`MenuItem` / `DatePicker` popups. `ListBox` and `TextEditor` scroll by re-slicing their own
item list rather than by clipping, so they still emit only the visible rows.

### Cost, and how to opt out

It is on by default in DOM mode, and the cost is real: node count and render time scale with
the **content extent** rather than the viewport area, and pre-rendering deliberately defeats
the culling `Panel.Render` normally applies to off-screen children. A `ScrollViewer` over ten
thousand unpaged rows becomes ten thousand row divs. Non-overflowing content is left alone —
there is nothing to scroll, so it stays on the cheaper clip path.

Turn it off for the whole surface:

```razor
<TuiView Width="100" Height="34" Mode="TuiRenderMode.Dom" PrerenderScrollContent="false" />
```

…or for one viewer, leaving the rest of the surface pre-rendered:

```csharp
ScrollViewer.SetPrerenderContent(_hugeLogViewer, false);
```

For very large tabular data, `Table.PageSize` (which realizes only the current page) is a
better fit than either setting. Canvas mode ignores both — it has no sub-region to clip.

## Prerendering

`TuiView` renders one frame synchronously from `OnInitialized`, so statically rendered and
prerendered output contains real markup rather than an empty container. That pass uses no JS
interop — cell metrics fall back to the renderer's defaults, which affects pixel sizing only,
never the text. The interactive loop then starts from `OnAfterRenderAsync` on the same
renderer instance and takes over. As a side effect there is no longer a blank first paint in
interactive WebAssembly.

Razor-authored children register themselves during their own initialization, which is after
the view first rendered, so `TuiView` refreshes the static frame when content is added and
the loop has not started yet.

`BlazorTuiApp.RenderStaticFrame(width, height)` is the same path if you drive the surface
yourself, and `DomGridMarkup.RenderDocument(...)` turns the resulting layers into the HTML
string a browser would receive — useful for tests and for rendering outside a component.

One caveat worth stating plainly: the emitted HTML is colour-run `<span>`s inside
`<div class="tui-row">`, hard-wrapped by TUI layout and split wherever colours change. The
text is present and extractable, but it carries no headings, links or landmarks — it indexes
as a wall of text. If search ranking is the goal, a parallel semantic block will serve you
much better than a taller cell grid.

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

### The loop must yield to the browser

On WebAssembly the TUI render loop shares the single UI thread with the browser, so it has
to hand that thread back on every pass. `BlazorTuiApp` yields through a timer once per
frame for exactly this reason.

The trap is subtle and worth knowing if you write your own host on this pattern: the loop
waits on a semaphore that each invalidation signals, and `await` on an **already-completed**
task resumes *synchronously* rather than returning to the scheduler. A frame whose own
rendering causes another invalidation therefore finds the wait already satisfied and
continues immediately — forever, without the event loop ever running again. The symptom is
not a slow page but a dead one: no input, no timers, no repaint, and no console output
(which also means such a loop cannot be diagnosed by logging from inside it). With a yield
in place the same situation merely costs frame rate.
