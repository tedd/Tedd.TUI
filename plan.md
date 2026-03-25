1. **Create ListBoxItem**: Inherit from `ContentControl`. Implement `IsSelected` property (boolean) and `Selected` / `Unselected` routed events (Bubbling). Create a default `ControlTemplate` (probably a `Border` or a `ContentPresenter` directly but with triggers to change colors when `IsSelected` is true). Add a click handler (`OnMouseDown` or similar) to raise an event or set `IsSelected` which `ListBox` can listen to? Standard WPF has `ListBoxItem` update its `IsSelected` on click, and `ListBox` listens to `ListBoxItem.SelectedEvent` to update its own `SelectedIndex`/`SelectedItem`.
2. **Refactor ListBox**:
   - Remove custom `MeasureOverride`, `ArrangeOverride`, `Render`, `GetVisualChild`, `VisualChildrenCount`.
   - Set `Template` in constructor to `ControlTemplate` containing `ScrollViewer` holding an `ItemsPresenter`.
   - Override `IsItemItsOwnContainerOverride` and `GetContainerForItemOverride`.
   - In `PrepareContainerForItemOverride`, set `IsSelected = true` if the index matches `SelectedIndex`.
   - Ensure `ItemsPanel` property defaults to `StackPanel`.
   - Listen to `ListBoxItem.SelectedEvent` to update `SelectedIndex`/`SelectedItem`.
   - Update existing `SelectionBackground`, `SelectionForeground` properties so they can be inherited or used by `ListBoxItem`'s triggers.
3. **Pre-commit**: Complete pre commit steps to ensure proper testing, verification, review, and reflection are done.
4. **Submit PR**: Formulate PR with the title `🏗️ Forge: [WPF Component/System] Parity Integration` as instructed.
