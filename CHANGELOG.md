# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Fixed
- **Blazor: mouse wheel never reached the TUI.** The Blazor host forwarded key and mouse-button events but no wheel events at all, so `ScrollViewer`/`ScrollBar`/`ListBox` could not be scrolled with the wheel in the browser. `TuiView` now handles `@onwheel` and `BlazorInputManager.QueueWheel` normalizes the browser delta to the host-wide convention (±`MouseWheelEventArgs.WheelNotch` per notch, positive away from the user), scaling each `deltaMode` (pixels/lines/pages) by what one notch means in that unit and preserving fractions so trackpads accumulate instead of truncating to zero.
- **Blazor: JavaScript and the renderer could disagree on cell size.** `tuiInterop.listenForResize` derived the column/row count from its own default metrics rather than the ones the renderer draws with, so the drawn grid did not match the requested one — leaving dead space when it guessed too wide, or pushing the right-hand scrollbar column outside the viewport (invisible and unclickable) when it guessed too narrow. `TuiView` now passes the renderer's resolved metrics, and `measureDom` caches what it measured.
- **Blazor DOM mode: full-grid repaint every frame.** `TuiDomGrid` now reuses the previous frame's markup string for rows whose cells did not change, so Blazor's diff skips them and only changed rows are patched into the DOM.
- **Blazor: drag input outran the renderer.** Pointer moves during a drag are coalesced to one call per animation frame; previously every raw mouse event (60–120 Hz) queued a render, so the queue outran the renderer and pinned the main thread during text selection and scrollbar drags.
- **`Tab` navigation could throw on trees with empty visual slots.** `VisualTreeEnumerator` now skips null elements and children, and `CanFocus` null-guards its argument, so focus navigation cannot fail with a `NullReferenceException`.

