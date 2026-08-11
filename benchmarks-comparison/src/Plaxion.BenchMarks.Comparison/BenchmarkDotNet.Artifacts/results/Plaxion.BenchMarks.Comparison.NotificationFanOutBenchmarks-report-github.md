```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-KENNUZ : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                       | Mean         | Error      | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |-------------:|-----------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
| Publish_Plaxion_1Handler     |     90.37 ns |   2.647 ns |   1.751 ns |   1.00 |    0.03 |    1 | 0.0116 |     152 B |        1.00 |
| Publish_Mediator_1Handler    |    109.08 ns |   1.082 ns |   0.566 ns |   1.21 |    0.02 |    1 | 0.0092 |     120 B |        0.79 |
| Publish_MediatR_1Handler     |    212.21 ns |  11.457 ns |   7.578 ns |   2.35 |    0.09 |    2 | 0.0267 |     352 B |        2.32 |
| Publish_Plaxion_10Handlers   |    594.92 ns |  18.209 ns |  10.836 ns |   6.59 |    0.17 |    3 | 0.0992 |    1304 B |        8.58 |
| Publish_Mediator_10Handlers  |  1,059.74 ns |  60.043 ns |  35.731 ns |  11.73 |    0.43 |    4 | 0.0916 |    1200 B |        7.89 |
| Publish_MediatR_10Handlers   |  1,344.94 ns |  56.307 ns |  37.243 ns |  14.89 |    0.48 |    5 | 0.1917 |    2512 B |       16.53 |
| Publish_Plaxion_50Handlers   |  5,258.13 ns | 208.766 ns | 138.086 ns |  58.21 |    1.81 |    6 | 0.4883 |    6424 B |       42.26 |
| Publish_Mediator_50Handlers  |  5,540.19 ns | 169.779 ns | 112.298 ns |  61.33 |    1.63 |    6 | 0.4578 |    6000 B |       39.47 |
| Publish_MediatR_50Handlers   |  6,926.63 ns | 338.845 ns | 224.125 ns |  76.68 |    2.75 |    7 | 0.9232 |   12112 B |       79.68 |
| Publish_Plaxion_100Handlers  | 10,816.74 ns | 680.831 ns | 405.152 ns | 119.74 |    4.79 |    8 | 0.9766 |   12824 B |       84.37 |
| Publish_Mediator_100Handlers | 11,215.77 ns | 221.412 ns | 146.450 ns | 124.16 |    2.75 |    8 | 0.9155 |   12000 B |       78.95 |
| Publish_MediatR_100Handlers  | 13,377.77 ns | 205.752 ns | 122.440 ns | 148.09 |    3.00 |    8 | 1.8311 |   24112 B |      158.63 |
