```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]   : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  ShortRun : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                             | Mean         | Error        | StdDev    | Ratio    | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------------------------- |-------------:|-------------:|----------:|---------:|--------:|-------:|----------:|------------:|
| Send_NoTransaction                                 |     23.42 ns |     5.230 ns |  0.287 ns |     1.00 |    0.01 | 0.0043 |      56 B |        1.00 |
| Send_TransactionBehavior_NonTransactional_FastPath |     91.60 ns |    10.041 ns |  0.550 ns |     3.91 |    0.05 | 0.0079 |     104 B |        1.86 |
| Send_Transactional_CommitPath                      |     98.30 ns |    50.973 ns |  2.794 ns |     4.20 |    0.11 | 0.0098 |     128 B |        2.29 |
| Send_Transactional_NoOpManager                     |     98.92 ns |    14.213 ns |  0.779 ns |     4.22 |    0.05 | 0.0098 |     128 B |        2.29 |
| Send_Transactional_RollbackPath                    | 25,347.52 ns | 1,694.885 ns | 92.902 ns | 1,082.31 |   11.94 | 0.3967 |    5272 B |       94.14 |
