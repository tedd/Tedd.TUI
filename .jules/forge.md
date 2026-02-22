## 2024-05-21 - Routed Event System Deficiency

**Observation:** The current `UIElement` architecture relies on standard C# events (e.g., `EventHandler`) for interaction. This prevents event bubbling and tunneling, critical for composite controls and decoupled interaction logic (e.g., handling button clicks at a parent `StackPanel` level).

**Strategic Action:** Implement a WPF-style Routed Event system with `Bubble`, `Tunnel`, and `Direct` strategies. This involves introducing `RoutedEvent`, `RoutedEventArgs`, and modifying `UIElement` to support `AddHandler`, `RemoveHandler`, and `RaiseEvent` traversal.
