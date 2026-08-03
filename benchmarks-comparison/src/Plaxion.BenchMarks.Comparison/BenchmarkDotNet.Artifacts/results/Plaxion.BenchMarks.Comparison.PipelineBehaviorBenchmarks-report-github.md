```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-HJCBLN : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                    | Mean        | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------- |------------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Send_Mediator_0Behaviors  |    17.01 ns |   0.146 ns |  0.096 ns |  0.56 |    0.00 |    1 |      - |      - |         - |          NA |
| Send_Plaxion_0Behaviors   |    30.39 ns |   0.213 ns |  0.141 ns |  1.00 |    0.01 |    2 |      - |      - |         - |          NA |
| Send_MediatR_0Behaviors   |    52.97 ns |   2.058 ns |  1.361 ns |  1.74 |    0.04 |    3 | 0.0201 |      - |     264 B |          NA |
| Send_Mediator_1Behavior   |    68.47 ns |   0.437 ns |  0.229 ns |  2.25 |    0.01 |    4 | 0.0098 |      - |     128 B |          NA |
| Send_Plaxion_1Behavior    |   127.76 ns |   0.727 ns |  0.380 ns |  4.20 |    0.02 |    5 | 0.0098 |      - |     128 B |          NA |
| Send_MediatR_1Behavior    |   161.27 ns |   2.447 ns |  1.456 ns |  5.31 |    0.05 |    6 | 0.0494 |      - |     648 B |          NA |
| Send_Mediator_5Behaviors  |   305.82 ns |   4.042 ns |  2.405 ns | 10.06 |    0.09 |    7 | 0.0486 |      - |     640 B |          NA |
| Send_Plaxion_5Behaviors   |   396.04 ns |   3.266 ns |  1.943 ns | 13.03 |    0.08 |    8 | 0.0486 |      - |     640 B |          NA |
| Send_MediatR_5Behaviors   |   452.57 ns |  15.602 ns | 10.320 ns | 14.89 |    0.33 |    8 | 0.1450 |      - |    1896 B |          NA |
| Send_Mediator_10Behaviors |   604.34 ns |   9.871 ns |  6.529 ns | 19.88 |    0.22 |    9 | 0.0973 |      - |    1280 B |          NA |
| Send_Plaxion_10Behaviors  |   722.35 ns |  14.813 ns |  9.798 ns | 23.77 |    0.32 |   10 | 0.0973 |      - |    1280 B |          NA |
| Send_MediatR_10Behaviors  |   814.91 ns |  21.196 ns | 12.613 ns | 26.81 |    0.41 |   10 | 0.2642 |      - |    3456 B |          NA |
| Send_Mediator_20Behaviors | 1,298.41 ns |  17.837 ns | 11.798 ns | 42.72 |    0.42 |   11 | 0.1945 |      - |    2560 B |          NA |
| Send_Plaxion_20Behaviors  | 1,548.83 ns | 119.834 ns | 71.311 ns | 50.96 |    2.24 |   11 | 0.1945 |      - |    2560 B |          NA |
| Send_MediatR_20Behaviors  | 1,680.35 ns |  32.523 ns | 17.010 ns | 55.29 |    0.58 |   11 | 0.5016 | 0.0019 |    6576 B |          NA |
