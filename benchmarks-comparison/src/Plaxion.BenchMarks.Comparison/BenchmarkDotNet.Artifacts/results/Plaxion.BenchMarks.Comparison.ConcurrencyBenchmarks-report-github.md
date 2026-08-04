```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-DOXMTQ : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                  | Mean         | Error        | StdDev       | Ratio | RatioSD | Rank | Completed Work Items | Lock Contentions | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-------------:|-------------:|------:|--------:|-----:|---------------------:|-----------------:|-------:|-------:|----------:|------------:|
| Concurrent_Mediator_1   |     49.76 ns |     2.273 ns |     1.503 ns |  0.40 |    0.01 |    1 |                    - |                - | 0.0134 |      - |     176 B |        1.00 |
| Concurrent_MediatR_1    |     99.47 ns |     4.470 ns |     2.957 ns |  0.80 |    0.03 |    2 |                    - |                - | 0.0281 |      - |     368 B |        2.09 |
| Concurrent_Plaxion_1    |    125.10 ns |     4.810 ns |     2.862 ns |  1.00 |    0.03 |    3 |                    - |                - | 0.0134 |      - |     176 B |        1.00 |
| Concurrent_Mediator_8   |    276.79 ns |     8.494 ns |     5.618 ns |  2.21 |    0.06 |    4 |                    - |                - | 0.0563 |      - |     736 B |        4.18 |
| Concurrent_MediatR_8    |    691.03 ns |    38.191 ns |    25.261 ns |  5.53 |    0.23 |    5 |                    - |                - | 0.1736 |      - |    2272 B |       12.91 |
| Concurrent_Plaxion_8    |    763.41 ns |    13.423 ns |     7.988 ns |  6.11 |    0.14 |    5 |                    - |                - | 0.0563 |      - |     736 B |        4.18 |
| Concurrent_Mediator_32  |  1,055.70 ns |    28.021 ns |    18.534 ns |  8.44 |    0.23 |    6 |                    - |                - | 0.2022 | 0.0019 |    2656 B |       15.09 |
| Concurrent_MediatR_32   |  2,724.47 ns |   166.899 ns |   110.393 ns | 21.79 |    0.96 |    7 |                    - |                - | 0.6714 | 0.0038 |    8800 B |       50.00 |
| Concurrent_Plaxion_32   |  2,985.51 ns |    68.250 ns |    40.615 ns | 23.88 |    0.59 |    7 |                    - |                - | 0.2022 | 0.0019 |    2656 B |       15.09 |
| Concurrent_Mediator_128 |  4,325.08 ns |   109.741 ns |    72.587 ns | 34.59 |    0.92 |    8 |                    - |                - | 0.7858 | 0.0229 |   10336 B |       58.73 |
| Concurrent_Plaxion_128  |  9,536.41 ns | 3,187.546 ns | 2,108.365 ns | 76.27 |   16.17 |    9 |                    - |                - | 0.7858 | 0.0229 |   10336 B |       58.73 |
| Concurrent_MediatR_128  | 10,519.97 ns |   662.162 ns |   437.979 ns | 84.13 |    3.79 |    9 |                    - |                - | 2.6703 | 0.0916 |   34912 B |      198.36 |
