```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
Intel Xeon Processor 2.30GHz, 1 CPU, 4 logical and 4 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3


```
| Method                     | Mean     | Error   | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |---------:|--------:|--------:|------:|-------:|----------:|------------:|
| Legacy_Measure_Implicit    | 716.3 ns | 2.65 ns | 2.35 ns |  1.00 | 0.0124 |     304 B |        1.00 |
| Optimized_Measure_Implicit | 417.4 ns | 0.36 ns | 0.30 ns |  0.58 |      - |         - |        0.00 |
| Legacy_Measure_Star        | 883.2 ns | 2.87 ns | 2.54 ns |  1.23 | 0.0086 |     224 B |        0.74 |
| Optimized_Measure_Star     | 557.9 ns | 0.74 ns | 0.66 ns |  0.78 |      - |         - |        0.00 |
