```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-HJCBLN : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                  | Mean        | Error      | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Completed Work Items | Lock Contentions | Gen1   | Allocated | Alloc Ratio |
|------------------------ |------------:|-----------:|----------:|-------:|--------:|-----:|-------:|---------------------:|-----------------:|-------:|----------:|------------:|
| Concurrent_Mediator_1   |    39.72 ns |   1.081 ns |  0.643 ns |   0.66 |    0.01 |    1 | 0.0134 |                    - |                - |      - |     176 B |        1.00 |
| Concurrent_Plaxion_1    |    60.50 ns |   1.046 ns |  0.547 ns |   1.00 |    0.01 |    2 | 0.0134 |                    - |                - |      - |     176 B |        1.00 |
| Concurrent_MediatR_1    |    77.11 ns |   3.443 ns |  2.277 ns |   1.27 |    0.04 |    3 | 0.0281 |                    - |                - |      - |     368 B |        2.09 |
| Concurrent_Mediator_8   |   232.25 ns |   6.395 ns |  4.230 ns |   3.84 |    0.07 |    4 | 0.0563 |                    - |                - |      - |     736 B |        4.18 |
| Concurrent_Plaxion_8    |   374.04 ns |   4.817 ns |  2.867 ns |   6.18 |    0.07 |    5 | 0.0563 |                    - |                - |      - |     736 B |        4.18 |
| Concurrent_MediatR_8    |   529.12 ns |  28.732 ns | 17.098 ns |   8.75 |    0.28 |    6 | 0.1736 |                    - |                - |      - |    2272 B |       12.91 |
| Concurrent_Mediator_32  |   888.85 ns |  30.151 ns | 17.943 ns |  14.69 |    0.31 |    7 | 0.2031 |               0.0000 |                - | 0.0019 |    2656 B |       15.09 |
| Concurrent_Plaxion_32   | 1,429.44 ns |  15.580 ns | 10.305 ns |  23.63 |    0.26 |    8 | 0.2022 |                    - |                - | 0.0019 |    2656 B |       15.09 |
| Concurrent_MediatR_32   | 2,067.46 ns |  64.401 ns | 38.324 ns |  34.17 |    0.67 |    9 | 0.6714 |                    - |                - | 0.0038 |    8800 B |       50.00 |
| Concurrent_Mediator_128 | 3,648.92 ns |  50.752 ns | 33.569 ns |  60.31 |    0.74 |   10 | 0.7896 |               0.0000 |                - | 0.0229 |   10336 B |       58.73 |
| Concurrent_Plaxion_128  | 5,792.46 ns | 121.035 ns | 80.057 ns |  95.74 |    1.50 |   11 | 0.7858 |                    - |                - | 0.0229 |   10336 B |       58.73 |
| Concurrent_MediatR_128  | 8,335.57 ns | 167.198 ns | 99.497 ns | 137.78 |    1.95 |   12 | 2.6703 |                    - |                - | 0.0916 |   34912 B |      198.36 |
