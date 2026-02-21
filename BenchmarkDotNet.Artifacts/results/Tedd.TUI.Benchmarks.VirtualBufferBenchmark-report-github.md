```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
Intel Xeon Processor 2.30GHz, 1 CPU, 4 logical and 4 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3


```
| Method                      | Mean     | Error    | StdDev   | Ratio | Allocated | Alloc Ratio |
|---------------------------- |---------:|---------:|---------:|------:|----------:|------------:|
| Legacy_SetPixel_NoClip      | 40.50 μs | 0.063 μs | 0.055 μs |  1.00 |         - |          NA |
| Optimized_SetPixel_NoClip   | 35.96 μs | 0.033 μs | 0.026 μs |  0.89 |         - |          NA |
| Legacy_SetPixel_WithClip    | 78.53 μs | 0.248 μs | 0.220 μs |  1.94 |         - |          NA |
| Optimized_SetPixel_WithClip | 36.46 μs | 0.114 μs | 0.101 μs |  0.90 |         - |          NA |
