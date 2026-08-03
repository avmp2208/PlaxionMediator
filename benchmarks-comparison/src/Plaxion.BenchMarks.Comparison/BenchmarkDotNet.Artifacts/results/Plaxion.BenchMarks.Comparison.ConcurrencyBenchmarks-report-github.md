```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-ASUFOU : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                  | Mean        | Error      | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Completed Work Items | Lock Contentions | Gen1   | Allocated | Alloc Ratio |
|------------------------ |------------:|-----------:|-----------:|-------:|--------:|-----:|-------:|---------------------:|-----------------:|-------:|----------:|------------:|
| Concurrent_Mediator_1   |    40.60 ns |   1.130 ns |   0.748 ns |   0.52 |    0.01 |    1 | 0.0134 |                    - |                - |      - |     176 B |        1.00 |
| Concurrent_MediatR_1    |    74.77 ns |   2.121 ns |   1.403 ns |   0.95 |    0.02 |    2 | 0.0281 |                    - |                - |      - |     368 B |        2.09 |
| Concurrent_Plaxion_1    |    78.48 ns |   2.998 ns |   1.568 ns |   1.00 |    0.03 |    2 | 0.0134 |                    - |                - |      - |     176 B |        1.00 |
| Concurrent_Mediator_8   |   245.45 ns |   8.242 ns |   5.452 ns |   3.13 |    0.09 |    3 | 0.0563 |                    - |                - |      - |     736 B |        4.18 |
| Concurrent_Plaxion_8    |   509.60 ns |   9.090 ns |   6.012 ns |   6.50 |    0.14 |    4 | 0.0563 |                    - |                - |      - |     736 B |        4.18 |
| Concurrent_MediatR_8    |   560.68 ns |  12.863 ns |   8.508 ns |   7.15 |    0.17 |    5 | 0.1736 |                    - |                - |      - |    2272 B |       12.91 |
| Concurrent_Mediator_32  |   969.28 ns |  17.195 ns |  11.373 ns |  12.35 |    0.27 |    6 | 0.2022 |                    - |                - | 0.0019 |    2656 B |       15.09 |
| Concurrent_Plaxion_32   | 1,912.72 ns |  33.762 ns |  22.332 ns |  24.38 |    0.53 |    7 | 0.2022 |                    - |                - |      - |    2656 B |       15.09 |
| Concurrent_MediatR_32   | 2,086.26 ns |  38.251 ns |  25.301 ns |  26.59 |    0.58 |    8 | 0.6714 |                    - |                - | 0.0038 |    8800 B |       50.00 |
| Concurrent_Mediator_128 | 3,806.10 ns |  95.448 ns |  56.800 ns |  48.51 |    1.13 |    9 | 0.7896 |                    - |                - | 0.0229 |   10336 B |       58.73 |
| Concurrent_Plaxion_128  | 7,938.64 ns | 156.212 ns | 103.324 ns | 101.19 |    2.25 |   10 | 0.7782 |                    - |                - | 0.0153 |   10336 B |       58.73 |
| Concurrent_MediatR_128  | 8,469.80 ns | 341.281 ns | 225.736 ns | 107.96 |    3.39 |   10 | 2.6703 |                    - |                - | 0.0916 |   34912 B |      198.36 |
