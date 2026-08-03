```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-CFKAWA : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                    | Mean        | Error      | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------- |------------:|-----------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Send_Mediator_0Behaviors  |    17.12 ns |   0.355 ns |   0.235 ns |  0.57 |    0.01 |    1 |      - |      - |         - |          NA |
| Send_Plaxion_0Behaviors   |    30.05 ns |   0.197 ns |   0.130 ns |  1.00 |    0.01 |    2 |      - |      - |         - |          NA |
| Send_MediatR_0Behaviors   |    60.19 ns |   4.308 ns |   2.850 ns |  2.00 |    0.09 |    3 | 0.0201 |      - |     264 B |          NA |
| Send_Mediator_1Behavior   |    68.46 ns |   0.808 ns |   0.534 ns |  2.28 |    0.02 |    4 | 0.0098 |      - |     128 B |          NA |
| Send_Plaxion_1Behavior    |   134.02 ns |   2.174 ns |   1.438 ns |  4.46 |    0.05 |    5 | 0.0243 |      - |     320 B |          NA |
| Send_MediatR_1Behavior    |   161.41 ns |   4.701 ns |   2.798 ns |  5.37 |    0.09 |    5 | 0.0494 |      - |     648 B |          NA |
| Send_Mediator_5Behaviors  |   329.28 ns |   5.484 ns |   3.627 ns | 10.96 |    0.12 |    6 | 0.0486 |      - |     640 B |          NA |
| Send_Plaxion_5Behaviors   |   403.45 ns |  12.149 ns |   8.036 ns | 13.42 |    0.26 |    7 | 0.0634 |      - |     832 B |          NA |
| Send_Mediator_10Behaviors |   613.21 ns |   5.226 ns |   2.733 ns | 20.40 |    0.12 |    8 | 0.0973 |      - |    1280 B |          NA |
| Send_Plaxion_10Behaviors  |   736.41 ns |   6.508 ns |   4.304 ns | 24.50 |    0.17 |    8 | 0.1125 |      - |    1472 B |          NA |
| Send_MediatR_5Behaviors   |   805.43 ns | 241.766 ns | 126.448 ns | 26.80 |    3.96 |    8 | 0.1450 |      - |    1896 B |          NA |
| Send_MediatR_10Behaviors  | 1,267.24 ns | 568.494 ns | 376.023 ns | 42.17 |   11.93 |    9 | 0.2642 |      - |    3456 B |          NA |
| Send_Mediator_20Behaviors | 1,316.11 ns |  17.918 ns |   9.371 ns | 43.79 |    0.34 |    9 | 0.1945 |      - |    2560 B |          NA |
| Send_Plaxion_20Behaviors  | 1,500.17 ns |  20.199 ns |  13.360 ns | 49.92 |    0.47 |    9 | 0.2098 |      - |    2752 B |          NA |
| Send_MediatR_20Behaviors  | 1,696.07 ns |  56.885 ns |  37.626 ns | 56.43 |    1.22 |   10 | 0.5016 | 0.0019 |    6576 B |          NA |
