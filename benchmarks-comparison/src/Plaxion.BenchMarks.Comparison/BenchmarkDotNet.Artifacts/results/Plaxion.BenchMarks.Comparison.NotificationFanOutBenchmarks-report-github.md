```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-FAALTU : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                       | Mean         | Error      | StdDev     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |-------------:|-----------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Publish_Mediator_1Handler    |     79.98 ns |   2.001 ns |   1.191 ns |  0.65 |    0.02 |    1 | 0.0092 |     120 B |        0.79 |
| Publish_Plaxion_1Handler     |    122.45 ns |   3.818 ns |   2.525 ns |  1.00 |    0.03 |    2 | 0.0114 |     152 B |        1.00 |
| Publish_MediatR_1Handler     |    157.82 ns |   6.475 ns |   3.853 ns |  1.29 |    0.04 |    3 | 0.0267 |     352 B |        2.32 |
| Publish_Mediator_10Handlers  |    731.74 ns |  20.577 ns |  13.610 ns |  5.98 |    0.16 |    4 | 0.0916 |    1200 B |        7.89 |
| Publish_Plaxion_10Handlers   |    769.51 ns |  10.416 ns |   6.198 ns |  6.29 |    0.13 |    4 | 0.0992 |    1304 B |        8.58 |
| Publish_MediatR_10Handlers   |  1,085.03 ns |  19.671 ns |  11.706 ns |  8.86 |    0.20 |    5 | 0.1907 |    2512 B |       16.53 |
| Publish_Plaxion_50Handlers   |  3,736.29 ns | 135.124 ns |  89.376 ns | 30.52 |    0.92 |    6 | 0.4883 |    6424 B |       42.26 |
| Publish_Mediator_50Handlers  |  3,840.60 ns |  51.238 ns |  30.491 ns | 31.38 |    0.66 |    6 | 0.4578 |    6000 B |       39.47 |
| Publish_MediatR_50Handlers   |  5,067.27 ns | 170.961 ns | 101.736 ns | 41.40 |    1.13 |    7 | 0.9232 |   12112 B |       79.68 |
| Publish_Plaxion_100Handlers  |  7,288.16 ns | 208.138 ns | 137.671 ns | 59.54 |    1.58 |    8 | 0.9766 |   12824 B |       84.37 |
| Publish_Mediator_100Handlers |  7,580.47 ns | 130.263 ns |  77.517 ns | 61.93 |    1.35 |    8 | 0.9155 |   12000 B |       78.95 |
| Publish_MediatR_100Handlers  | 10,751.90 ns | 264.043 ns | 138.100 ns | 87.84 |    2.02 |    9 | 1.8311 |   24112 B |      158.63 |
