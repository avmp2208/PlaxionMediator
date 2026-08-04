```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-FAALTU : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                    | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Dispatch_Mediator_50Types |   966.8 ns |  27.24 ns |  18.02 ns |  0.45 |    0.02 |    1 |      - |         - |          NA |
| Dispatch_Plaxion_50Types  | 2,141.6 ns | 102.36 ns |  67.70 ns |  1.00 |    0.04 |    2 |      - |         - |          NA |
| Dispatch_MediatR_50Types  | 5,739.5 ns | 481.89 ns | 318.74 ns |  2.68 |    0.16 |    3 | 1.0071 |   13200 B |          NA |
