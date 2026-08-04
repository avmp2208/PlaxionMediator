```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-JRUHVO : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                       | Mean        | Error      | StdDev     | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |------------:|-----------:|-----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Publish_Mediator_1Handler    |    59.40 ns |   1.815 ns |   1.080 ns |  0.66 |    0.01 |    1 | 0.0092 |     120 B |        0.79 |
| Publish_Plaxion_1Handler     |    90.47 ns |   1.518 ns |   0.904 ns |  1.00 |    0.01 |    2 | 0.0116 |     152 B |        1.00 |
| Publish_MediatR_1Handler     |   112.28 ns |   3.167 ns |   2.095 ns |  1.24 |    0.02 |    2 | 0.0268 |     352 B |        2.32 |
| Publish_Mediator_10Handlers  |   581.80 ns |  12.199 ns |   8.069 ns |  6.43 |    0.10 |    3 | 0.0916 |    1200 B |        7.89 |
| Publish_Plaxion_10Handlers   |   598.84 ns |  10.223 ns |   6.083 ns |  6.62 |    0.09 |    3 | 0.0992 |    1304 B |        8.58 |
| Publish_MediatR_10Handlers   |   732.56 ns |  26.409 ns |  17.468 ns |  8.10 |    0.20 |    3 | 0.1917 |    2512 B |       16.53 |
| Publish_Plaxion_50Handlers   | 2,808.52 ns |  67.879 ns |  44.897 ns | 31.05 |    0.56 |    4 | 0.4883 |    6424 B |       42.26 |
| Publish_Mediator_50Handlers  | 2,880.80 ns |  17.554 ns |   9.181 ns | 31.85 |    0.31 |    4 | 0.4578 |    6000 B |       39.47 |
| Publish_MediatR_50Handlers   | 3,466.18 ns | 109.857 ns |  65.374 ns | 38.32 |    0.77 |    4 | 0.9232 |   12112 B |       79.68 |
| Publish_Plaxion_100Handlers  | 5,515.70 ns |  78.019 ns |  51.605 ns | 60.97 |    0.79 |    5 | 0.9766 |   12824 B |       84.37 |
| Publish_Mediator_100Handlers | 5,966.56 ns |  76.892 ns |  50.859 ns | 65.96 |    0.82 |    6 | 0.9155 |   12000 B |       78.95 |
| Publish_MediatR_100Handlers  | 8,183.52 ns | 422.874 ns | 279.705 ns | 90.47 |    3.07 |    7 | 1.8387 |   24112 B |      158.63 |
