```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-GDXDUG : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                  | Mean        | Error      | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Completed Work Items | Lock Contentions | Gen1   | Allocated | Alloc Ratio |
|------------------------ |------------:|-----------:|-----------:|-------:|--------:|-----:|-------:|---------------------:|-----------------:|-------:|----------:|------------:|
| Concurrent_Mediator_1   |    42.45 ns |   2.714 ns |   1.795 ns |   0.88 |    0.05 |    1 | 0.0134 |                    - |                - |      - |     176 B |        1.00 |
| Concurrent_Plaxion_1    |    48.07 ns |   3.335 ns |   2.206 ns |   1.00 |    0.06 |    1 | 0.0134 |                    - |                - |      - |     176 B |        1.00 |
| Concurrent_MediatR_1    |    79.29 ns |   5.858 ns |   3.486 ns |   1.65 |    0.10 |    2 | 0.0281 |                    - |                - |      - |     368 B |        2.09 |
| Concurrent_Mediator_8   |   239.27 ns |   5.967 ns |   3.947 ns |   4.99 |    0.23 |    3 | 0.0563 |                    - |                - |      - |     736 B |        4.18 |
| Concurrent_Plaxion_8    |   264.93 ns |  18.591 ns |  12.297 ns |   5.52 |    0.34 |    4 | 0.0563 |                    - |                - |      - |     736 B |        4.18 |
| Concurrent_MediatR_8    |   553.59 ns |  22.548 ns |  14.914 ns |  11.54 |    0.57 |    5 | 0.1736 |                    - |                - |      - |    2272 B |       12.91 |
| Concurrent_Mediator_32  |   898.09 ns |  32.104 ns |  21.235 ns |  18.72 |    0.90 |    6 | 0.2031 |               0.0000 |                - | 0.0019 |    2656 B |       15.09 |
| Concurrent_Plaxion_32   |   950.61 ns |  27.468 ns |  18.168 ns |  19.81 |    0.92 |    6 | 0.2031 |                    - |                - | 0.0019 |    2656 B |       15.09 |
| Concurrent_MediatR_32   | 2,087.57 ns |  46.362 ns |  30.666 ns |  43.51 |    1.96 |    7 | 0.6714 |                    - |                - | 0.0038 |    8800 B |       50.00 |
| Concurrent_Mediator_128 | 3,581.52 ns |  96.587 ns |  57.477 ns |  74.65 |    3.39 |    8 | 0.7858 |                    - |                - | 0.0229 |   10336 B |       58.73 |
| Concurrent_Plaxion_128  | 3,987.13 ns | 206.625 ns | 136.670 ns |  83.10 |    4.47 |    8 | 0.7896 |                    - |                - | 0.0229 |   10336 B |       58.73 |
| Concurrent_MediatR_128  | 9,657.31 ns | 731.867 ns | 435.523 ns | 201.29 |   12.17 |    9 | 2.6703 |                    - |                - | 0.0916 |   34912 B |      198.36 |
