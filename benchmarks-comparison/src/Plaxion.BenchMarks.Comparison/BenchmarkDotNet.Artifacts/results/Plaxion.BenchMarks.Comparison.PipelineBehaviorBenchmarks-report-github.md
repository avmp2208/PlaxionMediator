```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-VPFTQC : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                    | Mean        | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------- |------------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Send_Mediator_0Behaviors  |    16.76 ns |   0.972 ns |  0.579 ns |  0.82 |    0.03 |    1 |      - |      - |         - |          NA |
| Send_Plaxion_0Behaviors   |    20.43 ns |   0.641 ns |  0.424 ns |  1.00 |    0.03 |    1 |      - |      - |         - |          NA |
| Send_MediatR_0Behaviors   |    56.74 ns |   3.696 ns |  2.445 ns |  2.78 |    0.13 |    2 | 0.0201 |      - |     264 B |          NA |
| Send_Mediator_1Behavior   |    72.58 ns |   4.329 ns |  2.864 ns |  3.55 |    0.15 |    3 | 0.0098 |      - |     128 B |          NA |
| Send_Plaxion_1Behavior    |   118.34 ns |   2.186 ns |  1.301 ns |  5.80 |    0.13 |    4 | 0.0134 |      - |     176 B |          NA |
| Send_MediatR_1Behavior    |   165.00 ns |   7.222 ns |  4.298 ns |  8.08 |    0.25 |    5 | 0.0494 |      - |     648 B |          NA |
| Send_Mediator_5Behaviors  |   328.02 ns |  27.149 ns | 16.156 ns | 16.07 |    0.81 |    6 | 0.0486 |      - |     640 B |          NA |
| Send_Plaxion_5Behaviors   |   391.31 ns |  10.291 ns |  5.382 ns | 19.16 |    0.45 |    6 | 0.0525 |      - |     688 B |          NA |
| Send_MediatR_5Behaviors   |   471.82 ns |  22.653 ns | 14.984 ns | 23.11 |    0.83 |    6 | 0.1450 |      - |    1896 B |          NA |
| Send_Mediator_10Behaviors |   603.51 ns |  17.405 ns | 11.512 ns | 29.56 |    0.79 |    7 | 0.0973 |      - |    1280 B |          NA |
| Send_Plaxion_10Behaviors  |   761.79 ns |  19.309 ns | 11.490 ns | 37.31 |    0.90 |    8 | 0.1011 |      - |    1328 B |          NA |
| Send_MediatR_10Behaviors  |   832.34 ns |  16.260 ns |  8.504 ns | 40.76 |    0.89 |    8 | 0.2642 |      - |    3456 B |          NA |
| Send_Mediator_20Behaviors | 1,320.21 ns |  40.737 ns | 24.242 ns | 64.66 |    1.69 |    9 | 0.1945 |      - |    2560 B |          NA |
| Send_Plaxion_20Behaviors  | 1,553.08 ns |  66.156 ns | 39.368 ns | 76.06 |    2.36 |    9 | 0.1984 |      - |    2608 B |          NA |
| Send_MediatR_20Behaviors  | 1,795.03 ns | 121.467 ns | 80.343 ns | 87.91 |    4.13 |    9 | 0.5016 | 0.0019 |    6576 B |          NA |
