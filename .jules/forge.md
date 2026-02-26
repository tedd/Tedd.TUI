## 2024-05-21 - Routed Event System Deficiency

**Observation:** The current `UIElement` architecture relies on standard C# events (e.g., `EventHandler`) for interaction. This prevents event bubbling and tunneling, critical for composite controls and decoupled interaction logic (e.g., handling button clicks at a parent `StackPanel` level).

**Strategic Action:** Implement a WPF-style Routed Event system with `Bubble`, `Tunnel`, and `Direct` strategies. This involves introducing `RoutedEvent`, `RoutedEventArgs`, and modifying `UIElement` to support `AddHandler`, `RemoveHandler`, and `RaiseEvent` traversal.

## 2024-05-23 - TreeView Integration

**Observation:** The TUI environment lacks a `TreeView` control, a staple of retro-modern environments (e.g., Norton Commander, Windows Explorer) for hierarchical data visualization. The existing `Table` and `ListBox` controls do not support hierarchical data binding or visual indentation with expanding/collapsing nodes.

**Strategic Action:** Implement `TreeView` and `TreeViewItem` controls. Synthesize the visual style of DOS directory trees (pipe characters `|`, `+`, `\-`) with modern WPF-style `ItemsSource` binding (via `ChildItemsPath` simplified template) and `ObservableCollection` support. Ensure `DataContext` propagates through the logical tree to support deep binding scenarios.

## 2024-05-24 - DataGrid Integration

**Observation:** The codebase lacks a standard `DataGrid` control for displaying tabular data with automatic column generation and data binding. The existing `Table` control requires manual row construction, which is tedious for dynamic data sources and deviates from modern WPF/Avalonia patterns.

**Strategic Action:** Implement `DataGrid` inheriting from `ItemsControl`. This control will utilize composition by hosting a `Table` internally for rendering but expose `ItemsSource` and `Columns` (with `BindingPath`) for modern data binding. It will support `AutoGenerateColumns` to simplify usage for rapid prototyping.

## 2026-02-26 - [Input Event Infrastructure & Data Context]
**Observation:** Standard input events (KeyDown, MouseDown) were virtual methods detached from the Routed Event system, preventing logical bubbling. Container controls used `List<UIElement>` exposing raw collections, causing failures in `Parent` assignment and DataContext inheritance when items were added manually.
**Strategic Action:**
- Refactored `KeyDown`, `KeyUp`, `MouseDown`, `MouseUp` to use the Routed Event system.
- Implemented `UIElementCollection` to enforce parentage on item addition/removal.
- Integrated `Slider` control to demonstrate new input capabilities.
- Implemented `OnParentChanged` in `UIElement` to automatically refresh inherited DataContext bindings.
