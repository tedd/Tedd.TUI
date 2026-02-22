```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
Intel Xeon Processor 2.30GHz, 1 CPU, 4 logical and 4 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3


```
| Method                | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------- |----------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| Legacy_ShortString    |  48.06 ns | 0.881 ns | 1.398 ns |  1.00 |    0.04 | 0.0020 |      48 B |        1.00 |
| Legacy_FullList       | 153.25 ns | 1.042 ns | 1.158 ns |  3.19 |    0.09 | 0.0033 |      80 B |        1.67 |
| Legacy_Ellipses       | 139.71 ns | 0.703 ns | 0.658 ns |  2.91 |    0.08 | 0.0041 |      96 B |        2.00 |
| Optimized_ShortString |  18.33 ns | 0.080 ns | 0.075 ns |  0.38 |    0.01 |      - |         - |        0.00 |
| Optimized_FullList    |  90.36 ns | 0.860 ns | 0.762 ns |  1.88 |    0.06 |      - |         - |        0.00 |
| Optimized_Ellipses    |  98.10 ns | 1.666 ns | 1.558 ns |  2.04 |    0.07 |      - |         - |        0.00 |
