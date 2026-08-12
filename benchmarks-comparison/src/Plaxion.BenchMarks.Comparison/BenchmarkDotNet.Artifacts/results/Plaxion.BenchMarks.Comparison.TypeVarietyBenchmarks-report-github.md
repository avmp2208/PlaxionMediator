```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-TWNJOK : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                    | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Dispatch_Mediator_50Types |   921.0 ns |  26.81 ns |  17.73 ns |  0.99 |    0.02 |    1 |      - |         - |          NA |
| Dispatch_Plaxion_50Types  |   935.0 ns |   9.24 ns |   5.50 ns |  1.00 |    0.01 |    1 |      - |         - |          NA |
| Dispatch_MediatR_50Types  | 5,275.9 ns | 608.23 ns | 402.31 ns |  5.64 |    0.41 |    2 | 1.0071 |   13200 B |          NA |
