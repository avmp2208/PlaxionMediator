```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-GDXDUG : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                    | Mean        | Error        | StdDev     | Median      | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |------------:|-------------:|-----------:|------------:|-------:|--------:|-----:|-------:|----------:|------------:|
| Send_Mediator_0Behaviors  |    16.40 ns |     0.330 ns |   0.218 ns |    16.46 ns |   0.86 |    0.01 |    1 |      - |         - |          NA |
| Send_Plaxion_0Behaviors   |    19.05 ns |     0.327 ns |   0.217 ns |    19.10 ns |   1.00 |    0.02 |    2 |      - |         - |          NA |
| Send_MediatR_0Behaviors   |    57.78 ns |     2.194 ns |   1.147 ns |    57.75 ns |   3.03 |    0.07 |    3 | 0.0201 |     264 B |          NA |
| Send_Mediator_1Behavior   |    69.03 ns |     1.863 ns |   1.109 ns |    68.91 ns |   3.62 |    0.07 |    3 | 0.0098 |     128 B |          NA |
| Send_Plaxion_1Behavior    |   112.02 ns |     3.713 ns |   1.942 ns |   111.93 ns |   5.88 |    0.12 |    4 | 0.0098 |     128 B |          NA |
| Send_MediatR_1Behavior    |   183.67 ns |    13.111 ns |   8.672 ns |   182.15 ns |   9.64 |    0.45 |    5 | 0.0494 |     648 B |          NA |
| Send_Mediator_5Behaviors  |   322.04 ns |     3.586 ns |   2.372 ns |   321.62 ns |  16.91 |    0.22 |    6 | 0.0486 |     640 B |          NA |
| Send_Plaxion_5Behaviors   |   437.81 ns |    26.662 ns |  13.945 ns |   443.35 ns |  22.99 |    0.73 |    7 | 0.0486 |     640 B |          NA |
| Send_MediatR_5Behaviors   |   495.72 ns |    30.617 ns |  20.251 ns |   485.37 ns |  26.03 |    1.05 |    7 | 0.1450 |    1896 B |          NA |
| Send_Mediator_10Behaviors |   603.36 ns |     7.831 ns |   5.179 ns |   601.57 ns |  31.68 |    0.43 |    8 | 0.0973 |    1280 B |          NA |
| Send_Plaxion_10Behaviors  |   750.99 ns |    27.652 ns |  18.290 ns |   746.35 ns |  39.43 |    1.01 |    9 | 0.0973 |    1280 B |          NA |
| Send_MediatR_10Behaviors  |   890.53 ns |    44.586 ns |  26.532 ns |   881.01 ns |  46.76 |    1.42 |    9 | 0.2632 |    3456 B |          NA |
| Send_Mediator_20Behaviors | 1,368.10 ns |   105.867 ns |  70.025 ns | 1,341.44 ns |  71.83 |    3.59 |   10 | 0.1945 |    2560 B |          NA |
| Send_MediatR_20Behaviors  | 1,893.98 ns |   186.737 ns | 123.515 ns | 1,855.04 ns |  99.44 |    6.28 |   11 | 0.4997 |    6576 B |          NA |
| Send_Plaxion_20Behaviors  | 2,063.44 ns | 1,110.375 ns | 734.444 ns | 1,642.27 ns | 108.34 |   36.79 |   11 | 0.1945 |    2560 B |          NA |
