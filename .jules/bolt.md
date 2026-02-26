## 2026-02-22 - Table Pagination Optimization
**Observation:** `Table.GetPaginationString` was allocating a `string` on every render (or cache miss) to display pagination. Benchmark showed ~48B alloc per call for short strings and ~153ns latency. This allocation pressure, although small per call, accumulates in the rendering loop.
**Strategic Action:** Refactored `GetPaginationString` to use `stackalloc char[256]` and return `int` (chars written). Updated `RenderPagination` and `HandlePaginationClick` to use this zero-allocation method. Result: 0 bytes allocated and ~18-98ns latency (38-70% reduction).

## 2026-02-24 - VirtualBuffer Bulk Rendering Optimization
**Observation:** `VirtualBuffer` operations relied heavily on `SetPixel` inside tight loops (e.g., in `Table.Render` and `Border.Render`). This incurred per-pixel bounds checking, clipping, and method call overhead.
**Strategic Action:** Implemented `DrawString` (Span-based), `DrawHLine`, `DrawVLine`, and `FillRect` with hoisted bounds/clip checks and direct array access. Refactored `Table` and `Border` to utilize these methods. Benchmarks show up to 2.75x performance improvement for `FillRect` and ~2.4x for `DrawString`.

## 2026-02-25 - ConsoleRenderer Double Buffering
**Observation:** `ConsoleRenderer` performed a full redraw of the buffer on every frame, emitting redundant ANSI sequences and `Console.Write` calls. This caused excessive I/O and potential flicker. Latency was ~24us for a 2000-cell buffer with 10KB allocations per frame.
**Strategic Action:** Implemented double-buffering with a `Cell[] _backBuffer`. The renderer now diffs the current frame against the previous frame, batching contiguous changes into a `StringBuilder` and only emitting necessary updates. Optimized cursor positioning and color state changes. Result: "No Change" render latency reduced to ~20us with 98% memory allocation reduction (208B vs 10KB). Sparse updates (1 char) reduced allocation by 97%.

## 2026-02-26 - Grid Layout Optimization
**Observation:** `Grid.MeasureOverride` and `ArrangeOverride` utilized LINQ (`.Sum()`, `.Where().Sum()`) for calculating row/column dimensions. This caused significant per-pass allocations (delegate creation, enumerator boxing) and CPU overhead in the core layout loop, especially for dynamic sizing.
**Strategic Action:** Replaced LINQ queries with manual `foreach` loops and accumulator variables. Relocated the original implementation to `Tedd.TUI.Archive` for rigorous A/B testing. Result: 100% reduction in managed heap allocations (0 bytes vs ~300 bytes per measure) and ~1.7x faster execution (417ns vs 716ns) for implicit layouts.
