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

## 2024-05-19 - ButtonBase and ToggleButton Hierarchy
**Observation:** Standard WPF UI components `Button`, `CheckBox`, and `RadioButton` implement specialized behavior via inheritance (`ButtonBase` handling basic click logic, `ToggleButton` handling checked states). Prior framework implementation instantiated these components directly under `UIElement` or `ContentControl`, breaking WPF structural isomorphism and duplicating event handling code.
**Strategic Action:** Introduced `ButtonBase` and `ToggleButton` abstract classes derived from `ContentControl`. Refactored `Button`, `CheckBox`, and `RadioButton` to properly subclass these foundational classes, unifying `Click`, `Checked`, and `Unchecked` routed events and implementing WPF `ClickMode` and `IsThreeState` logic.
## 2026-03-05 - ItemsControl ItemTemplate Data Binding Propagation
**Observation:** The TUI framework lacked standard `ItemTemplate` support on `ItemsControl`. Consequently, elements generated using an implicit template mechanism (i.e. via `ContentPresenter`) were passing only string representations (`GetItemText`) of data items instead of raw data objects, crippling hierarchical and complex layout data bindings for standard collection visualizations.
**Strategic Action:**
- Added the `ItemTemplate` DependencyProperty to `ItemsControl`.
- Modified `PrepareContainerForItemOverride` in `ItemsControl` to set the generated `ContentPresenter`'s `Content` strictly to the underlying item when an `ItemTemplate` is provided.
- Successfully propagated raw item instances and corresponding `ContentTemplate`s, mirroring structural WPF/XAML items presentation behavior while remaining backward compatible with scalar text fallbacks.

## 2024-03-08 - Panel ZIndex Parity Integration
**Observation:** The core `Panel` layout framework lacked the `ZIndex` attached dependency property found in WPF, leading to the inability to deterministically control the visual stacking order of sibling children during rendering and hit testing without altering their logical collection order.
**Strategic Action:** Implemented `Panel.ZIndexProperty`. Upgraded `Panel` to intercept visual child access via `GetVisualChild`, intercepting access with a cached, lazily-evaluated array sorted stably by `ZIndex` to ensure declaration order is respected for ties. Hooked into `UIElement.OnPropertyChanged` and `UIElementCollection` mutations to accurately invalidate the Z-state cache (`InvalidateZState()`), strictly aligning layout behavior with WPF paradigms.

## 2026-03-09 - ControlTemplate Trigger Integration
**Observation:** The TUI framework lacked standard visual state infrastructure within `ControlTemplate`. There was an absence of WPF parity regarding `Trigger` mechanisms, which dynamically evaluate dependency properties and apply visual state updates (via `Setter`) without imperative event wiring.
**Strategic Action:** Integrated WPF visual state triggers by establishing the `TriggerBase`, `Trigger`, and `Setter` object model. Extended `ControlTemplate` with a `Triggers` collection. Augmented `Control.OnPropertyChanged` to intercept dependency property mutations, evaluate active trigger conditions (`EvaluateTriggers`), dynamically inject setter values when conditions are met, and automatically revert to original local/inherited property states (`DependencyProperty.UnsetValue`) when conditions fail.
## 2026-03-09 - Thumb Primitive Integration
**Observation:** The TUI framework lacked a native `Thumb` control primitive, which is necessary for establishing draggable and resizeable interactions (such as ScrollBars and GridSplitters). Standard implementations utilized ad-hoc logic per-component, preventing standardized drag event routing.
**Strategic Action:**
- Engineered the `Thumb` primitive inheriting from `Control`.
- Implemented bubbling routed events for the dragging lifecycle: `DragStarted`, `DragDelta`, and `DragCompleted`.
- Managed mouse capture via `TuiWindow` to calculate horizontal and vertical screen coordinate deltas and emit custom `RoutedEventArgs` mirroring the WPF `System.Windows.Controls.Primitives.Thumb` architectural specification.

## 2026-03-09 - GridSplitter Integration
**Observation:** The TUI framework lacked a `GridSplitter` component, which is a standard WPF control used within a `Grid` to dynamically resize rows and columns.
**Strategic Action:** Engineered the `GridSplitter` component inheriting from `Thumb`. Leveraged existing `DragDelta` event routing to intercept user interactions and translate them into size modifications on adjacent `RowDefinition`/`ColumnDefinition` objects of the parent `Grid`, filling a critical UI parity gap while maintaining the zero-allocation recursive layout engine methodology.
## 2026-03-09 - Dependency Property Trigger Precedence Fix
**Observation:** Discovered a parity deficit where `Control.cs` manually cached and restored original `_localValues` to apply visual state triggers, breaking the XAML declarative property precedence (where Local > Trigger but explicit user overrides during an active trigger should persist).
**Strategic Action:**
- Segmented `DependencyObject` state into `_localValues` and `_triggerValues`.
- Implemented `SetTriggerValue`/`ClearTriggerValue` internal methods.
- Corrected `GetValue()` to accurately evaluate `_triggerValues` and `_localValues`, properly simulating explicit local overrides via `_triggerValues.Remove(dp)` in `SetValue`.
- Upgraded `Control.EvaluateTriggers` to use these native API paths, removing error-prone internal value caching logic and ensuring WPF-isomorphic resolution.
## 2026-03-09 - Grid Attached Property Parity Integration
**Observation:** Discovered an architectural parity deficit where layout container attached properties (such as `Row`, `Column`, `RowSpan`, and `ColumnSpan` on `Grid`) were registered using standard `DependencyProperty.Register` rather than the required `DependencyProperty.RegisterAttached`. While functionally equivalent in the current internal implementation, this deviated from standard WPF/XAML semantics and architectural mapping.
**Strategic Action:** Modified the property registrations in `Grid.cs` to correctly invoke `DependencyProperty.RegisterAttached`, properly aligning the declarative property models with the structural intent of the `DependencyProperty` system used for attached layout paradigms.
## 2026-03-09 - ICommand Integration
**Observation:** The TUI framework lacked support for the standard `ICommand` interface, preventing declarative MVVM command bindings (e.g. executing business logic without code-behind event handlers). Controls derived from `ButtonBase` relied exclusively on the imperative `Click` routed event.
**Strategic Action:**
- Engineered the `ICommand` interface in `Tedd.TUI.Input`.
- Integrated `Command` and `CommandParameter` dependency properties into `ButtonBase`.
- Wired property change handlers to automatically subscribe to `CanExecuteChanged` and coerce `IsEnabled` based on `CanExecute`.
- Modified `OnClick` logic to invoke `Command.Execute()` after dispatching the standard `Click` event, successfully achieving WPF behavioral isomorphism for declarative interactions.