### Added
- **CodeColoring: 49 new syntax-highlighting grammars** (76 total), ported from Prism.js v2 (MIT, attribution in `THIRD-PARTY-NOTICES.md`): JavaScript, TypeScript, C, C++, Java, Go, Kotlin, Swift, Dart, Ruby, PHP, R, Julia, Groovy, Scala, Objective-C, Visual Basic, F#, Haskell, Elixir, Erlang, Clojure, Lisp, Scheme, OCaml, TOML, INI, GraphQL, Docker, Makefile, Git, nginx, CMake, HCL/Terraform, HTTP (with Content-Type body highlighting), protobuf, LaTeX, Fortran, Pascal, Ada, COBOL, Prolog, Smalltalk, Tcl, Verilog, VHDL, Zig, Nim and Solidity. `CodeDocument` now falls back to a token's Prism alias when the theme lacks its primary type, and `Pattern` maps the JS `s` regex flag to `RegexOptions.Singleline`. Round-trip smoke tests cover every new grammar.
- **`DataGrid` control**: completed implementation with `AutoGenerateColumns` support (dynamic columns generation based on binding item type properties), collection changed actions (`Add`, `Remove`, `Reset`) to synchronize visual rows, and high-performance compiled property getter caching (using lambda expression compilation with reflection fallback).
- **`TimePicker` control** (MAUI/Avalonia/WinUI `TimePicker` equivalent): inline segmented 24-hour `HH:mm` editor (`HH:mm:ss` with `ShowSeconds`) — Left/Right or mouse clicks select the hour/minute/second segment, Up/Down spin it with wrap-around and no carry, first spin on an empty picker fills in midnight; values normalize into one day at whole-second precision and changes raise a bubbling `SelectedTimeChanged`.
- **`DatePicker` control** (WPF/Avalonia/MAUI `DatePicker` equivalent): inline segmented `yyyy-MM-dd` editor — Left/Right select the year/month/day segment (mouse clicks too), Up/Down spin it (month/day wrap, day clamps to month length, first spin on an empty picker fills in today) — plus a dropdown `Calendar` overlay opened via the arrow button, F4, Alt+Down, Enter or Space; picking a day commits `SelectedDate` and closes, Escape or clicking outside dismisses. Raises bubbling `SelectedDateChanged`.
- **`Calendar` control** (WPF/Avalonia `Calendar` equivalent): 20×8-cell month view with `<`/`>` header month navigation, configurable `FirstDayOfWeek`, and separate cursor (`DisplayDate`) and selection (`SelectedDate`) with distinct highlight colors plus a today marker. Keyboard: arrows move by day/week, PageUp/PageDown by month (day-clamped), Home/End to month edges, Enter/Space selects; mouse clicks select days or flip months. Raises bubbling `SelectedDateChanged`/`DisplayDateChanged`; invariant-culture month/weekday names keep rendering machine-independent.
- **`NumericUpDown` control** (Avalonia `NumericUpDown` / WinUI `NumberBox` / MAUI `Stepper` equivalent): integer spinner rendered as `[-]  42 [+]` with `Value`/`Minimum`/`Maximum`/`Increment`, a bubbling `ValueChanged` routed event, clamping (including re-clamp when the range changes), mouse spin buttons and Up/Down/`+`/`-` keyboard spinning.
- **`RepeatButton` control** (WPF/Avalonia primitive): a `Button` that raises `Click` repeatedly while pressed — immediately on press (`ClickMode.Press` default), then after `Delay` ms and every `Interval` ms via a background timer; keyboard repeat rides the terminal's own key auto-repeat.
- **`ToggleSwitch` control** (MAUI `Switch` / Avalonia+WinUI `ToggleSwitch` equivalent): sliding-knob on/off switch rendered as `[●──] Off` / `[──●] On` with configurable `OnContent`/`OffContent` state labels, knob/track/bracket colors and characters, plus optional content label; inherits mouse/keyboard toggling and `Checked`/`Unchecked` events from `ToggleButton`, including three-state (indeterminate) support.
- **Standalone Skia host** (`Tedd.TUI.Platform.Skia`): `TuiSkiaHost` renders a TUI onto any bare SkiaSharp `SKCanvas` — or headless to `SKImage`/PNG (`RenderToImage`/`RenderToPng`) — with no GUI framework dependency. Pixel-space `MouseDown`/`MouseUp`/`MouseMove` forwarding, `ProcessKey`/`SendText` keyboard input, `RenderRequested` repaint signalling and configurable font/background; load and render errors draw into the surface instead of throwing. For game engines, custom windowing (OpenTK/Silk.NET), server-side rendering and CI screenshots.
- **SDL2 host** (`Tedd.TUI.Platform.Sdl2`): `TuiSdl2Host` puts a TUI in a native SDL2 window on Windows/Linux/macOS — `Run(title, columns, rows)` opens the window and drives the whole event loop, or `Attach`/`HandleEvent`/`RenderFrame` composite the TUI into an SDL render loop you already own (game engines, emulators). Paints through the standalone Skia host into a streaming ARGB8888 texture; SDL keyboard/text-input/mouse events map to the TUI input pipeline (`Sdl2KeyMapper`) with HiDPI mouse scaling and cross-thread repaint wakeup; native SDL2 binaries via `ppy.SDL2-CS`.
- **GUI platform hosts** — the same TUI now renders inside desktop/mobile frameworks:
  - `Tedd.TUI.Platform.Wpf`: `TuiHostElement` paints the cell grid via `DrawingContext`, including inside the Visual Studio XAML designer (live-preview TUI editing). Errors render into the surface instead of breaking the designer.
  - `Tedd.TUI.Surface.Skia`: shared SkiaSharp cell-grid painter + framework-agnostic `TuiSurfaceController` host logic.
  - `Tedd.TUI.Platform.Avalonia`: `TuiHostControl` (Skia via WriteableBitmap, HiDPI-aware) for Windows/macOS/Linux.
  - `Tedd.TUI.Platform.WinUI`: `TuiHostControl` on `SKXamlCanvas` for Windows App SDK apps.
  - `Tedd.TUI.Platform.Maui`: `TuiHostView` on `SKCanvasView` (Android/iOS/Mac Catalyst/Windows) with touch→mouse mapping and `SendKey`/`SendText` keyboard injection; `UseTeddTui()` builder extension.
