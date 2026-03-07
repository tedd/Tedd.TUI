## 2024-05-18 - HeaderedItemsControl Compilation Fixes
**Observation:** `DependencyProperty.RegisterReadOnly` is not present in the current framework API. We need to implement it or use standard `Register` for `HasHeaderProperty` and manage it internally. The framework has a custom `DependencyProperty` implementation. `TreeView` is also failing to compile because it expects items to be strictly `TreeViewItem` but now `TreeViewItem.Items` is from `ItemsControl`, meaning it holds `object` instead of `TreeViewItem`.
**Strategic Action:**
1. Fix `HeaderedItemsControl.cs` to use `DependencyProperty.Register` for `HasHeaderProperty`.
2. `ItemsControl` (and thus `HeaderedItemsControl`) has `Items` of type `ItemCollection` which implements `IList` (of object). `TreeView` logic needs to cast objects to `TreeViewItem` when working with hierarchical items.
## 2024-05-18 - XAML Isomorphic Architecture Additions
**Observation:** Standard XAML parity regarding Margins on `UIElement` and Padding inner space reduction on `Control`'s Layout overrides was missing.
**Strategic Action:** Added `MarginProperty` to `UIElement` and integrated it into the base `Measure` and `Arrange` methods to ensure WPF-compliant layout algorithms. Additionally implemented `Padding` space reduction within `Control.MeasureOverride` and `Control.ArrangeOverride`.

## 2024-05-18 - UniformGrid Component Integration
**Observation:** The TUI layout system lacked a UniformGrid panel, a standard XAML component required for symmetrical control arrangements in MS-DOS style forms.
**Strategic Action:** Engineered the UniformGrid inheriting from Panel, mapping XAML-style Rows, Columns, and FirstColumn properties to the TUI character grid constraints.
## 2026-02-26 - [Input Event Infrastructure & Data Context]
**Observation:** Standard input events (KeyDown, MouseDown) were virtual methods detached from the Routed Event system, preventing logical bubbling. Container controls used `List<UIElement>` exposing raw collections, causing failures in `Parent` assignment and DataContext inheritance when items were added manually.
**Strategic Action:**
- Refactored `KeyDown`, `KeyUp`, `MouseDown`, `MouseUp` to use the Routed Event system.
- Implemented `UIElementCollection` to enforce parentage on item addition/removal.
- Integrated `Slider` control to demonstrate new input capabilities.
- Implemented `OnParentChanged` in `UIElement` to automatically refresh inherited DataContext bindings.

## 2024-05-25 - DockPanel Integration

**Observation:** The TUI environment lacks a `DockPanel` control, which is essential for creating layouts where elements are docked to the edges of a container (e.g., toolbars, status bars, side panels). Currently, achieving this requires complex nesting of `StackPanel` and `Grid` controls.

**Strategic Action:** Implement `DockPanel` control with `Dock` attached property (Left, Top, Right, Bottom) and `LastChildFill` property. This will allow for flexible and efficient layout management, mirroring the behavior of the WPF `DockPanel`.

## 2024-05-26 - Panel Base Class & Missing Layouts Integration

**Observation:** The TUI framework lacked a common base class for layout panels, leading to duplicated `UIElementCollection` management and redundant optimized `Render` implementations in `Grid`, `StackPanel`, and `DockPanel`. Additionally, standard WPF layout controls `WrapPanel` and `Canvas` were missing, preventing flexible flow and absolute-positioned layouts.

**Strategic Action:**
- Extracted common collection management and rendering logic into a new abstract `Panel` class.
- Refactored `Grid`, `StackPanel`, and `DockPanel` to inherit from `Panel`.
- Implemented `WrapPanel` with dynamic `Orientation` line-breaking logic.
- Implemented `Canvas` utilizing explicit `Canvas.Left` and `Canvas.Top` attached dependency properties for zero-constraint positioning.

## 2026-03-05 - TextEditor Integration

**Observation:** The TUI framework lacked an advanced multi-line `TextEditor` control, which is necessary to fill a parity gap with modern UI frameworks like WPF and Avalonia. A multi-line editing paradigm is critical for robust text input capabilities, expanding beyond the single-line `TextBox`.

**Strategic Action:**
- Implemented `TextEditor` as a `UIElement` handling multi-line state directly.
- Leveraged `List<string>` for lightweight line management to mitigate extensive text reallocations during simple keystrokes.
- Integrated arrow key navigation, text insertion, multi-line separation (Enter), and text deletion (Backspace/Delete) along with view scrolling bounded by `RenderSize`.

## 2024-05-18 - Expander Control Integration

**Observation:** The TUI framework lacked a native `Expander` control for progressively disclosing information or grouping settings, which is a standard structural component in modern client frameworks like WPF. The existing `HeaderedContentControl` provided a base, but no concrete implementation existed to toggle state while adhering to the TUI character-based visual constraints.

