```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
Intel Xeon Processor 2.30GHz, 1 CPU, 4 logical and 4 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3


```
| Method                                | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------------------------- |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| Legacy_Measure_ImplicitDefinitions    | 844.0 ns |  7.75 ns |  7.25 ns |  1.00 |    0.01 | 0.0238 |     576 B |        1.00 |
| Optimized_Measure_ImplicitDefinitions | 710.3 ns | 14.10 ns | 12.50 ns |  0.84 |    0.02 | 0.0105 |     264 B |        0.46 |
