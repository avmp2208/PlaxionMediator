```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-VIJZNV : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                    | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |---------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Dispatch_Mediator_50Types | 1.144 μs | 0.0360 μs | 0.0238 μs |  0.44 |    0.02 |    1 |      - |         - |          NA |
| Dispatch_Plaxion_50Types  | 2.614 μs | 0.1313 μs | 0.0868 μs |  1.00 |    0.04 |    2 |      - |         - |          NA |
| Dispatch_MediatR_50Types  | 7.534 μs | 0.2375 μs | 0.1414 μs |  2.88 |    0.10 |    3 | 1.0071 |   13200 B |          NA |
