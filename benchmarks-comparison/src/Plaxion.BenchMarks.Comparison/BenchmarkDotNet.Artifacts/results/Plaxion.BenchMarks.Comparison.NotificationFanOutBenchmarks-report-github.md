```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-TWNJOK : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                       | Mean        | Error      | StdDev     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |------------:|-----------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Publish_Mediator_1Handler    |    63.37 ns |   4.865 ns |   3.218 ns |  0.66 |    0.04 |    1 | 0.0092 |     120 B |        0.79 |
| Publish_Plaxion_1Handler     |    95.81 ns |   5.425 ns |   3.588 ns |  1.00 |    0.05 |    2 | 0.0116 |     152 B |        1.00 |
| Publish_MediatR_1Handler     |   117.90 ns |  18.431 ns |  10.968 ns |  1.23 |    0.12 |    2 | 0.0267 |     352 B |        2.32 |
| Publish_Plaxion_10Handlers   |   615.52 ns |  27.986 ns |  16.654 ns |  6.43 |    0.28 |    3 | 0.0992 |    1304 B |        8.58 |
| Publish_Mediator_10Handlers  |   616.08 ns |  43.152 ns |  25.679 ns |  6.44 |    0.34 |    3 | 0.0916 |    1200 B |        7.89 |
| Publish_MediatR_10Handlers   |   877.39 ns | 167.013 ns | 110.469 ns |  9.17 |    1.15 |    4 | 0.1907 |    2512 B |       16.53 |
| Publish_Plaxion_50Handlers   | 2,812.16 ns |  71.213 ns |  37.246 ns | 29.39 |    1.11 |    5 | 0.4883 |    6424 B |       42.26 |
| Publish_Mediator_50Handlers  | 3,022.01 ns | 115.474 ns |  76.379 ns | 31.58 |    1.36 |    5 | 0.4578 |    6000 B |       39.47 |
| Publish_MediatR_50Handlers   | 3,826.04 ns | 309.252 ns | 204.551 ns | 39.98 |    2.49 |    6 | 0.9232 |   12112 B |       79.68 |
| Publish_Plaxion_100Handlers  | 5,764.50 ns | 161.419 ns | 106.768 ns | 60.24 |    2.40 |    7 | 0.9766 |   12824 B |       84.37 |
| Publish_Mediator_100Handlers | 6,301.49 ns | 409.297 ns | 243.566 ns | 65.85 |    3.37 |    7 | 0.9155 |   12000 B |       78.95 |
| Publish_MediatR_100Handlers  | 8,189.91 ns | 706.581 ns | 467.360 ns | 85.59 |    5.57 |    8 | 1.8311 |   24112 B |      158.63 |
