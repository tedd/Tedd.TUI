- Looking for ways to increase WPF parity for Forge.
- `ItemsControl` currently lacks `ItemContainerGenerator` logic where we can use `IsItemItsOwnContainerOverride` and `GetContainerForItemOverride` from subclasses.
- The `ListBox` in `Tedd.TUI` does not have a `ListBoxItem` container class. It renders text or templates directly.
- The instructions mention: "ListBox: Achieves WPF architectural isomorphism by utilizing a `ControlTemplate` wrapping an `ItemsPresenter` within a `ScrollViewer`, removing custom `MeasureOverride` and `Render` logic and deferring layout to standard XAML paradigms. The generated container, `ListBoxItem`, inherits from `ContentControl` and manages its visual state via the `IsSelected` dependency property and `Selected`/`Unselected` bubbling routed events."

So my task is to refactor `ListBox` to use a `ControlTemplate` containing a `ScrollViewer` and an `ItemsPresenter`. And create `ListBoxItem` inheriting from `ContentControl`.
