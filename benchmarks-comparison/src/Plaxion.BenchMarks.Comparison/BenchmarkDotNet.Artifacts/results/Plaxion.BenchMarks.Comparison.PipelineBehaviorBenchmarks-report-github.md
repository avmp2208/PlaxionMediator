```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-YXHNZT : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                    | Mean        | Error      | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |------------:|-----------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| Send_Plaxion_0Behaviors   |    19.08 ns |   0.608 ns |   0.402 ns |   1.00 |    0.03 |    1 |      - |         - |          NA |
| Send_Mediator_0Behaviors  |    28.41 ns |   1.362 ns |   0.901 ns |   1.49 |    0.05 |    2 |      - |         - |          NA |
| Send_MediatR_0Behaviors   |   104.56 ns |   7.319 ns |   4.841 ns |   5.48 |    0.27 |    3 | 0.0201 |     264 B |          NA |
| Send_Mediator_1Behavior   |   136.28 ns |   4.129 ns |   2.731 ns |   7.14 |    0.20 |    4 | 0.0098 |     128 B |          NA |
| Send_Plaxion_1Behavior    |   176.81 ns |  61.522 ns |  40.693 ns |   9.27 |    2.04 |    5 | 0.0134 |     176 B |          NA |
| Send_MediatR_1Behavior    |   276.17 ns |   8.241 ns |   4.904 ns |  14.48 |    0.38 |    6 | 0.0494 |     648 B |          NA |
| Send_Mediator_5Behaviors  |   517.40 ns | 179.684 ns | 118.850 ns |  27.13 |    5.97 |    7 | 0.0486 |     640 B |          NA |
| Send_Mediator_10Behaviors |   613.18 ns |  22.374 ns |  13.314 ns |  32.15 |    0.93 |    7 | 0.0973 |    1280 B |          NA |
| Send_Plaxion_5Behaviors   |   641.06 ns |  20.572 ns |  12.242 ns |  33.61 |    0.91 |    7 | 0.0525 |     688 B |          NA |
| Send_MediatR_5Behaviors   |   838.16 ns |  39.359 ns |  26.033 ns |  43.94 |    1.58 |    8 | 0.1450 |    1896 B |          NA |
| Send_Plaxion_10Behaviors  | 1,319.68 ns |  36.391 ns |  24.071 ns |  69.19 |    1.85 |    9 | 0.1011 |    1328 B |          NA |
| Send_Mediator_20Behaviors | 1,331.71 ns |  23.970 ns |  12.537 ns |  69.82 |    1.55 |    9 | 0.1945 |    2560 B |          NA |
| Send_MediatR_10Behaviors  | 1,705.21 ns |  86.396 ns |  57.146 ns |  89.40 |    3.38 |   10 | 0.2642 |    3456 B |          NA |
| Send_Plaxion_20Behaviors  | 3,254.99 ns | 260.461 ns | 172.279 ns | 170.65 |    9.28 |   11 | 0.1984 |    2608 B |          NA |
| Send_MediatR_20Behaviors  | 3,581.33 ns | 269.749 ns | 160.523 ns | 187.76 |    8.84 |   11 | 0.4997 |    6576 B |          NA |
