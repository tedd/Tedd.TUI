```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
Intel Xeon Processor 2.30GHz, 1 CPU, 4 logical and 4 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3


```
| Method                     | Mean        | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|--------------------------- |------------:|---------:|---------:|------:|--------:|----------:|------------:|
| Baseline_DrawString_Short  |    37.03 ns | 0.154 ns | 0.120 ns |  1.00 |    0.00 |         - |          NA |
| Optimized_DrawString_Short |    15.51 ns | 0.029 ns | 0.027 ns |  0.42 |    0.00 |         - |          NA |
| Baseline_DrawString_Long   |   434.82 ns | 0.614 ns | 0.574 ns | 11.74 |    0.04 |         - |          NA |
| Optimized_DrawString_Long  |   246.72 ns | 0.432 ns | 0.404 ns |  6.66 |    0.02 |         - |          NA |
| Baseline_DrawHLine         |   193.63 ns | 0.337 ns | 0.315 ns |  5.23 |    0.02 |         - |          NA |
| Optimized_DrawHLine        |   113.42 ns | 0.134 ns | 0.125 ns |  3.06 |    0.01 |         - |          NA |
| Baseline_DrawVLine         |    87.02 ns | 0.191 ns | 0.179 ns |  2.35 |    0.01 |         - |          NA |
| Optimized_DrawVLine        |    63.62 ns | 0.133 ns | 0.118 ns |  1.72 |    0.01 |         - |          NA |
| Baseline_FillRect          | 3,195.08 ns | 6.971 ns | 6.180 ns | 86.29 |    0.31 |         - |          NA |
| Optimized_FillRect         | 1,159.31 ns | 2.519 ns | 2.357 ns | 31.31 |    0.11 |         - |          NA |
