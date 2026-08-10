```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-ZPGVYI : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                    | Mean        | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------- |------------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Send_Mediator_0Behaviors  |    15.57 ns |  0.398 ns |  0.263 ns |  0.78 |    0.04 |    1 |      - |      - |         - |          NA |
| Send_Plaxion_0Behaviors   |    20.08 ns |  1.640 ns |  1.085 ns |  1.00 |    0.07 |    2 |      - |      - |         - |          NA |
| Send_MediatR_0Behaviors   |    61.24 ns |  4.986 ns |  3.298 ns |  3.06 |    0.22 |    3 | 0.0201 |      - |     264 B |          NA |
| Send_Mediator_1Behavior   |    68.79 ns |  1.071 ns |  0.637 ns |  3.43 |    0.18 |    3 | 0.0098 |      - |     128 B |          NA |
| Send_Plaxion_1Behavior    |   113.80 ns |  2.931 ns |  1.939 ns |  5.68 |    0.30 |    4 | 0.0098 |      - |     128 B |          NA |
| Send_MediatR_1Behavior    |   168.23 ns |  4.352 ns |  2.590 ns |  8.40 |    0.44 |    5 | 0.0494 |      - |     648 B |          NA |
| Send_Mediator_5Behaviors  |   322.01 ns | 10.855 ns |  6.460 ns | 16.07 |    0.87 |    6 | 0.0486 |      - |     640 B |          NA |
| Send_Plaxion_5Behaviors   |   384.68 ns | 10.186 ns |  6.737 ns | 19.20 |    1.02 |    6 | 0.0486 |      - |     640 B |          NA |
| Send_MediatR_5Behaviors   |   481.04 ns | 12.416 ns |  7.388 ns | 24.01 |    1.27 |    7 | 0.1450 |      - |    1896 B |          NA |
| Send_Mediator_10Behaviors |   617.86 ns | 12.648 ns |  8.366 ns | 30.84 |    1.61 |    8 | 0.0973 |      - |    1280 B |          NA |
| Send_Plaxion_10Behaviors  |   760.17 ns | 15.829 ns | 10.470 ns | 37.95 |    1.98 |    9 | 0.0973 |      - |    1280 B |          NA |
| Send_MediatR_10Behaviors  |   867.83 ns | 65.331 ns | 43.212 ns | 43.32 |    3.01 |   10 | 0.2632 |      - |    3456 B |          NA |
| Send_Mediator_20Behaviors | 1,340.84 ns | 38.747 ns | 25.628 ns | 66.93 |    3.60 |   11 | 0.1945 |      - |    2560 B |          NA |
| Send_Plaxion_20Behaviors  | 1,512.33 ns | 23.561 ns | 14.021 ns | 75.49 |    3.88 |   11 | 0.1945 |      - |    2560 B |          NA |
| Send_MediatR_20Behaviors  | 1,842.75 ns | 75.199 ns | 49.740 ns | 91.99 |    5.22 |   11 | 0.5016 | 0.0019 |    6576 B |          NA |
