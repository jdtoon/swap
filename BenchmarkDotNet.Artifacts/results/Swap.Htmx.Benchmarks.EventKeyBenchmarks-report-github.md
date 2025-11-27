```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26100.7171)
Intel Core i7-10870H CPU 2.20GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 9.0.11 (9.0.1125.51716), X64 RyuJIT AVX2
  Job-OODWEW : .NET 9.0.11 (9.0.1125.51716), X64 RyuJIT AVX2

IterationCount=10  WarmupCount=3  

```
| Method                | Mean       | Error     | StdDev    | Median     | Gen0   | Allocated |
|---------------------- |-----------:|----------:|----------:|-----------:|-------:|----------:|
| CreateEventKey        |  0.0349 ns | 0.1244 ns | 0.0741 ns |  0.0000 ns |      - |         - |
| EventKeyEquality      |  0.1139 ns | 0.1597 ns | 0.1057 ns |  0.0846 ns |      - |         - |
| EventKeyHashCode      | 12.2890 ns | 1.0912 ns | 0.6494 ns | 12.1256 ns |      - |         - |
| ParameterizedEventKey | 34.5744 ns | 2.1182 ns | 1.4010 ns | 34.9443 ns | 0.0076 |      64 B |
