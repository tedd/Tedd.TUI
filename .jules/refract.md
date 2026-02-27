# Refract Journal

## 2025-02-19 - C# 14 Property Modernization
**Observation:** Identified manual backing fields for properties with validation logic in `ScrollBar.cs` (`Value`, `Minimum`, `Maximum`) and `Table.cs` (`ShowHorizontalLines`, `SelectedIndex`, `PageSize`, `CurrentPage`, `TotalRows`). These patterns (common in older C# versions) require verbose private field declarations.
**Strategic Action:** Replaced manual backing fields with the C# 14 `field` keyword. This reduces boilerplate, encapsulates storage within the property, and maintains validation logic directly in the setter.
