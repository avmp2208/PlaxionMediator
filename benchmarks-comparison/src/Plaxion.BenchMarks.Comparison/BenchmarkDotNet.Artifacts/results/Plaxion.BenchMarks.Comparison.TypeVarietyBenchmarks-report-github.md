```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-YXHNZT : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                    | Mean       | Error    | StdDev   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|---------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| Dispatch_Plaxion_50Types  |   877.9 ns | 19.23 ns | 12.72 ns |  1.00 |    0.02 |    1 |      - |         - |          NA |
| Dispatch_Mediator_50Types |   901.8 ns | 13.57 ns |  8.98 ns |  1.03 |    0.02 |    1 |      - |         - |          NA |
| Dispatch_MediatR_50Types  | 4,702.6 ns | 96.12 ns | 63.58 ns |  5.36 |    0.10 |    2 | 1.0071 |   13200 B |          NA |
