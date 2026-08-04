```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-JRUHVO : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                  | Mean        | Error      | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Completed Work Items | Lock Contentions | Gen1   | Allocated | Alloc Ratio |
|------------------------ |------------:|-----------:|-----------:|-------:|--------:|-----:|-------:|---------------------:|-----------------:|-------:|----------:|------------:|
| Concurrent_Mediator_1   |    39.28 ns |   2.558 ns |   1.692 ns |   0.84 |    0.06 |    1 | 0.0134 |                    - |                - |      - |     176 B |        1.00 |
| Concurrent_Plaxion_1    |    46.69 ns |   4.222 ns |   2.512 ns |   1.00 |    0.07 |    1 | 0.0134 |                    - |                - |      - |     176 B |        1.00 |
| Concurrent_MediatR_1    |    80.80 ns |   3.855 ns |   2.550 ns |   1.73 |    0.10 |    2 | 0.0281 |                    - |                - |      - |     368 B |        2.09 |
| Concurrent_Mediator_8   |   225.77 ns |   3.145 ns |   1.645 ns |   4.85 |    0.25 |    3 | 0.0563 |               0.0000 |                - |      - |     736 B |        4.18 |
| Concurrent_Plaxion_8    |   299.23 ns |  54.408 ns |  35.987 ns |   6.42 |    0.81 |    4 | 0.0563 |                    - |                - |      - |     736 B |        4.18 |
| Concurrent_MediatR_8    |   594.20 ns |  51.557 ns |  30.681 ns |  12.76 |    0.90 |    5 | 0.1736 |                    - |                - |      - |    2272 B |       12.91 |
| Concurrent_Mediator_32  |   858.19 ns |  29.829 ns |  19.730 ns |  18.43 |    1.02 |    6 | 0.2031 |                    - |                - | 0.0019 |    2656 B |       15.09 |
| Concurrent_Plaxion_32   | 1,824.39 ns |  72.042 ns |  47.651 ns |  39.17 |    2.21 |    7 | 0.2022 |                    - |                - | 0.0019 |    2656 B |       15.09 |
| Concurrent_MediatR_32   | 2,076.46 ns | 119.271 ns |  78.890 ns |  44.58 |    2.78 |    8 | 0.6714 |                    - |                - | 0.0038 |    8800 B |       50.00 |
| Concurrent_Mediator_128 | 3,546.54 ns |  44.456 ns |  26.455 ns |  76.15 |    3.90 |    9 | 0.7896 |               0.0000 |                - | 0.0229 |   10336 B |       58.73 |
| Concurrent_Plaxion_128  | 3,828.64 ns | 316.631 ns | 165.604 ns |  82.21 |    5.35 |    9 | 0.7858 |                    - |                - | 0.0229 |   10336 B |       58.73 |
| Concurrent_MediatR_128  | 8,600.15 ns | 593.038 ns | 392.258 ns | 184.65 |   12.33 |   10 | 2.6703 |                    - |                - | 0.0916 |   34912 B |      198.36 |
