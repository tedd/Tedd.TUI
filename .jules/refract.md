# Refract Journal

## 2025-02-23 - Property and Collection Modernization
**Observation:** Identified manual backing fields for `SelectedIndex` in `TabControl` and `Parent` in `UIElement`, typical of pre-C# 14 patterns. Also noted explicit `new List<T>()` initializations, which are verbose compared to C# 12 collection expressions.
**Strategic Action:** Applied C# 14 `field` keyword to auto-implemented properties to remove manual backing fields, reducing boilerplate. Replaced list instantiations with C# 12 collection expressions (`[]`) for conciseness.
