```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-HJCBLN : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                    | Mean       | Error    | StdDev   | Ratio | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|---------:|---------:|------:|-----:|-------:|----------:|------------:|
| Dispatch_Mediator_50Types |   901.5 ns |  8.12 ns |  5.37 ns |  0.26 |    1 |      - |         - |          NA |
| Dispatch_Plaxion_50Types  | 3,409.9 ns | 29.95 ns | 19.81 ns |  1.00 |    2 |      - |         - |          NA |
| Dispatch_MediatR_50Types  | 4,741.2 ns | 38.51 ns | 22.92 ns |  1.39 |    3 | 1.0071 |   13200 B |          NA |
