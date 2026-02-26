## 2024-05-27 - Architectural Discrepancies
**Observation:** The documentation claims full Bubbling/Tunneling support for standard input events (`KeyDown`, `MouseDown`). Code analysis reveals these are virtual method calls (`OnKeyDown`) on the focused element and do not utilize the `RoutedEvent` infrastructure, with the exception of `Button.Click`.
**Strategic Action:** Update README to accurately reflect that input is handled via virtual methods on the focused element, while the Routed Event system is available for custom control events like `Click`.

**Observation:** `StackPanel.Children` exposes a raw `List<UIElement>`, meaning `Children.Add()` fails to set the `Parent` property, breaking the visual tree. Users must use `AddChild()` or `XamlLoader` (which likely handles this).
**Strategic Action:** Documentation must emphasize `AddChild`.

**Observation:** `Table` control is manual. `DataGrid` provides `ItemsSource` binding and `AutoGenerateColumns`.
**Strategic Action:** Documentation should highlight `DataGrid` for list binding scenarios.
