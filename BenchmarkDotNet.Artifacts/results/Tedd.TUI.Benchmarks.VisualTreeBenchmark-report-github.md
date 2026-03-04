```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
Intel Xeon Processor 2.30GHz, 1 CPU, 4 logical and 4 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3


```
| Method                | Mean     | Error   | StdDev  | Ratio | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------- |---------:|--------:|--------:|------:|-------:|-------:|----------:|------------:|
| IterativeYield        | 119.5 μs | 0.75 μs | 1.15 μs |  1.00 |      - |      - |    4280 B |        1.00 |
| RecursiveList         | 122.2 μs | 0.92 μs | 0.86 μs |  1.02 | 2.9297 | 0.2441 |   70240 B |       16.41 |
| OptimizedEnumerator   | 118.7 μs | 1.12 μs | 1.05 μs |  0.99 |      - |      - |    4208 B |        0.98 |
| CurrentImplementation | 121.4 μs | 1.07 μs | 0.95 μs |  1.02 |      - |      - |     144 B |        0.03 |
