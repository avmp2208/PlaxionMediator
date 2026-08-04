```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-UCAZZR : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                    | Mean        | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------- |------------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| Send_Mediator_0Behaviors  |    15.46 ns |  0.105 ns |  0.069 ns |  0.68 |    0.01 |    1 |      - |      - |         - |          NA |
| Send_Plaxion_0Behaviors   |    22.70 ns |  0.597 ns |  0.395 ns |  1.00 |    0.02 |    2 |      - |      - |         - |          NA |
| Send_MediatR_0Behaviors   |    52.16 ns |  2.878 ns |  1.903 ns |  2.30 |    0.09 |    3 | 0.0201 |      - |     264 B |          NA |
| Send_Mediator_1Behavior   |    68.53 ns |  1.242 ns |  0.821 ns |  3.02 |    0.06 |    4 | 0.0098 |      - |     128 B |          NA |
| Send_Plaxion_1Behavior    |   119.52 ns |  3.364 ns |  2.002 ns |  5.27 |    0.12 |    5 | 0.0098 |      - |     128 B |          NA |
| Send_MediatR_1Behavior    |   162.58 ns |  5.027 ns |  2.629 ns |  7.16 |    0.16 |    6 | 0.0494 |      - |     648 B |          NA |
| Send_Mediator_5Behaviors  |   308.06 ns |  5.624 ns |  3.720 ns | 13.57 |    0.27 |    7 | 0.0486 |      - |     640 B |          NA |
| Send_Plaxion_5Behaviors   |   391.69 ns |  4.824 ns |  3.191 ns | 17.26 |    0.32 |    8 | 0.0486 |      - |     640 B |          NA |
| Send_MediatR_5Behaviors   |   450.51 ns | 21.082 ns | 12.545 ns | 19.85 |    0.62 |    8 | 0.1450 |      - |    1896 B |          NA |
| Send_Mediator_10Behaviors |   604.65 ns | 23.168 ns | 13.787 ns | 26.64 |    0.73 |    9 | 0.0973 |      - |    1280 B |          NA |
| Send_Plaxion_10Behaviors  |   716.20 ns |  4.616 ns |  3.053 ns | 31.55 |    0.54 |    9 | 0.0973 |      - |    1280 B |          NA |
| Send_MediatR_10Behaviors  |   845.41 ns | 24.000 ns | 15.875 ns | 37.25 |    0.91 |   10 | 0.2642 |      - |    3456 B |          NA |
| Send_Mediator_20Behaviors | 1,335.62 ns | 28.163 ns | 16.759 ns | 58.84 |    1.20 |   11 | 0.1945 |      - |    2560 B |          NA |
| Send_Plaxion_20Behaviors  | 1,493.24 ns | 28.785 ns | 19.039 ns | 65.79 |    1.35 |   11 | 0.1945 |      - |    2560 B |          NA |
| Send_MediatR_20Behaviors  | 1,687.25 ns | 36.795 ns | 19.244 ns | 74.33 |    1.47 |   11 | 0.5016 | 0.0019 |    6576 B |          NA |
