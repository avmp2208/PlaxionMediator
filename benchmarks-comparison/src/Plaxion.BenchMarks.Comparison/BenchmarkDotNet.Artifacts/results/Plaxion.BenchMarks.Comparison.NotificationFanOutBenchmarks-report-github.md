```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-HPLYXY : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                       | Mean         | Error        | StdDev     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |-------------:|-------------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Publish_Mediator_1Handler    |     72.62 ns |     1.869 ns |   1.236 ns |  0.61 |    0.02 |    1 | 0.0092 |     120 B |        0.79 |
| Publish_Plaxion_1Handler     |    118.54 ns |     4.221 ns |   2.792 ns |  1.00 |    0.03 |    2 | 0.0116 |     152 B |        1.00 |
| Publish_MediatR_1Handler     |    165.09 ns |     4.417 ns |   2.922 ns |  1.39 |    0.04 |    3 | 0.0267 |     352 B |        2.32 |
| Publish_Mediator_10Handlers  |    693.28 ns |    12.051 ns |   6.303 ns |  5.85 |    0.14 |    4 | 0.0916 |    1200 B |        7.89 |
| Publish_Plaxion_10Handlers   |    785.28 ns |    34.381 ns |  22.741 ns |  6.63 |    0.23 |    4 | 0.0992 |    1304 B |        8.58 |
| Publish_MediatR_10Handlers   |  1,015.08 ns |    75.263 ns |  49.782 ns |  8.57 |    0.44 |    5 | 0.1917 |    2512 B |       16.53 |
| Publish_Plaxion_50Handlers   |  3,667.57 ns |    89.586 ns |  59.255 ns | 30.95 |    0.83 |    6 | 0.4883 |    6424 B |       42.26 |
| Publish_Mediator_50Handlers  |  3,937.06 ns | 1,512.249 ns | 899.915 ns | 33.23 |    7.24 |    6 | 0.4578 |    6000 B |       39.47 |
| Publish_MediatR_50Handlers   |  4,769.01 ns |   505.405 ns | 334.294 ns | 40.25 |    2.83 |    7 | 0.9232 |   12112 B |       79.68 |
| Publish_Plaxion_100Handlers  |  7,482.39 ns |   195.353 ns | 129.214 ns | 63.15 |    1.74 |    8 | 0.9766 |   12824 B |       84.37 |
| Publish_MediatR_100Handlers  | 10,298.68 ns | 1,004.989 ns | 598.053 ns | 86.92 |    5.16 |    9 | 1.8311 |   24112 B |      158.63 |
| Publish_Mediator_100Handlers | 11,601.57 ns |   166.321 ns |  98.975 ns | 97.92 |    2.30 |    9 | 0.9155 |   12000 B |       78.95 |
