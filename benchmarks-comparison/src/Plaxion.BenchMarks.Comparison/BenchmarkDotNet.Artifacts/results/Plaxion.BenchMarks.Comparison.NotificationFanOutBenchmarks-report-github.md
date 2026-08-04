```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-UCAZZR : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                       | Mean        | Error      | StdDev     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |------------:|-----------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Publish_Mediator_1Handler    |    58.92 ns |   0.707 ns |   0.421 ns |  0.66 |    0.01 |    1 | 0.0092 |     120 B |        0.79 |
| Publish_Plaxion_1Handler     |    89.22 ns |   1.705 ns |   1.127 ns |  1.00 |    0.02 |    2 | 0.0116 |     152 B |        1.00 |
| Publish_MediatR_1Handler     |   115.47 ns |   2.536 ns |   1.509 ns |  1.29 |    0.02 |    3 | 0.0268 |     352 B |        2.32 |
| Publish_Mediator_10Handlers  |   565.40 ns |   9.082 ns |   6.007 ns |  6.34 |    0.10 |    4 | 0.0916 |    1200 B |        7.89 |
| Publish_Plaxion_10Handlers   |   604.36 ns |  13.071 ns |   7.778 ns |  6.77 |    0.12 |    4 | 0.0992 |    1304 B |        8.58 |
| Publish_MediatR_10Handlers   |   743.79 ns |  27.440 ns |  18.150 ns |  8.34 |    0.22 |    4 | 0.1917 |    2512 B |       16.53 |
| Publish_Plaxion_50Handlers   | 2,752.46 ns |  58.021 ns |  38.377 ns | 30.85 |    0.55 |    5 | 0.4883 |    6424 B |       42.26 |
| Publish_Mediator_50Handlers  | 2,938.96 ns |  61.823 ns |  40.892 ns | 32.94 |    0.59 |    5 | 0.4578 |    6000 B |       39.47 |
| Publish_MediatR_50Handlers   | 3,579.49 ns | 207.333 ns | 108.439 ns | 40.12 |    1.24 |    5 | 0.9232 |   12112 B |       79.68 |
| Publish_Plaxion_100Handlers  | 5,459.38 ns |  89.508 ns |  53.265 ns | 61.20 |    0.93 |    6 | 0.9766 |   12824 B |       84.37 |
| Publish_Mediator_100Handlers | 5,895.82 ns | 171.361 ns | 101.974 ns | 66.09 |    1.34 |    6 | 0.9155 |   12000 B |       78.95 |
| Publish_MediatR_100Handlers  | 6,744.40 ns | 200.894 ns | 119.549 ns | 75.60 |    1.56 |    6 | 1.8387 |   24112 B |      158.63 |
