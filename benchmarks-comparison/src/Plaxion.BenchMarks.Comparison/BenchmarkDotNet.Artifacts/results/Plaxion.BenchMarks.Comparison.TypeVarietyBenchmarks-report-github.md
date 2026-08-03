```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-CFKAWA : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                    | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|----------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Dispatch_Mediator_50Types |   894.3 ns |   6.93 ns |  4.13 ns |  0.22 |    0.00 |    1 |      - |         - |          NA |
| Dispatch_Plaxion_50Types  | 4,120.6 ns | 107.34 ns | 71.00 ns |  1.00 |    0.02 |    2 |      - |         - |          NA |
| Dispatch_MediatR_50Types  | 5,105.4 ns | 153.05 ns | 91.08 ns |  1.24 |    0.03 |    2 | 1.0071 |   13200 B |          NA |
