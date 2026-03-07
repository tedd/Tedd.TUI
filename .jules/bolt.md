## 2024-05-18 - ConsoleRenderer Render Loop Bottleneck
**Observation:** `ConsoleRenderer.Render` loops W*H times doing `IsSame` checks and manually pushing to a `_charBuffer` char by char. IsSame can be optimized using Unsafe.As/SIMD if struct is aligned, or just refactoring the chunking to be span-based.
**Strategic Action:** We need to refactor `ConsoleRenderer.Render` to process `VirtualBuffer.Cells` in a more streamlined way.
## 2024-05-18 - ConsoleRenderer Loop Vectorization
**Observation:** By refactoring the inner loop of `ConsoleRenderer.Render` to use `ref` variables, `Unsafe.Add`, and inline struct field comparison, we dropped execution time from ~21.45us to ~12.94us (40% reduction) and eliminated the remaining 104B allocations.
**Strategic Action:** Commit this optimization using contemporary .NET ref and Unsafe paradigms to improve terminal redraw speed drastically.
## 2024-05-20 - Grid Layout Suboptimal Iterations
**Observation:** During `Grid` measurement and arrangement passes, `foreach` enumeration over generic `List<T>` collections (`RowDefinitions` and `ColumnDefinitions`) incurs hidden bounds-checking overhead and possible enumerator allocations.
**Strategic Action:** Replace all list enumeration in tight rendering/layout loops with `System.Runtime.InteropServices.CollectionsMarshal.AsSpan` and `ref var` local iteration to elide bounds checks and guarantee zero-allocation.

## 2025-03-05 - String.Split Allocation Optimization
**Observation:** In critical layout paths such as XAML property setting, text editor parsing, and Markdown tokenization, `string.Split()` was heavily utilized. This incurs an `O(n)` array allocation overhead and generates numerous intermediate string objects, leading to excessive GC pressure. A BenchmarkDotNet symmetric test confirmed that `string.Split()` on standard inputs allocated 104-520 Bytes per call, whereas `ReadOnlySpan<char>` sliced loops allocated 0 Bytes.
**Strategic Action:** Refactored `XamlLoader.cs`, `TextEditor.cs`, and `MarkdownParser.cs` to utilize `ReadOnlySpan<char>.IndexOf`, `Slice`, and `EnumerateLines()`. This successfully reduces time and space complexities in these routines from `O(n)` time and `O(n)` space allocations down to `O(n)` time with `O(1)` intermediate array allocations, demonstrating up to 50% faster execution.

## 2026-03-07 - UIElement.RaiseEvent Allocation Bottleneck
**Observation:** `RaiseEvent` allocated a `List<UIElement>` on every event propagation to store the routing path. Given that events like `MouseMove` can fire frequently, this continuous `O(h)` memory allocation (where h is the hierarchy depth) generates significant GC pressure. BenchmarkDotNet confirmed `~1.00 us` execution time with `1144 B` allocated per call.
**Strategic Action:** Refactored `RaiseEvent` to utilize a two-pass algorithm. The first pass measures the route depth `h`. The second pass borrows an array from `System.Buffers.ArrayPool<UIElement>.Shared.Rent(h)`, populates it, executes the routing pass, and returns the array to the pool. Time complexity remains `O(h)`, but space complexity allocation drops to `O(1)`, completely eliminating GC allocations during event dispatch.
