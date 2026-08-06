```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-TIXYSW : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                  | Mean        | Error      | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Completed Work Items | Lock Contentions | Gen1   | Allocated | Alloc Ratio |
|------------------------ |------------:|-----------:|-----------:|-------:|--------:|-----:|-------:|---------------------:|-----------------:|-------:|----------:|------------:|
| Concurrent_Mediator_1   |    39.42 ns |   1.658 ns |   1.096 ns |   0.89 |    0.03 |    1 | 0.0134 |                    - |                - |      - |     176 B |        1.00 |
| Concurrent_Plaxion_1    |    44.38 ns |   1.339 ns |   0.886 ns |   1.00 |    0.03 |    2 | 0.0134 |                    - |                - |      - |     176 B |        1.00 |
| Concurrent_MediatR_1    |    74.44 ns |   1.925 ns |   1.273 ns |   1.68 |    0.04 |    3 | 0.0281 |                    - |                - |      - |     368 B |        2.09 |
| Concurrent_Mediator_8   |   224.88 ns |   4.580 ns |   2.725 ns |   5.07 |    0.11 |    4 | 0.0563 |               0.0000 |                - |      - |     736 B |        4.18 |
| Concurrent_Plaxion_8    |   249.73 ns |   8.731 ns |   5.775 ns |   5.63 |    0.16 |    4 | 0.0563 |                    - |                - |      - |     736 B |        4.18 |
| Concurrent_MediatR_8    |   550.49 ns |  23.106 ns |  15.283 ns |  12.41 |    0.40 |    5 | 0.1736 |                    - |                - |      - |    2272 B |       12.91 |
| Concurrent_Mediator_32  |   872.18 ns |  30.801 ns |  18.329 ns |  19.66 |    0.54 |    6 | 0.2031 |                    - |                - | 0.0019 |    2656 B |       15.09 |
| Concurrent_Plaxion_32   |   884.17 ns |  10.786 ns |   5.641 ns |  19.93 |    0.40 |    6 | 0.2031 |               0.0000 |                - | 0.0019 |    2656 B |       15.09 |
| Concurrent_MediatR_32   | 1,988.73 ns |  65.286 ns |  43.183 ns |  44.83 |    1.26 |    7 | 0.6714 |                    - |                - | 0.0038 |    8800 B |       50.00 |
| Concurrent_Mediator_128 | 3,351.72 ns |  92.772 ns |  55.207 ns |  75.56 |    1.86 |    8 | 0.7896 |                    - |                - | 0.0229 |   10336 B |       58.73 |
| Concurrent_Plaxion_128  | 3,610.68 ns |  78.169 ns |  51.704 ns |  81.39 |    1.90 |    8 | 0.7896 |               0.0000 |                - | 0.0229 |   10336 B |       58.73 |
| Concurrent_MediatR_128  | 8,253.81 ns | 381.456 ns | 252.309 ns | 186.06 |    6.47 |    9 | 2.6703 |                    - |                - | 0.0916 |   34912 B |      198.36 |
