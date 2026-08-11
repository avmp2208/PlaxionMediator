```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-KENNUZ : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                    | Mean        | Error      | StdDev     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |------------:|-----------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Send_Mediator_0Behaviors  |    17.01 ns |   0.989 ns |   0.589 ns |  0.49 |    0.02 |    1 |      - |         - |          NA |
| Send_Plaxion_0Behaviors   |    34.48 ns |   2.064 ns |   1.228 ns |  1.00 |    0.05 |    2 |      - |         - |          NA |
| Send_MediatR_0Behaviors   |    74.62 ns |  13.964 ns |   9.236 ns |  2.17 |    0.27 |    3 | 0.0201 |     264 B |          NA |
| Send_Mediator_1Behavior   |    81.39 ns |   7.366 ns |   4.872 ns |  2.36 |    0.16 |    3 | 0.0098 |     128 B |          NA |
| Send_MediatR_1Behavior    |   210.35 ns |  23.377 ns |  15.463 ns |  6.11 |    0.47 |    4 | 0.0494 |     648 B |          NA |
| Send_Plaxion_1Behavior    |   211.54 ns |   9.095 ns |   6.016 ns |  6.14 |    0.26 |    4 | 0.0134 |     176 B |          NA |
| Send_Mediator_5Behaviors  |   339.20 ns |  18.757 ns |  11.162 ns |  9.85 |    0.45 |    5 | 0.0486 |     640 B |          NA |
| Send_Plaxion_5Behaviors   |   448.07 ns |  38.900 ns |  25.730 ns | 13.01 |    0.83 |    6 | 0.0525 |     688 B |          NA |
| Send_MediatR_5Behaviors   |   576.51 ns |  85.505 ns |  56.556 ns | 16.74 |    1.66 |    7 | 0.1450 |    1896 B |          NA |
| Send_Mediator_10Behaviors |   691.77 ns |  87.040 ns |  57.572 ns | 20.09 |    1.73 |    7 | 0.0973 |    1280 B |          NA |
| Send_Plaxion_10Behaviors  |   885.57 ns |  98.779 ns |  65.336 ns | 25.71 |    2.00 |    8 | 0.1011 |    1328 B |          NA |
| Send_MediatR_10Behaviors  | 1,071.85 ns | 162.088 ns | 107.211 ns | 31.12 |    3.14 |    8 | 0.2632 |    3456 B |          NA |
| Send_Mediator_20Behaviors | 1,474.64 ns | 132.547 ns |  87.672 ns | 42.82 |    2.81 |    9 | 0.1945 |    2560 B |          NA |
| Send_Plaxion_20Behaviors  | 1,671.07 ns | 145.205 ns |  96.044 ns | 48.52 |    3.10 |    9 | 0.1984 |    2608 B |          NA |
| Send_MediatR_20Behaviors  | 2,173.29 ns | 277.634 ns | 183.638 ns | 63.10 |    5.49 |   10 | 0.4997 |    6576 B |          NA |
