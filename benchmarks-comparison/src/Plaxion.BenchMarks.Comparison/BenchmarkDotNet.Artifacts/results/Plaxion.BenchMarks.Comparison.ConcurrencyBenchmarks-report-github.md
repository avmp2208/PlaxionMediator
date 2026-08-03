```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-CFKAWA : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                  | Mean        | Error      | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Completed Work Items | Lock Contentions | Gen1   | Allocated | Alloc Ratio |
|------------------------ |------------:|-----------:|-----------:|-------:|--------:|-----:|-------:|---------------------:|-----------------:|-------:|----------:|------------:|
| Concurrent_Mediator_1   |    39.34 ns |   0.749 ns |   0.392 ns |   0.61 |    0.01 |    1 | 0.0134 |                    - |                - |      - |     176 B |        1.00 |
| Concurrent_Plaxion_1    |    64.08 ns |   0.889 ns |   0.588 ns |   1.00 |    0.01 |    2 | 0.0134 |                    - |                - |      - |     176 B |        1.00 |
| Concurrent_MediatR_1    |    78.90 ns |   3.620 ns |   2.394 ns |   1.23 |    0.04 |    3 | 0.0281 |                    - |                - |      - |     368 B |        2.09 |
| Concurrent_Mediator_8   |   242.42 ns |   4.593 ns |   2.733 ns |   3.78 |    0.05 |    4 | 0.0563 |                    - |                - |      - |     736 B |        4.18 |
| Concurrent_Plaxion_8    |   372.95 ns |  10.104 ns |   6.683 ns |   5.82 |    0.11 |    5 | 0.0563 |                    - |                - |      - |     736 B |        4.18 |
| Concurrent_MediatR_8    |   649.61 ns | 160.236 ns | 105.986 ns |  10.14 |    1.58 |    6 | 0.1736 |                    - |                - |      - |    2272 B |       12.91 |
| Concurrent_Mediator_32  |   895.48 ns |  16.622 ns |   9.891 ns |  13.97 |    0.19 |    7 | 0.2031 |               0.0000 |                - | 0.0019 |    2656 B |       15.09 |
| Concurrent_Plaxion_32   | 1,539.19 ns |  46.239 ns |  30.584 ns |  24.02 |    0.50 |    8 | 0.2022 |                    - |                - | 0.0019 |    2656 B |       15.09 |
| Concurrent_MediatR_32   | 2,006.58 ns |  53.509 ns |  31.842 ns |  31.31 |    0.54 |    9 | 0.6714 |                    - |                - | 0.0038 |    8800 B |       50.00 |
| Concurrent_Mediator_128 | 3,691.25 ns |  69.011 ns |  45.647 ns |  57.61 |    0.85 |   10 | 0.7896 |               0.0000 |                - | 0.0229 |   10336 B |       58.73 |
| Concurrent_Plaxion_128  | 5,980.40 ns |  83.757 ns |  49.842 ns |  93.33 |    1.10 |   11 | 0.7858 |                    - |                - | 0.0229 |   10336 B |       58.73 |
| Concurrent_MediatR_128  | 8,907.46 ns | 220.310 ns | 131.103 ns | 139.01 |    2.29 |   12 | 2.6703 |                    - |                - | 0.0916 |   34912 B |      198.36 |
