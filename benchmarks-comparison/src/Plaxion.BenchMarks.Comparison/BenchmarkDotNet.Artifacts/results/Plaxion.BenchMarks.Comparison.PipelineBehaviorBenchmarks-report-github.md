```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-FAALTU : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                    | Mean        | Error      | StdDev     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |------------:|-----------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Send_Mediator_0Behaviors  |    18.06 ns |   0.436 ns |   0.260 ns |  0.36 |    0.01 |    1 |      - |         - |          NA |
| Send_Plaxion_0Behaviors   |    50.76 ns |   1.112 ns |   0.736 ns |  1.00 |    0.02 |    2 |      - |         - |          NA |
| Send_Mediator_1Behavior   |    81.55 ns |   5.328 ns |   3.524 ns |  1.61 |    0.07 |    3 | 0.0098 |     128 B |          NA |
| Send_MediatR_0Behaviors   |   126.27 ns |  47.880 ns |  31.670 ns |  2.49 |    0.60 |    4 | 0.0201 |     264 B |          NA |
| Send_Plaxion_1Behavior    |   169.06 ns |   6.296 ns |   3.746 ns |  3.33 |    0.08 |    4 | 0.0098 |     128 B |          NA |
| Send_MediatR_1Behavior    |   206.62 ns |   8.839 ns |   5.846 ns |  4.07 |    0.12 |    4 | 0.0494 |     648 B |          NA |
| Send_Mediator_5Behaviors  |   350.49 ns |  14.513 ns |   8.636 ns |  6.91 |    0.19 |    5 | 0.0486 |     640 B |          NA |
| Send_Plaxion_5Behaviors   |   512.78 ns |  62.615 ns |  41.416 ns | 10.10 |    0.79 |    6 | 0.0486 |     640 B |          NA |
| Send_MediatR_5Behaviors   |   590.80 ns |  54.574 ns |  36.098 ns | 11.64 |    0.70 |    6 | 0.1450 |    1896 B |          NA |
| Send_Mediator_10Behaviors |   696.32 ns |  18.649 ns |  12.335 ns | 13.72 |    0.30 |    7 | 0.0973 |    1280 B |          NA |
| Send_Plaxion_10Behaviors  | 1,031.28 ns |  94.488 ns |  62.498 ns | 20.32 |    1.21 |    8 | 0.0973 |    1280 B |          NA |
| Send_MediatR_10Behaviors  | 1,051.36 ns |  53.661 ns |  35.494 ns | 20.71 |    0.73 |    8 | 0.2632 |    3456 B |          NA |
| Send_Mediator_20Behaviors | 1,501.06 ns |  60.133 ns |  39.774 ns | 29.58 |    0.85 |    9 | 0.1945 |    2560 B |          NA |
| Send_Plaxion_20Behaviors  | 2,103.46 ns | 318.179 ns | 210.456 ns | 41.44 |    4.00 |   10 | 0.1945 |    2560 B |          NA |
| Send_MediatR_20Behaviors  | 2,139.07 ns |  67.876 ns |  44.895 ns | 42.15 |    1.03 |   10 | 0.4997 |    6576 B |          NA |
