```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-FAALTU : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                  | Mean         | Error      | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Completed Work Items | Lock Contentions | Gen1   | Allocated | Alloc Ratio |
|------------------------ |-------------:|-----------:|-----------:|-------:|--------:|-----:|-------:|---------------------:|-----------------:|-------:|----------:|------------:|
| Concurrent_Mediator_1   |     58.28 ns |   1.410 ns |   0.839 ns |   0.63 |    0.02 |    1 | 0.0134 |                    - |                - |      - |     176 B |        1.00 |
| Concurrent_Plaxion_1    |     92.08 ns |   3.121 ns |   2.064 ns |   1.00 |    0.03 |    2 | 0.0134 |                    - |                - |      - |     176 B |        1.00 |
| Concurrent_MediatR_1    |    123.72 ns |   7.454 ns |   4.930 ns |   1.34 |    0.06 |    3 | 0.0281 |                    - |                - |      - |     368 B |        2.09 |
| Concurrent_Mediator_8   |    323.85 ns |   9.138 ns |   6.044 ns |   3.52 |    0.10 |    4 | 0.0563 |                    - |                - |      - |     736 B |        4.18 |
| Concurrent_Plaxion_8    |    547.22 ns |  11.746 ns |   7.769 ns |   5.95 |    0.15 |    5 | 0.0563 |                    - |                - |      - |     736 B |        4.18 |
| Concurrent_MediatR_8    |    777.79 ns |  15.472 ns |   9.207 ns |   8.45 |    0.20 |    6 | 0.1736 |                    - |                - |      - |    2272 B |       12.91 |
| Concurrent_Mediator_32  |  1,231.30 ns |  31.883 ns |  21.089 ns |  13.38 |    0.36 |    7 | 0.2022 |                    - |                - | 0.0019 |    2656 B |       15.09 |
| Concurrent_Plaxion_32   |  2,080.27 ns |  70.954 ns |  42.224 ns |  22.60 |    0.65 |    8 | 0.2022 |                    - |                - |      - |    2656 B |       15.09 |
| Concurrent_MediatR_32   |  3,067.66 ns |  55.344 ns |  28.946 ns |  33.33 |    0.77 |    9 | 0.6714 |                    - |                - | 0.0038 |    8800 B |       50.00 |
| Concurrent_Mediator_128 |  4,804.40 ns |  97.404 ns |  64.427 ns |  52.20 |    1.29 |   10 | 0.7858 |                    - |                - | 0.0229 |   10336 B |       58.73 |
| Concurrent_Plaxion_128  |  8,278.78 ns | 216.533 ns | 143.223 ns |  89.95 |    2.41 |   11 | 0.7782 |                    - |                - | 0.0153 |   10336 B |       58.73 |
| Concurrent_MediatR_128  | 12,586.24 ns | 533.608 ns | 352.949 ns | 136.75 |    4.66 |   12 | 2.6703 |                    - |                - | 0.0916 |   34912 B |      198.36 |
