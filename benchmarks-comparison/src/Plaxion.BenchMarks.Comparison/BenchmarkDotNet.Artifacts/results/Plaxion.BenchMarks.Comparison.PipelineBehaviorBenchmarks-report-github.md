```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-TWNJOK : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                    | Mean        | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------- |------------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Send_Mediator_0Behaviors  |    18.90 ns |   0.283 ns |  0.187 ns |  0.92 |    0.02 |    1 |      - |      - |         - |          NA |
| Send_Plaxion_0Behaviors   |    20.50 ns |   0.956 ns |  0.500 ns |  1.00 |    0.03 |    1 |      - |      - |         - |          NA |
| Send_MediatR_0Behaviors   |    64.33 ns |   7.034 ns |  4.653 ns |  3.14 |    0.23 |    2 | 0.0201 |      - |     264 B |          NA |
| Send_Mediator_1Behavior   |    73.44 ns |   3.399 ns |  2.248 ns |  3.58 |    0.13 |    2 | 0.0098 |      - |     128 B |          NA |
| Send_Plaxion_1Behavior    |   124.02 ns |   3.516 ns |  2.326 ns |  6.05 |    0.18 |    3 | 0.0134 |      - |     176 B |          NA |
| Send_MediatR_1Behavior    |   163.83 ns |  12.759 ns |  7.593 ns |  7.99 |    0.40 |    4 | 0.0494 |      - |     648 B |          NA |
| Send_Mediator_5Behaviors  |   328.86 ns |  14.557 ns |  9.629 ns | 16.05 |    0.59 |    5 | 0.0486 |      - |     640 B |          NA |
| Send_Plaxion_5Behaviors   |   397.49 ns |  14.761 ns |  8.784 ns | 19.40 |    0.61 |    5 | 0.0525 |      - |     688 B |          NA |
| Send_MediatR_5Behaviors   |   475.24 ns |  34.520 ns | 22.833 ns | 23.19 |    1.20 |    5 | 0.1450 |      - |    1896 B |          NA |
| Send_Mediator_10Behaviors |   625.71 ns |  26.439 ns | 17.488 ns | 30.54 |    1.09 |    6 | 0.0973 |      - |    1280 B |          NA |
| Send_Plaxion_10Behaviors  |   793.18 ns |  32.372 ns | 19.264 ns | 38.71 |    1.28 |    7 | 0.1011 |      - |    1328 B |          NA |
| Send_MediatR_10Behaviors  |   852.77 ns |  60.220 ns | 39.832 ns | 41.62 |    2.10 |    7 | 0.2642 |      - |    3456 B |          NA |
| Send_Mediator_20Behaviors | 1,351.27 ns |  77.408 ns | 51.201 ns | 65.94 |    2.85 |    8 | 0.1945 |      - |    2560 B |          NA |
| Send_Plaxion_20Behaviors  | 1,569.23 ns |  58.956 ns | 38.996 ns | 76.58 |    2.56 |    9 | 0.1984 |      - |    2608 B |          NA |
| Send_MediatR_20Behaviors  | 1,831.37 ns | 161.944 ns | 96.370 ns | 89.37 |    4.94 |    9 | 0.5016 | 0.0019 |    6576 B |          NA |
