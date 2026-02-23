## 2024-05-21 - Routed Event System Deficiency

**Observation:** The current `UIElement` architecture relies on standard C# events (e.g., `EventHandler`) for interaction. This prevents event bubbling and tunneling, critical for composite controls and decoupled interaction logic (e.g., handling button clicks at a parent `StackPanel` level).

**Strategic Action:** Implement a WPF-style Routed Event system with `Bubble`, `Tunnel`, and `Direct` strategies. This involves introducing `RoutedEvent`, `RoutedEventArgs`, and modifying `UIElement` to support `AddHandler`, `RemoveHandler`, and `RaiseEvent` traversal.

## 2024-05-23 - TreeView Integration

**Observation:** The TUI environment lacks a `TreeView` control, a staple of retro-modern environments (e.g., Norton Commander, Windows Explorer) for hierarchical data visualization. The existing `Table` and `ListBox` controls do not support hierarchical data binding or visual indentation with expanding/collapsing nodes.

**Strategic Action:** Implement `TreeView` and `TreeViewItem` controls. Synthesize the visual style of DOS directory trees (pipe characters `|`, `+`, `\-`) with modern WPF-style `ItemsSource` binding (via `ChildItemsPath` simplified template) and `ObservableCollection` support. Ensure `DataContext` propagates through the logical tree to support deep binding scenarios.
