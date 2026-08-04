```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-JRUHVO : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                    | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Dispatch_Mediator_50Types |   844.1 ns |  4.85 ns |  2.89 ns |  0.97 |    0.00 |    1 |      - |         - |          NA |
| Dispatch_Plaxion_50Types  |   870.3 ns |  1.04 ns |  0.55 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Dispatch_MediatR_50Types  | 4,689.4 ns | 66.43 ns | 39.53 ns |  5.39 |    0.04 |    2 | 1.0071 |   13200 B |          NA |
