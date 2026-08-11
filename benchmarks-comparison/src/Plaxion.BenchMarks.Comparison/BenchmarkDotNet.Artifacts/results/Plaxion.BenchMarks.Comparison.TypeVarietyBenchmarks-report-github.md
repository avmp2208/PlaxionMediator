```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-SLFTPX : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  ShortRun   : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

LaunchCount=1  WarmupCount=3  

```
| Method                    | Job        | IterationCount | Mean       | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |----------- |--------------- |-----------:|------------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Dispatch_Mediator_50Types | Job-SLFTPX | 10             |   985.5 ns |   103.09 ns |  68.19 ns |  0.97 |    0.10 |    1 |      - |         - |          NA |
| Dispatch_Plaxion_50Types  | Job-SLFTPX | 10             | 1,016.9 ns |   122.69 ns |  81.15 ns |  1.01 |    0.11 |    1 |      - |         - |          NA |
| Dispatch_MediatR_50Types  | Job-SLFTPX | 10             | 5,920.6 ns | 1,039.55 ns | 687.60 ns |  5.86 |    0.79 |    2 | 1.0071 |   13200 B |          NA |
|                           |            |                |            |             |           |       |         |      |        |           |             |
| Dispatch_Plaxion_50Types  | ShortRun   | 3              |   908.0 ns |    71.56 ns |   3.92 ns |  1.00 |    0.01 |    1 |      - |         - |          NA |
| Dispatch_Mediator_50Types | ShortRun   | 3              | 1,013.5 ns | 1,964.08 ns | 107.66 ns |  1.12 |    0.10 |    1 |      - |         - |          NA |
| Dispatch_MediatR_50Types  | ShortRun   | 3              | 5,489.9 ns | 7,813.84 ns | 428.30 ns |  6.05 |    0.41 |    2 | 1.0071 |   13200 B |          NA |
