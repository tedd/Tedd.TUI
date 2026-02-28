```

BenchmarkDotNet v0.13.12, Ubuntu 24.04.3 LTS (Noble Numbat)
Intel Xeon Processor 2.30GHz, 1 CPU, 4 logical and 4 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX2
  DefaultJob : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX2


```
| Method       | Mean     | Error    | StdDev   | Ratio | Allocated | Alloc Ratio |
|------------- |---------:|---------:|---------:|------:|----------:|------------:|
| LegacyRender | 17.47 μs | 0.081 μs | 0.076 μs |  1.00 |     104 B |        1.00 |
| ModernRender | 12.97 μs | 0.043 μs | 0.040 μs |  0.74 |         - |        0.00 |
