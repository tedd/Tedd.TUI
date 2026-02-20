```

BenchmarkDotNet v0.13.12, Ubuntu 24.04.3 LTS (Noble Numbat)
Intel Xeon Processor 2.30GHz, 1 CPU, 4 logical and 4 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX2
  DefaultJob : .NET 10.0.0 (10.0.25.52411), X64 RyuJIT AVX2


```
| Method      | Mean     | Error     | StdDev    | Allocated |
|------------ |---------:|----------:|----------:|----------:|
| ShortString | 1.492 ns | 0.0293 ns | 0.0274 ns |         - |
| FullList    | 3.998 ns | 0.0171 ns | 0.0151 ns |         - |
| Ellipses    | 4.061 ns | 0.0257 ns | 0.0240 ns |         - |
