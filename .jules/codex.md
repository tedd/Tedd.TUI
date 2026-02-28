## 2024-05-27 - Architectural Discrepancies
**Observation:** The documentation claims full Bubbling/Tunneling support for standard input events (`KeyDown`, `MouseDown`). Code analysis reveals these are virtual method calls (`OnKeyDown`) on the focused element and do not utilize the `RoutedEvent` infrastructure, with the exception of `Button.Click`.
**Strategic Action:** Update README to accurately reflect that input is handled via virtual methods on the focused element, while the Routed Event system is available for custom control events like `Click`.

**Observation:** `StackPanel.Children` exposes a raw `List<UIElement>`, meaning `Children.Add()` fails to set the `Parent` property, breaking the visual tree. Users must use `AddChild()` or `XamlLoader` (which likely handles this).
**Strategic Action:** Documentation must emphasize `AddChild`.

**Observation:** `Table` control is manual. `DataGrid` provides `ItemsSource` binding and `AutoGenerateColumns`.
**Strategic Action:** Documentation should highlight `DataGrid` for list binding scenarios.

## 2024-05-28 - Architectural Articulation Update
**Observation:** The `README.md` contained outdated statements regarding explicit `AddChild` requirements for container controls like `StackPanel`, despite the framework employing `UIElementCollection` which implicitly propagates the `Parent` dependency property context. Furthermore, standard inputs (`KeyDownEvent`, `MouseDownEvent`) were incorrectly documented as isolated virtual method invocations rather than formally integrated components of the bubbling Routed Event architecture.
**Strategic Action:** Synchronized `README.md` to accurately reflect the implicit visual tree hierarchy establishment via standard collection methods (e.g., `Children.Add`) and articulated the comprehensive integration of standard inputs into the Routed Event framework.
