```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-UCAZZR : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                    | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Dispatch_Mediator_50Types |   851.2 ns |  7.24 ns |  4.31 ns |  0.99 |    0.01 |    1 |      - |         - |          NA |
| Dispatch_Plaxion_50Types  |   860.4 ns |  4.68 ns |  2.45 ns |  1.00 |    0.00 |    1 |      - |         - |          NA |
| Dispatch_MediatR_50Types  | 4,703.1 ns | 80.03 ns | 52.93 ns |  5.47 |    0.06 |    2 | 1.0071 |   13200 B |          NA |
