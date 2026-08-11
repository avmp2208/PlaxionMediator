```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-KENNUZ : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                  | Mean        | Error      | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Completed Work Items | Lock Contentions | Gen1   | Allocated | Alloc Ratio |
|------------------------ |------------:|-----------:|-----------:|-------:|--------:|-----:|-------:|---------------------:|-----------------:|-------:|----------:|------------:|
| Concurrent_Mediator_1   |    39.18 ns |   2.745 ns |   1.634 ns |   0.88 |    0.05 |    1 | 0.0134 |                    - |                - |      - |     176 B |        1.00 |
| Concurrent_Plaxion_1    |    44.57 ns |   2.901 ns |   1.919 ns |   1.00 |    0.06 |    1 | 0.0134 |                    - |                - |      - |     176 B |        1.00 |
| Concurrent_MediatR_1    |    73.54 ns |   2.171 ns |   1.135 ns |   1.65 |    0.07 |    2 | 0.0281 |                    - |                - |      - |     368 B |        2.09 |
| Concurrent_Mediator_8   |   224.02 ns |   5.765 ns |   3.813 ns |   5.03 |    0.22 |    3 | 0.0563 |               0.0000 |                - |      - |     736 B |        4.18 |
| Concurrent_Plaxion_8    |   239.00 ns |  10.940 ns |   6.510 ns |   5.37 |    0.25 |    3 | 0.0563 |                    - |                - |      - |     736 B |        4.18 |
| Concurrent_MediatR_8    |   519.07 ns |  15.786 ns |   8.256 ns |  11.66 |    0.50 |    4 | 0.1736 |                    - |                - |      - |    2272 B |       12.91 |
| Concurrent_Plaxion_32   |   937.28 ns |  19.357 ns |  11.519 ns |  21.06 |    0.87 |    5 | 0.2022 |                    - |                - | 0.0019 |    2656 B |       15.09 |
| Concurrent_Mediator_32  |   952.89 ns |  29.297 ns |  19.378 ns |  21.41 |    0.95 |    5 | 0.2031 |               0.0000 |                - | 0.0019 |    2656 B |       15.09 |
| Concurrent_MediatR_32   | 2,126.68 ns | 107.195 ns |  63.790 ns |  47.79 |    2.33 |    6 | 0.6714 |                    - |                - | 0.0038 |    8800 B |       50.00 |
| Concurrent_Mediator_128 | 3,370.45 ns |  48.582 ns |  28.911 ns |  75.74 |    3.07 |    7 | 0.7896 |                    - |                - | 0.0229 |   10336 B |       58.73 |
| Concurrent_Plaxion_128  | 4,560.26 ns | 117.939 ns |  78.009 ns | 102.47 |    4.40 |    8 | 0.7858 |                    - |                - | 0.0229 |   10336 B |       58.73 |
| Concurrent_MediatR_128  | 8,859.42 ns | 513.542 ns | 339.676 ns | 199.08 |   10.75 |    9 | 2.6703 |                    - |                - | 0.0916 |   34912 B |      198.36 |
