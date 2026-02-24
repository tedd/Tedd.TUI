## 2026-02-22 - Table Pagination Optimization
**Observation:** `Table.GetPaginationString` was allocating a `string` on every render (or cache miss) to display pagination. Benchmark showed ~48B alloc per call for short strings and ~153ns latency. This allocation pressure, although small per call, accumulates in the rendering loop.
**Strategic Action:** Refactored `GetPaginationString` to use `stackalloc char[256]` and return `int` (chars written). Updated `RenderPagination` and `HandlePaginationClick` to use this zero-allocation method. Result: 0 bytes allocated and ~18-98ns latency (38-70% reduction).

## 2026-02-24 - VirtualBuffer Bulk Rendering Optimization
**Observation:** `VirtualBuffer` operations relied heavily on `SetPixel` inside tight loops (e.g., in `Table.Render` and `Border.Render`). This incurred per-pixel bounds checking, clipping, and method call overhead.
**Strategic Action:** Implemented `DrawString` (Span-based), `DrawHLine`, `DrawVLine`, and `FillRect` with hoisted bounds/clip checks and direct array access. Refactored `Table` and `Border` to utilize these methods. Benchmarks show up to 2.75x performance improvement for `FillRect` and ~2.4x for `DrawString`.
