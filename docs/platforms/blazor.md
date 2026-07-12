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
