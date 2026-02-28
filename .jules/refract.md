# Refract Journal

## 2025-02-19 - C# 14 Property Modernization
**Observation:** Identified manual backing fields for properties with validation logic in `ScrollBar.cs` (`Value`, `Minimum`, `Maximum`) and `Table.cs` (`ShowHorizontalLines`, `SelectedIndex`, `PageSize`, `CurrentPage`, `TotalRows`). These patterns (common in older C# versions) require verbose private field declarations.
**Strategic Action:** Replaced manual backing fields with the C# 14 `field` keyword. This reduces boilerplate, encapsulates storage within the property, and maintains validation logic directly in the setter.

## 2025-02-19 - C# 14 Property Modernization, C# 12 Collections, C# 13 System.Threading.Lock
**Observation:** Identified manual backing fields for `SelectedItem` in `TreeView.cs` and `DataGrid.cs`. Legacy collection initializations like `new Dictionary<Type, PropertyInfo?>()` and `new ObservableCollection<TreeViewItem>()` were present in `TreeView.cs` and `ItemsControl.cs`. Missing thread safety for metadata caches (`_displayMemberCache`, `_childItemsCache`) which could cause race conditions.
**Strategic Action:** Applied C# 14 `field` keyword to `SelectedItem` properties, eliminating `_selectedItem` manual backing fields. Implemented C# 12 collection expressions (`[]`) for concise instantiations. Added C# 13 `System.Threading.Lock` in `TreeView.cs` and `ItemsControl.cs` to ensure deterministic thread safety when reading or clearing reflection metadata caches.
