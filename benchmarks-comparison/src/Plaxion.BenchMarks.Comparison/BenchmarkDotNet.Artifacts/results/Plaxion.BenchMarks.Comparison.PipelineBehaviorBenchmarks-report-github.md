```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-ASUFOU : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                    | Mean        | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------- |------------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Send_Mediator_0Behaviors  |    16.89 ns |  0.181 ns |  0.120 ns |  0.35 |    0.00 |    1 |      - |      - |         - |          NA |
| Send_Plaxion_0Behaviors   |    48.39 ns |  0.553 ns |  0.366 ns |  1.00 |    0.01 |    2 |      - |      - |         - |          NA |
| Send_MediatR_0Behaviors   |    53.61 ns |  1.442 ns |  0.954 ns |  1.11 |    0.02 |    3 | 0.0201 |      - |     264 B |          NA |
| Send_Mediator_1Behavior   |    67.84 ns |  1.387 ns |  0.725 ns |  1.40 |    0.02 |    4 | 0.0098 |      - |     128 B |          NA |
| Send_MediatR_1Behavior    |   165.63 ns | 11.578 ns |  7.658 ns |  3.42 |    0.15 |    5 | 0.0494 |      - |     648 B |          NA |
| Send_Plaxion_1Behavior    |   167.58 ns |  1.972 ns |  1.173 ns |  3.46 |    0.03 |    5 | 0.0329 |      - |     432 B |          NA |
| Send_Mediator_5Behaviors  |   312.53 ns | 10.028 ns |  6.633 ns |  6.46 |    0.14 |    6 | 0.0486 |      - |     640 B |          NA |
| Send_MediatR_5Behaviors   |   481.46 ns | 23.821 ns | 14.176 ns |  9.95 |    0.29 |    7 | 0.1450 |      - |    1896 B |          NA |
| Send_Plaxion_5Behaviors   |   547.86 ns | 16.777 ns |  9.984 ns | 11.32 |    0.21 |    7 | 0.1059 |      - |    1392 B |          NA |
| Send_Mediator_10Behaviors |   603.98 ns |  9.482 ns |  5.642 ns | 12.48 |    0.14 |    7 | 0.0973 |      - |    1280 B |          NA |
| Send_MediatR_10Behaviors  |   800.12 ns | 23.369 ns | 13.906 ns | 16.54 |    0.30 |    8 | 0.2642 |      - |    3456 B |          NA |
| Send_Plaxion_10Behaviors  |   948.02 ns | 13.820 ns |  9.141 ns | 19.59 |    0.23 |    8 | 0.1974 |      - |    2592 B |          NA |
| Send_Mediator_20Behaviors | 1,275.79 ns | 23.087 ns | 12.075 ns | 26.37 |    0.30 |    9 | 0.1945 |      - |    2560 B |          NA |
| Send_MediatR_20Behaviors  | 1,699.70 ns | 75.365 ns | 49.849 ns | 35.13 |    1.01 |   10 | 0.5016 | 0.0019 |    6576 B |          NA |
| Send_Plaxion_20Behaviors  | 2,015.69 ns | 24.899 ns | 14.817 ns | 41.66 |    0.42 |   10 | 0.3815 |      - |    4992 B |          NA |
