```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-ASUFOU : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                    | Mean       | Error     | StdDev   | Ratio | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |-----------:|----------:|---------:|------:|-----:|-------:|----------:|------------:|
| Dispatch_Mediator_50Types |   905.2 ns |  18.60 ns | 12.30 ns |  0.14 |    1 |      - |         - |          NA |
| Dispatch_MediatR_50Types  | 4,833.2 ns | 107.05 ns | 70.81 ns |  0.75 |    2 | 1.0071 |   13200 B |          NA |
| Dispatch_Plaxion_50Types  | 6,434.9 ns |  53.11 ns | 35.13 ns |  1.00 |    3 |      - |         - |          NA |
