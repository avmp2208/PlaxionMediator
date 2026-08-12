```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  Job-YXHNZT : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

IterationCount=10  LaunchCount=1  WarmupCount=3  

```
| Method                  | Mean        | Error      | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Completed Work Items | Lock Contentions | Gen1   | Allocated | Alloc Ratio |
|------------------------ |------------:|-----------:|-----------:|-------:|--------:|-----:|-------:|---------------------:|-----------------:|-------:|----------:|------------:|
| Concurrent_Mediator_1   |    46.01 ns |   2.028 ns |   1.207 ns |   0.94 |    0.08 |    1 | 0.0134 |                    - |                - |      - |     176 B |        1.00 |
| Concurrent_Plaxion_1    |    49.30 ns |   7.307 ns |   4.348 ns |   1.01 |    0.12 |    1 | 0.0134 |                    - |                - |      - |     176 B |        1.00 |
| Concurrent_MediatR_1    |    86.71 ns |   3.780 ns |   1.977 ns |   1.77 |    0.15 |    2 | 0.0281 |                    - |                - |      - |     368 B |        2.09 |
| Concurrent_Plaxion_8    |   233.42 ns |   4.460 ns |   2.950 ns |   4.77 |    0.39 |    3 | 0.0563 |                    - |                - |      - |     736 B |        4.18 |
| Concurrent_Mediator_8   |   248.39 ns |  10.486 ns |   6.240 ns |   5.07 |    0.43 |    3 | 0.0563 |                    - |                - |      - |     736 B |        4.18 |
| Concurrent_MediatR_8    |   641.89 ns |  84.328 ns |  55.778 ns |  13.11 |    1.52 |    4 | 0.1736 |                    - |                - |      - |    2272 B |       12.91 |
| Concurrent_Mediator_32  |   935.76 ns |  32.140 ns |  21.258 ns |  19.11 |    1.59 |    5 | 0.2031 |                    - |                - | 0.0019 |    2656 B |       15.09 |
| Concurrent_Plaxion_32   |   979.71 ns |  24.304 ns |  16.076 ns |  20.01 |    1.64 |    5 | 0.2022 |                    - |                - | 0.0019 |    2656 B |       15.09 |
| Concurrent_MediatR_32   | 2,395.79 ns |  95.827 ns |  50.120 ns |  48.92 |    4.06 |    6 | 0.6714 |                    - |                - | 0.0038 |    8800 B |       50.00 |
| Concurrent_Plaxion_128  | 3,745.18 ns | 148.303 ns |  98.093 ns |  76.48 |    6.45 |    7 | 0.7858 |                    - |                - | 0.0229 |   10336 B |       58.73 |
| Concurrent_Mediator_128 | 4,211.92 ns | 893.831 ns | 591.214 ns |  86.01 |   13.47 |    7 | 0.7896 |                    - |                - | 0.0229 |   10336 B |       58.73 |
| Concurrent_MediatR_128  | 9,258.87 ns | 344.642 ns | 205.091 ns | 189.07 |   15.75 |    8 | 2.6703 |                    - |                - | 0.0916 |   34912 B |      198.36 |
