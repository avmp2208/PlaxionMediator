```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-KENNUZ : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                    | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Dispatch_Mediator_50Types |   920.0 ns |  38.57 ns |  22.95 ns |  0.90 |    0.07 |    1 |      - |         - |          NA |
| Dispatch_Plaxion_50Types  | 1,027.2 ns | 122.89 ns |  81.28 ns |  1.01 |    0.11 |    1 |      - |         - |          NA |
| Dispatch_MediatR_50Types  | 5,597.0 ns | 872.97 ns | 577.42 ns |  5.48 |    0.68 |    2 | 1.0071 |   13200 B |          NA |
