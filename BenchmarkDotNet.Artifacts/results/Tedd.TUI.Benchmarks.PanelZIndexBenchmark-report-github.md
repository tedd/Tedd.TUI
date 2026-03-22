```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
Intel Xeon Processor 2.30GHz, 1 CPU, 4 logical and 4 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3


```
| Method                  | ChildCount | Mean        | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------ |----------- |------------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| **EnsureZSorted_Legacy**    | **10**         |    **499.2 ns** |   **9.50 ns** |  **10.17 ns** |  **1.00** |    **0.03** | **0.0229** |     **544 B** |        **1.00** |
| EnsureZSorted_Optimized | 10         |  1,363.1 ns |  26.65 ns |  28.52 ns |  2.73 |    0.08 | 0.0038 |     104 B |        0.19 |
|                         |            |             |           |           |       |         |        |           |             |
| **EnsureZSorted_Legacy**    | **50**         |  **2,021.9 ns** |  **30.87 ns** |  **33.03 ns** |  **1.00** |    **0.02** | **0.0610** |    **1504 B** |        **1.00** |
| EnsureZSorted_Optimized | 50         |  8,779.2 ns | 169.69 ns | 150.43 ns |  4.34 |    0.10 | 0.0153 |     424 B |        0.28 |
|                         |            |             |           |           |       |         |        |           |             |
| **EnsureZSorted_Legacy**    | **100**        |  **4,652.6 ns** |  **51.20 ns** |  **47.89 ns** |  **1.00** |    **0.01** | **0.1144** |    **2704 B** |        **1.00** |
| EnsureZSorted_Optimized | 100        | 19,160.6 ns | 261.62 ns | 218.47 ns |  4.12 |    0.06 | 0.0305 |     824 B |        0.30 |
