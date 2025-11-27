```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.7171)
Intel Core i7-10870H CPU 2.20GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 9.0.11 (9.0.1125.51716), X64 RyuJIT AVX2
  Job-OODWEW : .NET 9.0.11 (9.0.1125.51716), X64 RyuJIT AVX2

IterationCount=10  WarmupCount=3  

```
| Method          | Mean      | Error     | StdDev    | Gen0   | Allocated |
|---------------- |----------:|----------:|----------:|-------:|----------:|
| SimpleResponse  |  58.85 ns |  5.391 ns |  3.208 ns | 0.0381 |     320 B |
| ComplexResponse | 141.21 ns | 19.518 ns | 12.910 ns | 0.0772 |     648 B |
| WithState       |  65.12 ns |  9.891 ns |  5.886 ns | 0.0440 |     368 B |
