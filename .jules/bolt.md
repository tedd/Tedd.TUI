## 2024-05-18 - ConsoleRenderer Render Loop Bottleneck
**Observation:** `ConsoleRenderer.Render` loops W*H times doing `IsSame` checks and manually pushing to a `_charBuffer` char by char. IsSame can be optimized using Unsafe.As/SIMD if struct is aligned, or just refactoring the chunking to be span-based.
**Strategic Action:** We need to refactor `ConsoleRenderer.Render` to process `VirtualBuffer.Cells` in a more streamlined way.
## 2024-05-18 - ConsoleRenderer Loop Vectorization
**Observation:** By refactoring the inner loop of `ConsoleRenderer.Render` to use `ref` variables, `Unsafe.Add`, and inline struct field comparison, we dropped execution time from ~21.45us to ~12.94us (40% reduction) and eliminated the remaining 104B allocations.
**Strategic Action:** Commit this optimization using contemporary .NET ref and Unsafe paradigms to improve terminal redraw speed drastically.
## 2024-05-20 - Grid Layout Suboptimal Iterations
**Observation:** During `Grid` measurement and arrangement passes, `foreach` enumeration over generic `List<T>` collections (`RowDefinitions` and `ColumnDefinitions`) incurs hidden bounds-checking overhead and possible enumerator allocations.
**Strategic Action:** Replace all list enumeration in tight rendering/layout loops with `System.Runtime.InteropServices.CollectionsMarshal.AsSpan` and `ref var` local iteration to elide bounds checks and guarantee zero-allocation.
