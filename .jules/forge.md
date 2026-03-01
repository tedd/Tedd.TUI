## 2024-05-18 - HeaderedItemsControl Compilation Fixes
**Observation:** `DependencyProperty.RegisterReadOnly` is not present in the current framework API. We need to implement it or use standard `Register` for `HasHeaderProperty` and manage it internally. The framework has a custom `DependencyProperty` implementation. `TreeView` is also failing to compile because it expects items to be strictly `TreeViewItem` but now `TreeViewItem.Items` is from `ItemsControl`, meaning it holds `object` instead of `TreeViewItem`.
**Strategic Action:**
1. Fix `HeaderedItemsControl.cs` to use `DependencyProperty.Register` for `HasHeaderProperty`.
2. `ItemsControl` (and thus `HeaderedItemsControl`) has `Items` of type `ItemCollection` which implements `IList` (of object). `TreeView` logic needs to cast objects to `TreeViewItem` when working with hierarchical items.
