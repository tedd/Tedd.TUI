Tasks:
1. Create `ListBoxItem.cs` that inherits from `ContentControl`.
    - It needs an `IsSelected` dependency property.
    - It needs `Selected` and `Unselected` routed events (`RoutingStrategy.Bubble`).
    - The template for `ListBoxItem` should bind background/foreground colors appropriately, or it can be handled by triggers in `ListBoxItem`'s default ControlTemplate. Actually, wait, `ListBox` should provide a default style/template.
2. Refactor `ListBox.cs` to use `ControlTemplate`.
    - Remove `MeasureOverride`, `ArrangeOverride`, `Render`, `VisualChildrenCount`, `GetVisualChild`.
    - Set its default `Template` in the constructor: a `ControlTemplate` returning a `ScrollViewer` that contains an `ItemsPresenter`.
    - Override `GetContainerForItemOverride` to return `new ListBoxItem()`.
    - Override `IsItemItsOwnContainerOverride` to return `item is ListBoxItem`.
    - Override `PrepareContainerForItemOverride` to set `Content`, `ContentTemplate`, and also bind or set `IsSelected` if the item matches `SelectedIndex`. Or maybe handle `SelectionChanged` to update the containers' `IsSelected` states. Wait, `ItemsControl` provides `ItemsPresenter` which generates containers. How do we access generated containers?
    - If `ItemsPresenter` generates the containers, it uses `GetContainerForItemCore` and `PrepareContainerForItemOverride`. We can hook there to attach the `ListBox` as a parent or set up bindings? `ListBox`'s `SelectionChanged` will need to update `IsSelected` on the containers. We can iterate over `ItemsPanelRoot.Children` (which are the generated containers) and update their `IsSelected` property.

Wait, the instructions say:
"ListBox: Achieves WPF architectural isomorphism by utilizing a `ControlTemplate` wrapping an `ItemsPresenter` within a `ScrollViewer`, removing custom `MeasureOverride` and `Render` logic and deferring layout to standard XAML paradigms. The generated container, `ListBoxItem`, inherits from `ContentControl` and manages its visual state via the `IsSelected` dependency property and `Selected`/`Unselected` bubbling routed events."

Let's do this!
