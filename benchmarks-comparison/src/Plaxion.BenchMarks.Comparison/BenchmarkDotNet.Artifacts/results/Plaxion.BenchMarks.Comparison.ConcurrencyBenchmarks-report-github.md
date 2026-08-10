```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-ZPGVYI : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                  | Mean        | Error      | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Completed Work Items | Lock Contentions | Gen1   | Allocated | Alloc Ratio |
|------------------------ |------------:|-----------:|-----------:|-------:|--------:|-----:|-------:|---------------------:|-----------------:|-------:|----------:|------------:|
| Concurrent_Mediator_1   |    43.07 ns |   2.299 ns |   1.521 ns |   0.93 |    0.04 |    1 | 0.0134 |                    - |                - |      - |     176 B |        1.00 |
| Concurrent_Plaxion_1    |    46.40 ns |   1.550 ns |   1.025 ns |   1.00 |    0.03 |    1 | 0.0134 |                    - |                - |      - |     176 B |        1.00 |
| Concurrent_MediatR_1    |    94.49 ns |   9.232 ns |   6.107 ns |   2.04 |    0.13 |    2 | 0.0281 |                    - |                - |      - |     368 B |        2.09 |
| Concurrent_Mediator_8   |   240.13 ns |   7.282 ns |   4.817 ns |   5.18 |    0.15 |    3 | 0.0563 |                    - |                - |      - |     736 B |        4.18 |
| Concurrent_Plaxion_8    |   255.29 ns |  11.604 ns |   7.675 ns |   5.50 |    0.20 |    3 | 0.0563 |                    - |                - |      - |     736 B |        4.18 |
| Concurrent_MediatR_8    |   584.36 ns |  47.985 ns |  31.739 ns |  12.60 |    0.71 |    4 | 0.1736 |                    - |                - |      - |    2272 B |       12.91 |
| Concurrent_Mediator_32  |   891.49 ns |  22.156 ns |  13.185 ns |  19.22 |    0.49 |    5 | 0.2031 |               0.0000 |                - | 0.0019 |    2656 B |       15.09 |
| Concurrent_Plaxion_32   |   940.87 ns |  43.934 ns |  29.060 ns |  20.29 |    0.74 |    5 | 0.2022 |                    - |                - | 0.0019 |    2656 B |       15.09 |
| Concurrent_MediatR_32   | 2,292.05 ns | 140.724 ns |  93.080 ns |  49.42 |    2.19 |    6 | 0.6714 |                    - |                - | 0.0038 |    8800 B |       50.00 |
| Concurrent_Mediator_128 | 3,683.47 ns | 138.553 ns |  91.644 ns |  79.42 |    2.53 |    7 | 0.7896 |               0.0000 |                - | 0.0229 |   10336 B |       58.73 |
| Concurrent_Plaxion_128  | 4,085.42 ns | 112.004 ns |  74.084 ns |  88.09 |    2.42 |    8 | 0.7858 |                    - |                - | 0.0229 |   10336 B |       58.73 |
| Concurrent_MediatR_128  | 9,070.95 ns | 614.580 ns | 406.506 ns | 195.59 |    9.34 |    9 | 2.6703 |                    - |                - | 0.0916 |   34912 B |      198.36 |
