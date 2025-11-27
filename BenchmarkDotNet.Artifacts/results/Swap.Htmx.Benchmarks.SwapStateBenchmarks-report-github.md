```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.7171)
Intel Core i7-10870H CPU 2.20GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 9.0.11 (9.0.1125.51716), X64 RyuJIT AVX2
  Job-OODWEW : .NET 9.0.11 (9.0.1125.51716), X64 RyuJIT AVX2

IterationCount=10  WarmupCount=3  

```
| Method            | Mean        | Error      | StdDev     | Gen0   | Allocated |
|------------------ |------------:|-----------:|-----------:|-------:|----------:|
| GetStateValues    | 722.2344 ns | 59.0796 ns | 35.1573 ns | 0.1268 |    1064 B |
| ToQueryString     |   1.1487 ns |  0.2054 ns |  0.1222 ns |      - |         - |
| PropertyUpdate    |   0.0351 ns |  0.0316 ns |  0.0188 ns |      - |         - |
| CreateAndPopulate | 750.3117 ns | 54.0356 ns | 35.7412 ns | 0.1431 |    1200 B |
