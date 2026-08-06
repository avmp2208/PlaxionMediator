```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-TIXYSW : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                    | Mean        | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------- |------------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Send_Mediator_0Behaviors  |    15.64 ns |  0.294 ns |  0.175 ns |  0.67 |    0.01 |    1 |      - |      - |         - |          NA |
| Send_Plaxion_0Behaviors   |    23.45 ns |  0.136 ns |  0.081 ns |  1.00 |    0.00 |    2 |      - |      - |         - |          NA |
| Send_MediatR_0Behaviors   |    52.15 ns |  2.245 ns |  1.485 ns |  2.22 |    0.06 |    3 | 0.0201 |      - |     264 B |          NA |
| Send_Mediator_1Behavior   |    68.21 ns |  1.716 ns |  1.021 ns |  2.91 |    0.04 |    4 | 0.0098 |      - |     128 B |          NA |
| Send_Plaxion_1Behavior    |   121.88 ns |  1.295 ns |  0.856 ns |  5.20 |    0.04 |    5 | 0.0098 |      - |     128 B |          NA |
| Send_MediatR_1Behavior    |   173.32 ns | 10.807 ns |  7.148 ns |  7.39 |    0.29 |    6 | 0.0494 |      - |     648 B |          NA |
| Send_Mediator_5Behaviors  |   307.58 ns |  4.978 ns |  3.293 ns | 13.11 |    0.14 |    7 | 0.0486 |      - |     640 B |          NA |
| Send_Plaxion_5Behaviors   |   392.78 ns |  6.619 ns |  4.378 ns | 16.75 |    0.19 |    8 | 0.0486 |      - |     640 B |          NA |
| Send_MediatR_5Behaviors   |   447.27 ns |  8.975 ns |  5.341 ns | 19.07 |    0.22 |    8 | 0.1450 |      - |    1896 B |          NA |
| Send_Mediator_10Behaviors |   596.48 ns | 14.923 ns |  9.870 ns | 25.43 |    0.41 |    9 | 0.0973 |      - |    1280 B |          NA |
| Send_Plaxion_10Behaviors  |   742.15 ns | 17.569 ns | 10.455 ns | 31.64 |    0.44 |    9 | 0.0973 |      - |    1280 B |          NA |
| Send_MediatR_10Behaviors  |   820.02 ns | 35.519 ns | 23.493 ns | 34.96 |    0.96 |    9 | 0.2642 |      - |    3456 B |          NA |
| Send_Mediator_20Behaviors | 1,315.22 ns | 32.625 ns | 21.579 ns | 56.08 |    0.90 |   10 | 0.1945 |      - |    2560 B |          NA |
| Send_Plaxion_20Behaviors  | 1,502.94 ns | 50.806 ns | 33.605 ns | 64.08 |    1.38 |   11 | 0.1945 |      - |    2560 B |          NA |
| Send_MediatR_20Behaviors  | 1,644.02 ns | 31.341 ns | 18.651 ns | 70.10 |    0.79 |   11 | 0.5016 | 0.0019 |    6576 B |          NA |
