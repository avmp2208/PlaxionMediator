```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]   : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  ShortRun : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                              | Mean         | Error       | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------ |-------------:|------------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Send_Baseline_NoAuth                |     33.23 ns |    16.47 ns |   0.903 ns |   1.00 |    0.03 | 0.0018 |      24 B |        1.00 |
| Send_AuthBehavior_NoChecks_FastPath |    137.76 ns |    46.82 ns |   2.567 ns |   4.15 |    0.12 | 0.0055 |      72 B |        3.00 |
| Send_OneCheck_Success               |    218.06 ns |    77.62 ns |   4.255 ns |   6.56 |    0.19 | 0.0079 |     104 B |        4.33 |
| Send_ThreeChecks_Success            |    240.34 ns |    56.40 ns |   3.091 ns |   7.24 |    0.19 | 0.0079 |     104 B |        4.33 |
| Send_OneCheck_Denied_Exception      | 20,395.00 ns | 3,717.34 ns | 203.760 ns | 613.97 |   15.29 | 0.2136 |    2936 B |      122.33 |
