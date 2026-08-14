```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2


```
| Method                              | Mean         | Error      | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------ |-------------:|-----------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Send_Baseline_NoAuth                |     25.72 ns |   0.484 ns |   0.662 ns |   1.00 |    0.04 | 0.0018 |      24 B |        1.00 |
| Send_AuthBehavior_NoChecks_FastPath |    103.18 ns |   1.849 ns |   1.729 ns |   4.01 |    0.12 | 0.0055 |      72 B |        3.00 |
| Send_OneCheck_Success               |    174.43 ns |   3.223 ns |   2.857 ns |   6.79 |    0.20 | 0.0079 |     104 B |        4.33 |
| Send_ThreeChecks_Success            |    184.59 ns |   3.609 ns |   3.706 ns |   7.18 |    0.23 | 0.0079 |     104 B |        4.33 |
| Send_OneCheck_Denied_Exception      | 18,662.45 ns | 339.509 ns | 390.979 ns | 725.94 |   23.36 | 0.2136 |    2936 B |      122.33 |
