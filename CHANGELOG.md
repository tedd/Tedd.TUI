# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- **GUI platform hosts** — the same TUI now renders inside desktop/mobile frameworks:
  - `Tedd.TUI.Platform.Wpf`: `TuiHostElement` paints the cell grid via `DrawingContext`, including inside the Visual Studio XAML designer (live-preview TUI editing). Errors render into the surface instead of breaking the designer.
  - `Tedd.TUI.Surface.Skia`: shared SkiaSharp cell-grid painter + framework-agnostic `TuiSurfaceController` host logic.
  - `Tedd.TUI.Platform.Avalonia`: `TuiHostControl` (Skia via WriteableBitmap, HiDPI-aware) for Windows/macOS/Linux.
  - `Tedd.TUI.Platform.WinUI`: `TuiHostControl` on `SKXamlCanvas` for Windows App SDK apps.
  - `Tedd.TUI.Platform.Maui`: `TuiHostView` on `SKCanvasView` (Android/iOS/Mac Catalyst/Windows) with touch→mouse mapping and `SendKey`/`SendText` keyboard injection; `UseTeddTui()` builder extension.
- **`TuiXamlView` Blazor component**: hosts a XAML-defined TUI (`Source` file/URL or inline `Xaml`, plus `Controller`), standalone or nested in a `TuiView`; `XamlSource` resolves files, app-base paths and HTTP.
- **XamlLoader designer compatibility**: XML namespaces, prefixed elements, `x:Name` and designer attributes (`mc:Ignorable`, `d:DesignWidth`, `x:Class`, …) are now tolerated/ignored, so one file works in XAML editors and every host.
- **Documentation**: `docs/` with getting-started (all hosts × XAML/programmatic), XAML guide, per-platform guides, and a GitHub Pages website (`docs/index.html`) with renderer-generated screenshots.
- **Documentation — rich-content feature tour**: website and docs now showcase markdown rendering (`MarkdownView`), syntax highlighting (`CodeDocument`, 27 grammars) and inline images (Sixel/Kitty/iTerm2 + truecolor half-block fallback) with new renderer-generated screenshots (`docs/assets/markdown.svg`, `code.svg`, `images.svg`); added a supported-controls catalogue, an inline-image setup section to the console guide, and noted Windows Terminal 1.22+ Sixel support in the package table.
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
