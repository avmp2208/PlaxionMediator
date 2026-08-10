```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-GDXDUG : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                    | Mean       | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Dispatch_Mediator_50Types |   872.2 ns |    21.20 ns |  14.02 ns |  0.96 |    0.06 |    1 |      - |         - |          NA |
| Dispatch_Plaxion_50Types  |   916.5 ns |   102.65 ns |  67.90 ns |  1.00 |    0.10 |    1 |      - |         - |          NA |
| Dispatch_MediatR_50Types  | 5,771.3 ns | 1,167.44 ns | 694.73 ns |  6.33 |    0.84 |    2 | 1.0071 |   13200 B |          NA |
