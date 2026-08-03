```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-HJCBLN : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                       | Mean        | Error      | StdDev     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |------------:|-----------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Publish_Mediator_1Handler    |    59.30 ns |   0.544 ns |   0.285 ns |  0.68 |    0.01 |    1 | 0.0092 |     120 B |        0.79 |
| Publish_Plaxion_1Handler     |    86.87 ns |   0.973 ns |   0.579 ns |  1.00 |    0.01 |    2 | 0.0116 |     152 B |        1.00 |
| Publish_MediatR_1Handler     |   109.31 ns |   2.047 ns |   1.354 ns |  1.26 |    0.02 |    3 | 0.0268 |     352 B |        2.32 |
| Publish_Mediator_10Handlers  |   562.68 ns |   5.946 ns |   3.539 ns |  6.48 |    0.06 |    4 | 0.0916 |    1200 B |        7.89 |
| Publish_Plaxion_10Handlers   |   594.78 ns |  12.897 ns |   8.531 ns |  6.85 |    0.10 |    4 | 0.0992 |    1304 B |        8.58 |
| Publish_MediatR_10Handlers   |   715.60 ns |  16.460 ns |  10.887 ns |  8.24 |    0.13 |    5 | 0.1917 |    2512 B |       16.53 |
| Publish_Plaxion_50Handlers   | 2,737.11 ns |  30.113 ns |  17.920 ns | 31.51 |    0.28 |    6 | 0.4883 |    6424 B |       42.26 |
| Publish_Mediator_50Handlers  | 2,849.83 ns |  19.181 ns |  10.032 ns | 32.81 |    0.23 |    6 | 0.4578 |    6000 B |       39.47 |
| Publish_MediatR_50Handlers   | 3,483.08 ns | 152.285 ns | 100.727 ns | 40.10 |    1.13 |    6 | 0.9232 |   12112 B |       79.68 |
| Publish_Plaxion_100Handlers  | 5,412.67 ns |  55.495 ns |  33.024 ns | 62.31 |    0.53 |    7 | 0.9766 |   12824 B |       84.37 |
| Publish_Mediator_100Handlers | 5,780.07 ns |  31.358 ns |  16.401 ns | 66.54 |    0.46 |    7 | 0.9155 |   12000 B |       78.95 |
| Publish_MediatR_100Handlers  | 7,943.89 ns | 254.053 ns | 168.040 ns | 91.45 |    1.93 |    8 | 1.8311 |   24112 B |      158.63 |
