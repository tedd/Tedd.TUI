```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
Intel Xeon Processor 2.30GHz, 1 CPU, 4 logical and 4 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3


```
| Method                       | Mean     | Error    | StdDev   | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------------- |---------:|---------:|---------:|------:|-------:|-------:|----------:|------------:|
| Archive_Render_Full          | 24.05 μs | 0.131 μs | 0.122 μs |  1.00 | 1.2207 | 0.0916 |   29448 B |       1.000 |
| Optimized_Render_Full        | 21.53 μs | 0.103 μs | 0.086 μs |  0.90 | 1.0986 | 0.0916 |   26176 B |       0.889 |
| Archive_Render_NoChange      | 19.54 μs | 0.058 μs | 0.051 μs |  0.81 |      - |      - |     208 B |       0.007 |
| Optimized_Render_NoChange    | 19.08 μs | 0.087 μs | 0.082 μs |  0.79 |      - |      - |         - |       0.000 |
| Archive_Render_SmallChange   | 20.91 μs | 0.063 μs | 0.056 μs |  0.87 |      - |      - |     256 B |       0.009 |
| Optimized_Render_SmallChange | 19.59 μs | 0.349 μs | 0.309 μs |  0.81 |      - |      - |         - |       0.000 |
