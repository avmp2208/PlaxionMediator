```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-JRUHVO : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                    | Mean        | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------- |------------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Send_Mediator_0Behaviors  |    16.20 ns |  0.302 ns |  0.200 ns |  0.73 |    0.01 |    1 |      - |      - |         - |          NA |
| Send_Plaxion_0Behaviors   |    22.19 ns |  0.170 ns |  0.089 ns |  1.00 |    0.01 |    2 |      - |      - |         - |          NA |
| Send_MediatR_0Behaviors   |    56.02 ns |  3.462 ns |  2.290 ns |  2.52 |    0.10 |    3 | 0.0201 |      - |     264 B |          NA |
| Send_Mediator_1Behavior   |    68.47 ns |  1.451 ns |  0.863 ns |  3.09 |    0.04 |    3 | 0.0098 |      - |     128 B |          NA |
| Send_Plaxion_1Behavior    |   120.69 ns |  1.397 ns |  0.831 ns |  5.44 |    0.04 |    4 | 0.0098 |      - |     128 B |          NA |
| Send_MediatR_1Behavior    |   165.30 ns |  6.739 ns |  4.010 ns |  7.45 |    0.17 |    5 | 0.0494 |      - |     648 B |          NA |
| Send_Mediator_5Behaviors  |   309.10 ns |  5.830 ns |  3.469 ns | 13.93 |    0.16 |    6 | 0.0486 |      - |     640 B |          NA |
| Send_Plaxion_5Behaviors   |   388.76 ns |  7.209 ns |  4.768 ns | 17.52 |    0.22 |    7 | 0.0486 |      - |     640 B |          NA |
| Send_MediatR_5Behaviors   |   479.34 ns | 13.606 ns |  8.999 ns | 21.61 |    0.40 |    8 | 0.1450 |      - |    1896 B |          NA |
| Send_Mediator_10Behaviors |   603.20 ns | 13.646 ns |  9.026 ns | 27.19 |    0.40 |    9 | 0.0973 |      - |    1280 B |          NA |
| Send_Plaxion_10Behaviors  |   747.55 ns |  6.795 ns |  3.554 ns | 33.70 |    0.20 |    9 | 0.0973 |      - |    1280 B |          NA |
| Send_MediatR_10Behaviors  |   809.17 ns | 13.999 ns |  7.322 ns | 36.47 |    0.34 |    9 | 0.2642 |      - |    3456 B |          NA |
| Send_Mediator_20Behaviors | 1,316.00 ns | 35.166 ns | 23.260 ns | 59.32 |    1.03 |   10 | 0.1945 |      - |    2560 B |          NA |
| Send_Plaxion_20Behaviors  | 1,466.63 ns | 23.509 ns | 15.550 ns | 66.11 |    0.71 |   11 | 0.1945 |      - |    2560 B |          NA |
| Send_MediatR_20Behaviors  | 1,708.21 ns | 53.505 ns | 35.390 ns | 77.00 |    1.55 |   12 | 0.5016 | 0.0019 |    6576 B |          NA |
