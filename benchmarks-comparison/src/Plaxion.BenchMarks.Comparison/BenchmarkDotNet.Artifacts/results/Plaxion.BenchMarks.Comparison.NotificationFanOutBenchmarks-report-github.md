```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-TIXYSW : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                       | Mean        | Error      | StdDev     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |------------:|-----------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Publish_Mediator_1Handler    |    59.10 ns |   2.768 ns |   1.647 ns |  0.66 |    0.02 |    1 | 0.0092 |     120 B |        0.79 |
| Publish_Plaxion_1Handler     |    89.16 ns |   2.457 ns |   1.625 ns |  1.00 |    0.02 |    2 | 0.0116 |     152 B |        1.00 |
| Publish_MediatR_1Handler     |   121.67 ns |   3.921 ns |   2.594 ns |  1.37 |    0.04 |    3 | 0.0268 |     352 B |        2.32 |
| Publish_Mediator_10Handlers  |   584.04 ns |  19.838 ns |  13.122 ns |  6.55 |    0.18 |    4 | 0.0916 |    1200 B |        7.89 |
| Publish_Plaxion_10Handlers   |   586.86 ns |  11.990 ns |   7.135 ns |  6.58 |    0.14 |    4 | 0.0992 |    1304 B |        8.58 |
| Publish_MediatR_10Handlers   |   738.66 ns |  36.388 ns |  24.068 ns |  8.29 |    0.29 |    5 | 0.1917 |    2512 B |       16.53 |
| Publish_Plaxion_50Handlers   | 2,808.66 ns |  99.450 ns |  65.780 ns | 31.51 |    0.89 |    6 | 0.4883 |    6424 B |       42.26 |
| Publish_Mediator_50Handlers  | 2,991.50 ns | 118.307 ns |  78.253 ns | 33.56 |    1.02 |    6 | 0.4578 |    6000 B |       39.47 |
| Publish_MediatR_50Handlers   | 3,565.68 ns | 120.807 ns |  79.906 ns | 40.00 |    1.10 |    7 | 0.9232 |   12112 B |       79.68 |
| Publish_Plaxion_100Handlers  | 5,573.20 ns | 113.775 ns |  67.706 ns | 62.53 |    1.29 |    8 | 0.9766 |   12824 B |       84.37 |
| Publish_Mediator_100Handlers | 5,854.28 ns | 171.401 ns | 113.371 ns | 65.68 |    1.65 |    8 | 0.9155 |   12000 B |       78.95 |
| Publish_MediatR_100Handlers  | 6,864.30 ns | 237.563 ns | 157.133 ns | 77.01 |    2.14 |    9 | 1.8387 |   24112 B |      158.63 |
