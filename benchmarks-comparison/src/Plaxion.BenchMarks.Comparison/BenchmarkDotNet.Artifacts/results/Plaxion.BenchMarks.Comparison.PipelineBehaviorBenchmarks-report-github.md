```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-NZEAKA : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                    | Mean        | Error      | StdDev     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |------------:|-----------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Send_Mediator_0Behaviors  |    19.77 ns |   0.622 ns |   0.370 ns |  0.38 |    0.03 |    1 |      - |         - |          NA |
| Send_Plaxion_0Behaviors   |    52.84 ns |   6.664 ns |   4.408 ns |  1.01 |    0.11 |    2 |      - |         - |          NA |
| Send_MediatR_0Behaviors   |    76.25 ns |   3.865 ns |   2.300 ns |  1.45 |    0.12 |    3 | 0.0201 |     264 B |          NA |
| Send_Mediator_1Behavior   |   103.62 ns |   4.074 ns |   2.695 ns |  1.97 |    0.16 |    4 | 0.0098 |     128 B |          NA |
| Send_Plaxion_1Behavior    |   149.68 ns |  15.999 ns |  10.582 ns |  2.85 |    0.30 |    5 | 0.0098 |     128 B |          NA |
| Send_MediatR_1Behavior    |   219.56 ns |  20.832 ns |  13.779 ns |  4.18 |    0.42 |    6 | 0.0494 |     648 B |          NA |
| Send_Mediator_5Behaviors  |   462.03 ns |  15.262 ns |   9.082 ns |  8.80 |    0.72 |    7 | 0.0486 |     640 B |          NA |
| Send_Plaxion_5Behaviors   |   536.98 ns |  25.582 ns |  15.223 ns | 10.23 |    0.86 |    7 | 0.0486 |     640 B |          NA |
| Send_MediatR_5Behaviors   |   696.30 ns |  25.663 ns |  13.422 ns | 13.26 |    1.08 |    8 | 0.1450 |    1896 B |          NA |
| Send_Mediator_10Behaviors |   952.44 ns |  55.551 ns |  36.743 ns | 18.14 |    1.59 |    9 | 0.0973 |    1280 B |          NA |
| Send_Plaxion_10Behaviors  |   968.66 ns |  41.951 ns |  27.748 ns | 18.45 |    1.55 |    9 | 0.0973 |    1280 B |          NA |
| Send_MediatR_10Behaviors  | 1,424.84 ns |  56.172 ns |  33.427 ns | 27.14 |    2.25 |   10 | 0.2632 |    3456 B |          NA |
| Send_Mediator_20Behaviors | 2,029.22 ns | 115.862 ns |  68.948 ns | 38.65 |    3.32 |   11 | 0.1945 |    2560 B |          NA |
| Send_Plaxion_20Behaviors  | 2,094.38 ns | 401.704 ns | 265.702 ns | 39.89 |    5.79 |   11 | 0.1945 |    2560 B |          NA |
| Send_MediatR_20Behaviors  | 2,920.35 ns |  72.892 ns |  38.124 ns | 55.62 |    4.49 |   12 | 0.4997 |    6576 B |          NA |
