```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-ASUFOU : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                       | Mean        | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |------------:|-----------:|----------:|------:|--------:|-----:|-------:|----------:|------------:|
| Publish_Mediator_1Handler    |    59.18 ns |   1.151 ns |  0.761 ns |  0.67 |    0.01 |    1 | 0.0092 |     120 B |        0.79 |
| Publish_Plaxion_1Handler     |    88.34 ns |   0.418 ns |  0.277 ns |  1.00 |    0.00 |    2 | 0.0116 |     152 B |        1.00 |
| Publish_MediatR_1Handler     |   113.45 ns |   4.705 ns |  3.112 ns |  1.28 |    0.03 |    3 | 0.0268 |     352 B |        2.32 |
| Publish_Mediator_10Handlers  |   569.75 ns |  13.272 ns |  8.779 ns |  6.45 |    0.10 |    4 | 0.0916 |    1200 B |        7.89 |
| Publish_Plaxion_10Handlers   |   576.66 ns |   5.180 ns |  3.083 ns |  6.53 |    0.04 |    4 | 0.0992 |    1304 B |        8.58 |
| Publish_MediatR_10Handlers   |   700.22 ns |   9.230 ns |  5.492 ns |  7.93 |    0.06 |    4 | 0.1917 |    2512 B |       16.53 |
| Publish_Plaxion_50Handlers   | 2,725.13 ns |  35.908 ns | 21.368 ns | 30.85 |    0.25 |    5 | 0.4883 |    6424 B |       42.26 |
| Publish_Mediator_50Handlers  | 2,933.72 ns |  65.398 ns | 43.257 ns | 33.21 |    0.48 |    5 | 0.4578 |    6000 B |       39.47 |
| Publish_MediatR_50Handlers   | 3,333.72 ns |  72.024 ns | 42.860 ns | 37.74 |    0.47 |    5 | 0.9232 |   12112 B |       79.68 |
| Publish_Plaxion_100Handlers  | 5,342.80 ns |  55.310 ns | 32.914 ns | 60.48 |    0.40 |    6 | 0.9766 |   12824 B |       84.37 |
| Publish_Mediator_100Handlers | 5,791.42 ns | 142.711 ns | 94.395 ns | 65.56 |    1.04 |    6 | 0.9155 |   12000 B |       78.95 |
| Publish_MediatR_100Handlers  | 6,738.48 ns |  90.987 ns | 54.145 ns | 76.28 |    0.62 |    6 | 1.8387 |   24112 B |      158.63 |
