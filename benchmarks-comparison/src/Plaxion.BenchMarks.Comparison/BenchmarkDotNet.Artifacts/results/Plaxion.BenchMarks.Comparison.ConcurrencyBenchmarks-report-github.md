```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-TWNJOK : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                  | Mean        | Error      | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Completed Work Items | Lock Contentions | Gen1   | Allocated | Alloc Ratio |
|------------------------ |------------:|-----------:|-----------:|-------:|--------:|-----:|-------:|---------------------:|-----------------:|-------:|----------:|------------:|
| Concurrent_Mediator_1   |    40.89 ns |   3.183 ns |   2.105 ns |   0.86 |    0.05 |    1 | 0.0134 |                    - |                - |      - |     176 B |        1.00 |
| Concurrent_Plaxion_1    |    47.63 ns |   3.317 ns |   1.974 ns |   1.00 |    0.06 |    1 | 0.0134 |                    - |                - |      - |     176 B |        1.00 |
| Concurrent_MediatR_1    |    80.58 ns |   5.742 ns |   3.798 ns |   1.69 |    0.10 |    2 | 0.0281 |                    - |                - |      - |     368 B |        2.09 |
| Concurrent_Mediator_8   |   242.93 ns |  12.660 ns |   8.374 ns |   5.11 |    0.26 |    3 | 0.0563 |                    - |                - |      - |     736 B |        4.18 |
| Concurrent_Plaxion_8    |   252.74 ns |   8.339 ns |   5.516 ns |   5.31 |    0.24 |    3 | 0.0563 |                    - |                - |      - |     736 B |        4.18 |
| Concurrent_MediatR_8    |   534.41 ns |  41.895 ns |  24.931 ns |  11.24 |    0.67 |    4 | 0.1736 |                    - |                - |      - |    2272 B |       12.91 |
| Concurrent_Mediator_32  |   941.73 ns |  63.750 ns |  42.167 ns |  19.80 |    1.16 |    5 | 0.2031 |                    - |                - | 0.0019 |    2656 B |       15.09 |
| Concurrent_Plaxion_32   | 1,060.69 ns |  45.164 ns |  29.873 ns |  22.30 |    1.07 |    5 | 0.2022 |                    - |                - | 0.0019 |    2656 B |       15.09 |
| Concurrent_MediatR_32   | 2,247.53 ns | 117.068 ns |  69.666 ns |  47.26 |    2.34 |    6 | 0.6714 |                    - |                - | 0.0038 |    8800 B |       50.00 |
| Concurrent_Mediator_128 | 3,827.53 ns | 128.738 ns |  76.610 ns |  80.48 |    3.55 |    7 | 0.7896 |                    - |                - | 0.0229 |   10336 B |       58.73 |
| Concurrent_Plaxion_128  | 4,107.76 ns | 136.049 ns |  89.988 ns |  86.37 |    3.88 |    7 | 0.7896 |                    - |                - | 0.0229 |   10336 B |       58.73 |
| Concurrent_MediatR_128  | 8,772.46 ns | 511.756 ns | 304.538 ns | 184.46 |    9.54 |    8 | 2.6703 |                    - |                - | 0.0916 |   34912 B |      198.36 |
