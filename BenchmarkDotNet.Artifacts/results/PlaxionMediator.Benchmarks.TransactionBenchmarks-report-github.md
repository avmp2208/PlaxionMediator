```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2


```
| Method                                             | Mean         | Error      | StdDev     | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |-------------:|-----------:|-----------:|-------:|--------:|-------:|----------:|------------:|
| Send_NoTransaction                                 |     34.04 ns |   0.711 ns |   0.790 ns |   1.00 |    0.03 | 0.0042 |      56 B |        1.00 |
| Send_TransactionBehavior_NonTransactional_FastPath |    113.96 ns |   2.279 ns |   3.745 ns |   3.35 |    0.13 | 0.0079 |     104 B |        1.86 |
| Send_Transactional_CommitPath                      |    127.06 ns |   2.597 ns |   3.965 ns |   3.73 |    0.14 | 0.0098 |     128 B |        2.29 |
| Send_Transactional_NoOpManager                     |    127.59 ns |   2.265 ns |   1.891 ns |   3.75 |    0.10 | 0.0098 |     128 B |        2.29 |
| Send_Transactional_RollbackPath                    | 33,426.29 ns | 667.041 ns | 998.395 ns | 982.54 |   36.49 | 0.3967 |    5272 B |       94.14 |
