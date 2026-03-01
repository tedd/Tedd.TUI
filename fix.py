with open(".jules/forge.md", "r") as f:
    text = f.read()

text = text.replace("<<<<<<< HEAD\n- Refactored", "1. Fix `HeaderedItemsControl.cs` to use `DependencyProperty.Register` for `HasHeaderProperty`.\n2. `ItemsControl` (and thus `HeaderedItemsControl`) has `Items` of type `ItemCollection` which implements `IList` (of object). `TreeView` logic needs to cast objects to `TreeViewItem` when working with hierarchical items.\n\n## 2026-02-26 - [Input Event Infrastructure & Data Context]\n**Observation:** Standard input events (KeyDown, MouseDown) were virtual methods detached from the Routed Event system, preventing logical bubbling. Container controls used `List<UIElement>` exposing raw collections, causing failures in `Parent` assignment and DataContext inheritance when items were added manually.\n**Strategic Action:**\n- Refactored")

text = text.replace("bounded by `RenderSize`.\n=======\n1. Fix `HeaderedItemsControl.cs` to use `DependencyProperty.Register` for `HasHeaderProperty`.\n2. `ItemsControl` (and thus `HeaderedItemsControl`) has `Items` of type `ItemCollection` which implements `IList` (of object). `TreeView` logic needs to cast objects to `TreeViewItem` when working with hierarchical items.\n>>>>>>> origin/main\n", "bounded by `RenderSize`.\n")

with open(".jules/forge.md", "w") as f:
    f.write(text)