**Strategic Action:** Engineered an `Expander` control inheriting from `HeaderedContentControl`. Implemented the `IsExpanded` dependency property and corresponding `Expanded`/`Collapsed` routed events (bubbling up the logical tree). Designed an internalized template using `StackPanel`, `Border`, and `ContentPresenter` to map the boolean state to layout visibility (`ContentPresenter.Visibility`), successfully translating WPF's expander paradigm into the zero-allocation recursive layout engine of the TUI framework using explicit ASCII indicators (`[+]`, `[-]`).
## 2026-03-05 - Event Routing Parity (Tunneling Phase)
**Observation:** Standard XAML input event parity was missing the tunneling phase. While bubbling events like `KeyDownEvent` existed, their tunneling counterparts (`PreviewKeyDownEvent`) were absent, preventing parent elements from intercepting events prior to child handling.
**Strategic Action:**
- Registered `RoutingStrategy.Tunnel` events for `PreviewKeyDown`, `PreviewKeyUp`, `PreviewMouseDown`, `PreviewMouseUp`, and `PreviewMouseMove` in `UIElement`.
- Implemented two-phase dispatch in `TuiWindow.ProcessKey` and `ConsoleInputManager` mouse handlers: dispatching the preview event first, and only dispatching the bubbling event if the preview event's `Handled` property remained false.
## 2026-03-05 - ItemsControl Class Structure and ItemsPanel Parity

**Observation:** The TUI environment's `ItemsControl` class lacked parity with WPF's layout mechanism. Specifically, it inherited from `UIElement` instead of `Control`, which prevented template support. Furthermore, it lacked the standard `ItemsPanel` dependency property (and associated `ItemsPanelTemplate`), meaning its layout could not be declaratively changed as in standard XAML.

**Strategic Action:**
- Modified `ItemsControl` to inherit from `Control`, aligning its inheritance hierarchy with WPF.
- Implemented the `ItemsPanelTemplate` class inheriting from `FrameworkTemplate` to support dynamic panel generation.
- Added the `ItemsPanel` dependency property to `ItemsControl`, utilizing a default factory that generates a `StackPanel` with `Orientation.Vertical` to fulfill the standard default layout behavior.
## 2025-03-03 - GroupBox Integration
**Observation:** The TUI framework lacked a native component for visual grouping with explicit title support natively mapping to the TUI (like a WPF GroupBox), although `HeaderedContentControl` and `Border` existed.
**Strategic Action:** Developed `GroupBox` by subclassing `HeaderedContentControl` and leveraging the `ControlTemplate` engine to map the `Header` to a `Border` element's `Title`, synthesizing the visual paradigms of DOS-era environments with contemporary .NET object models.

## 2025-03-05 - DependencyProperty Value Precedence
**Observation:** Discovered a parity deficit in the Dependency Property system where local value removal was not correctly mapped to WPF paradigms. Previously, `SetValue(null)` was attempting to clear values or act as a pseudo-clear, violating standard declarative property workflows. The system lacked `DependencyObject.ClearValue()`, preventing fallback to default or inherited values.
**Strategic Action:** Implemented `ClearValue(DependencyProperty)` on `DependencyObject` to support WPF-isomorphic property resolution, correctly allowing fallback to default values upon local value removal, and modified `SetValue` to deterministically store `null` when provided instead of deleting the entry.
## 2026-03-05 - Toggle Control Event Routing Integration
**Observation:** The TUI framework lacked standard `Checked` and `Unchecked` routed events for `CheckBox` and `RadioButton` controls. This prevented structural parity with established UI frameworks like WPF where state changes on toggle controls bubble up the logical tree for parent container interception.
**Strategic Action:** Registered `Checked` and `Unchecked` bubbling routed events (`RoutingStrategy.Bubble`) in both `CheckBox` and `RadioButton`. Overrode `OnPropertyChanged` for `IsCheckedProperty` to dispatch the appropriate event upon state change. Adjusted `RadioButton` logic to trigger group updates before dispatching the `Checked` event, ensuring synchronous propagation of the corresponding `Unchecked` events on sibling controls.
## 2026-03-05 - PasswordBox Component Integration
**Observation:** The TUI framework lacked a native `PasswordBox` component for masking secure text inputs, a fundamental requirement for credential forms in advanced UI frameworks like WPF/Avalonia. The underlying `TextBox` possessed an `IsPassword` property, but exposing an explicit control that correctly masks input and provides a unified `Password` property bridges a significant input component parity gap.
**Strategic Action:** Engineered the `PasswordBox` component inheriting from `Control`. Leveraged the internal `ControlTemplate` engine to host a `TextBox` configured for secure character masking (`IsPassword = true`). Implemented manual keystroke synchronization (`OnKeyDown`) to reliably push internal text updates to the exposed `Password` dependency property, sidestepping TUI framework limitations with `TwoWay` dependency property binding resolution.
## 2026-03-05 - ButtonBase and ToggleButton Integration
**Observation:** The TUI framework lacked common structural abstraction for button controls (`Button`, `CheckBox`, `RadioButton`). This missing hierarchy prevents parity with WPF where routing for `ClickEvent`, `ClickMode` (Release, Press, Hover), and toggle states (`IsChecked`, `IsThreeState`) are managed centrally.
**Strategic Action:**
- Implemented `ButtonBase` inheriting from `ContentControl` to govern `ClickMode` and `IsPressed` state logic, acting as the foundation for button components.
- Implemented `ToggleButton` inheriting from `ButtonBase` to natively support `IsChecked` (`bool?`), `IsThreeState`, and unified `Checked`/`Unchecked`/`Indeterminate` routed events.
- Refactored `Button` to inherit from `ButtonBase` and `CheckBox`/`RadioButton` to inherit from `ToggleButton`, eliminating property duplication and manually routed events while achieving structural isomorphism with WPF.