- **`TuiXamlView` Blazor component**: hosts a XAML-defined TUI (`Source` file/URL or inline `Xaml`, plus `Controller`), standalone or nested in a `TuiView`; `XamlSource` resolves files, app-base paths and HTTP.
- **XamlLoader designer compatibility**: XML namespaces, prefixed elements, `x:Name` and designer attributes (`mc:Ignorable`, `d:DesignWidth`, `x:Class`, …) are now tolerated/ignored, so one file works in XAML editors and every host.
- **Documentation**: `docs/` with getting-started (all hosts × XAML/programmatic), XAML guide, per-platform guides, and a GitHub Pages website (`docs/index.html`) with renderer-generated screenshots.
- **Documentation — rich-content feature tour**: website and docs now showcase markdown rendering (`MarkdownView`), syntax highlighting (`CodeDocument`, 27 grammars) and inline images (Sixel/Kitty/iTerm2 + truecolor half-block fallback) with screenshots (`docs/assets/markdown.png`, `code.png`, `images.png`); added a supported-controls catalogue, an inline-image setup section to the console guide, and noted Windows Terminal 1.22+ Sixel support in the package table.
- **`Tedd.TUI.DocsScreenshots`**: a headless generator app that builds the docs website's sample screens (hero window, `MarkdownView`, `CodeDocument`, `Image`, a form and a `Table`) from real controls and renders them to PNG via `TuiSkiaHost`, so the GitHub Pages site and READMEs show genuine character-cell renderer output instead of hand-drawn SVG mockups (whose manual glyph spacing didn't match the real cell grid).
- **Packaging/CI**: shared NuGet metadata + package icon for all 11 packages; deploy workflow packs and publishes every package on `deploy`/`deploy/prod` with rising versions (`VERSION` file + run number); CI moved to Windows with MAUI workload; GitHub Pages deploy workflow.
- Initial release of **Tedd.TUI**.
- Core framework architecture (`UIElement`, `DependencyProperty`, `Geometry`).
- Recursive layout engine (Measure/Arrange).
- Decoupled rendering pipeline (`VirtualBuffer`, `ConsoleRenderer`).
- Basic controls: `TextBlock`, `Border`, `StackPanel`, `Button`, `TuiWindow`.
- Reflection-based XAML Loader.
- Basic Data Binding support.
- Unit tests for core functionality.
- CI/CD workflows using GitHub Actions.

### Fixed
- Fixed scrollbar thumb drags in terminals only taking effect on mouse release: the console host now requests VT mouse mode 1002 (button-event tracking) so terminals report motion while a button is held, and SGR motion reports (`Cb` bit 32) are dispatched as MouseMove — captured drags (scrollbar/Thumb) follow the pointer live.
- Fixed a bug where underlying characters bled through menu items and overlay borders when spaces were rendered with an opaque background.
- Fixed an issue where popup borders for menus and comboboxes always showed a vertical scrollbar by default, by configuring their vertical scrollbar visibility to `Auto`.
- Fixed constant 100% CPU use while idle: `DependencyObject.SetValue` now raises change notifications only when the effective value changes, so property writes during Measure/Render no longer re-arm the render loop every frame.
- Fixed `TuiApp.Stop()` racing the run loop; console teardown now happens on the loop thread and `Stop` is thread-safe and idempotent.
- Fixed a hot spin in the Windows wait loop when stdin is redirected, and console input mode / mouse tracking now restore on exit.
- Fixed escape-sequence handling: split sequences are reassembled, and SGR mouse reports are no longer truncated at the CSI introducer.
- Fixed keyboard activation of buttons/checkboxes on Unix by synthesizing KeyUp events (ClickMode.Release semantics).
- Fixed binding modes: TwoWay/OneWayToSource now write back to the source, OneTime transfers once; `SetBinding` replaces an existing binding instead of leaking its subscription.
- Fixed mouse coordinates inside scrolled ScrollViewer/Border content (click-to-caret and mouse capture were off by the scroll offset).
- Fixed overlays not re-fitting on window resize (dialogs re-center, popups clamp on-screen) and `Table.CurrentPage` not clamping when rows are removed.
- Fixed menu-bar hover stealing keyboard focus when no menu was open.
- Fixed the truecolor renderer invalidating the entire back-buffer after every frame containing an image; it now re-encodes images only when placements change or text overdraws them.
