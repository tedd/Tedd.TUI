# Refract Journal

## 2025-02-19 - C# 14 Property Modernization
**Observation:** Identified manual backing fields for properties with validation logic in `ScrollBar.cs` (`Value`, `Minimum`, `Maximum`) and `Table.cs` (`ShowHorizontalLines`, `SelectedIndex`, `PageSize`, `CurrentPage`, `TotalRows`). These patterns (common in older C# versions) require verbose private field declarations.
**Strategic Action:** Replaced manual backing fields with the C# 14 `field` keyword. This reduces boilerplate, encapsulates storage within the property, and maintains validation logic directly in the setter.

## 2025-02-19 - C# 14 Property Modernization, C# 12 Collections, C# 13 System.Threading.Lock
**Observation:** Identified manual backing fields for `SelectedItem` in `TreeView.cs` and `DataGrid.cs`. Legacy collection initializations like `new Dictionary<Type, PropertyInfo?>()` and `new ObservableCollection<TreeViewItem>()` were present in `TreeView.cs` and `ItemsControl.cs`. Missing thread safety for metadata caches (`_displayMemberCache`, `_childItemsCache`) which could cause race conditions.
**Strategic Action:** Applied C# 14 `field` keyword to `SelectedItem` properties, eliminating `_selectedItem` manual backing fields. Implemented C# 12 collection expressions (`[]`) for concise instantiations. Added C# 13 `System.Threading.Lock` in `TreeView.cs` and `ItemsControl.cs` to ensure deterministic thread safety when reading or clearing reflection metadata caches.

## 2024-11-20 - Lexical Deficits - field properties, collections, lock

**Observation:** Private backing fields were used in auto-properties `_content`, `_title`, `_header` and `_isExpanded` in DialogBox.cs, Border.cs, MenuItem.cs, and TreeViewItem.cs (.NET 5-8 style).
**Strategic Action:** Applied C# 14 `field` keyword to minimize boilerplate and remove the private backing fields.

**Observation:** Legacy locking constructs like `System.Threading.Lock _displayMemberCacheLock = new System.Threading.Lock()` were used in `TreeView.cs` and `ItemsControl.cs`.
**Strategic Action:** Consolidated lock instantiation by adopting C# 13 `new()` expression.

**Observation:** Legacy list and dictionary collections were initialized explicitly using verbose constructs like `new List<UIElement>()` and `new Dictionary<string, List<Pattern>>()` in Markdown/Paragraph.cs, CodeColoring/Grammar.cs, CodeColoring/LanguageRegistry.cs, and CodeColoring/Theme.cs.
**Strategic Action:** Enforced C# 12 collection expressions (`[]`) to reduce lexical boilerplate and object allocation overhead.

## 2024-05-24 - Parameter Optimization
**Observation:** `Table.AddRow` utilized legacy `params T[]` parameter arrays, which allocate an array on the heap for each invocation containing multiple parameters.
**Strategic Action:** Applied C# 13 `params ReadOnlySpan<T>` feature to the parameter arrays for the identified method. This allows the compiler to allocate the parameter values on the stack or use an inline array instead of a heap-allocated array, significantly reducing GC pressure.

## 2025-02-19 - Syntactic Modernization: field keywords, target-typed new(), collection expressions
**Observation:** Discovered `_templatedParent` backing field in `UIElement.cs` (.NET 5/6 legacy patterns). Detected a verbose `new Dictionary<DependencyProperty, object>()` instantiation in `DependencyObject.cs`. Located verbose array-like initialization (`new List<RowDefinition> { new RowDefinition() }`) for implicit rows/cols in `Grid.cs`.
**Strategic Action:**
- Applied the C# 14 `field` keyword to `TemplatedParent` property, eliminating the manual backing field.
- Implemented C# 13 target-typed `new()` for the `_values` dictionary instantiation in `DependencyObject.cs` to eliminate redundant type declarations.
- Utilized C# 12 collection expressions (`[]`) via the `??=` operator (`_implicitRows ??= [new RowDefinition()]`) to reduce lexical boilerplate in `Grid.cs`.
## 2024-03-04 - C# 13 System.Threading.Lock Modernization
**Observation:** The codebase contained a legacy lock statement (`lock (_globalCompiledGetters)`) utilizing an arbitrary object for synchronization in `DataGrid.cs`, which is an obsolete .NET 5/6 pattern that lacks explicit thread safety semantics.
**Strategic Action:** Transitioned the synchronization mechanism to use the C# 13 `System.Threading.Lock` type by introducing a dedicated `_globalCompiledGettersLock = new();` instance to enforce deterministic thread safety and structural clarity.

## 2025-03-04 - Syntactic Modernization: Expression-Bodied Members for Properties
**Observation:** Legacy .NET 5-8 property getter/setter block structures (`get { return (Type)GetValue(Property); }`) and (`set { SetValue(Property, value); }`) were extensively utilized across numerous UI controls (e.g., `DialogBox.cs`, `ComboBox.cs`, `UIElement.cs`, `MarkdownView.cs`) for Dependency Property accessors, creating unnecessary lexical boilerplate.
**Strategic Action:** Applied C# expression-bodied members (`=>`) to all identified `get` and `set` accessors for Dependency Properties to eliminate lexical boilerplate and enforce structural conciseness according to modern C# syntax standards.

## 2026-03-06 - Property Modernization
**Observation:** Identified legacy auto-implemented properties utilizing manual private backing fields (e.g., `_templateRoot` in `Control.cs`, `_parent` in `UIElement.cs`, `_theme` in `MarkdownView.cs`, `_content` in `TuiWindow.cs`).
**Strategic Action:** Applied C# 14 field-backed properties to minimize lexical boilerplate and encapsulate validation logic directly within the accessor. Replaced manual backing fields with the `field` keyword or automatic properties where applicable, effectively reducing cognitive load without altering the functional semantics.
## 2024-05-19 - C# 14 Array Initialization and Slicing Optimization
**Observation:** Legacy local character array initialized on every method invocation in `FindNextSpecial` within `src/Tedd.TUI/Markdown/MarkdownParser.cs` causing unnecessary heap allocation: `char[] chars = { '[', '!', '*', '`' };`.
**Strategic Action:** Upgraded to C# 12 collection expressions mapped to a stack-allocated C# 14 primitive: `ReadOnlySpan<char> chars = ['[', '!', '*', '`'];`. Replaced `string.IndexOfAny` with zero-allocation slicing leveraging C# 14 implicit span conversions: `text.AsSpan(start).IndexOfAny(chars);`.
