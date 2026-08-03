```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-CFKAWA : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                       | Mean        | Error      | StdDev     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |------------:|-----------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Publish_Mediator_1Handler    |    58.73 ns |   1.788 ns |   1.064 ns |  0.65 |    0.01 |    1 | 0.0092 |     120 B |        0.79 |
| Publish_Plaxion_1Handler     |    90.31 ns |   1.208 ns |   0.799 ns |  1.00 |    0.01 |    2 | 0.0116 |     152 B |        1.00 |
| Publish_MediatR_1Handler     |   118.90 ns |   1.793 ns |   1.186 ns |  1.32 |    0.02 |    3 | 0.0268 |     352 B |        2.32 |
| Publish_Mediator_10Handlers  |   576.10 ns |  10.386 ns |   6.181 ns |  6.38 |    0.08 |    4 | 0.0916 |    1200 B |        7.89 |
| Publish_Plaxion_10Handlers   |   591.19 ns |   8.096 ns |   4.818 ns |  6.55 |    0.08 |    4 | 0.0992 |    1304 B |        8.58 |
| Publish_MediatR_10Handlers   |   727.27 ns |  10.903 ns |   5.702 ns |  8.05 |    0.09 |    4 | 0.1917 |    2512 B |       16.53 |
| Publish_Plaxion_50Handlers   | 2,775.33 ns |  77.848 ns |  51.492 ns | 30.73 |    0.60 |    5 | 0.4883 |    6424 B |       42.26 |
| Publish_Mediator_50Handlers  | 2,920.65 ns |  37.464 ns |  24.780 ns | 32.34 |    0.38 |    5 | 0.4578 |    6000 B |       39.47 |
| Publish_MediatR_50Handlers   | 3,418.66 ns |  76.614 ns |  50.676 ns | 37.86 |    0.62 |    6 | 0.9232 |   12112 B |       79.68 |
| Publish_Plaxion_100Handlers  | 5,522.10 ns | 152.615 ns | 100.945 ns | 61.15 |    1.19 |    7 | 0.9766 |   12824 B |       84.37 |
| Publish_Mediator_100Handlers | 5,903.95 ns |  75.728 ns |  50.089 ns | 65.38 |    0.77 |    8 | 0.9155 |   12000 B |       78.95 |
| Publish_MediatR_100Handlers  | 7,695.78 ns | 562.681 ns | 372.179 ns | 85.22 |    4.00 |    9 | 1.8387 |   24112 B |      158.63 |