## 2026-03-09 - Separator Integration
**Observation:** The TUI framework lacked a native generic `Separator` component inheriting from `Control`. Specifically, menus (`MenuItem`) needed a structural element to delineate groups of items without stealing focus or responding to input, a standard paradigm in WPF and other client frameworks.
**Strategic Action:**
- Engineered the `Separator` control inheriting from `Control`.
- Enforced `Focusable = false` by default.
- Implemented a default template producing a simple `Border` with `BoxStyle.None` and 1-cell height.
- Overrode `Render` to draw a strict horizontal line (`\u2500`) constrained by `RenderSize.Width`, completing an architectural gap in standard menu layout syntax.
## 2026-03-09 - ScrollViewer ScrollBarVisibility Attached Properties
**Observation:** Discovered a parity deficit where `ScrollViewer.HorizontalScrollBarVisibility` and `VerticalScrollBarVisibility` were implemented as standard C# properties rather than dependency properties, and specifically, they were not attached properties. This violates WPF architecture where scroll bar visibility is a canonical attached property used widely in templates and styles on elements containing a ScrollViewer (such as ListBox).
**Strategic Action:** Refactored `HorizontalScrollBarVisibility` and `VerticalScrollBarVisibility` to be registered via `DependencyProperty.RegisterAttached`, implementing the required static `Get...` and `Set...` methods while maintaining instance property wrappers, bringing it into strict 1:1 behavioral and structural mapping with WPF standard API.

## 2026-03-09 - Control Content Alignment Parity Integration
**Observation:** Discovered a parity deficit where the `Control` class lacked `HorizontalContentAlignment` and `VerticalContentAlignment` dependency properties. Consequently, components utilizing control templates and `ContentPresenter` (like `ContentControl`, `Button`, `GroupBox`, `Expander`) could not leverage declarative bindings to adjust the internal alignment of their content, restricting standard WPF layout paradigms.
**Strategic Action:**
- Registered `HorizontalContentAlignment` and `VerticalContentAlignment` dependency properties on the `Control` base class.
- Modified default `ControlTemplate` implementations within `ContentControl`, `Button`, `GroupBox`, and `Expander` to bind the generated `ContentPresenter`'s `HorizontalAlignment` and `VerticalAlignment` directly to these new inherited parent properties, bridging a significant component styling parity gap.
## 2026-03-09 - DataContext Propagation Parity Integration
**Observation:** Discovered a core architectural deficit where layout container controls (e.g., `Border`, `DialogBox`, `Table`, `TuiWindow`, `Grid`) were manually overriding `OnDataContextChanged` to explicitly assign a local `DataContext` value onto their content or child elements. This manual set operation inadvertently overwrote and masked standard logical/visual tree inheritance tracking, breaking WPF/XAML isomorphism by treating inherited context mutations as local overrides.
**Strategic Action:**
- Systematically removed `OnDataContextChanged` overrides in `Border`, `DialogBox`, `Table`, `TuiWindow`, and `Grid`.
- Removed explicit `DataContext` local value setters when assigning child elements (e.g., `PushOverlay` in `TuiWindow.cs`, `Content` setter in `DialogBox.cs`).
- Delegated context propagation fully to `UIElement.OnPropertyChanged` which naturally handles `IsInherited` property traversal, matching the exact WPF hierarchical structure and eliminating false-positive `HasLocalValue` states on visual children.

## 2024-06-25 - Dependency Object Style Precedence Integration
**Observation:** Discovered a core parity deficit where the `DependencyObject` property system lacked support for `Style` values and strict XAML value precedence (`Local > Trigger > Style > Inherited > Default`). Previously, `Style` property evaluation mechanics and their specific impact on triggers/local overrides were absent.
**Strategic Action:**
- Created the `Style` class encompassing `Setter` and `TriggerBase` collections.
- Integrated `StyleProperty` into the `UIElement` foundation.
- Restructured `DependencyObject.GetValue()` and assignment logic to deterministically prioritize `_localValues`, `_triggerValues`, and `_styleValues` dictionaries, resolving property collisions according to standard WPF rules.
- Added comprehensive testing in `StyleTests.cs` to guarantee exact behavioral mapping under concurrent state mutations.
