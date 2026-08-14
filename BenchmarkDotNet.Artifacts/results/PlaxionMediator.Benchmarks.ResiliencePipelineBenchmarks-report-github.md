```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]     : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  DefaultJob : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2


```
| Method                   | Mean     | Error    | StdDev   | Gen0   | Allocated |
|------------------------- |---------:|---------:|---------:|-------:|----------:|
| Send_FullChain_CacheMiss | 202.6 ns |  4.00 ns |  5.34 ns | 0.0122 |     160 B |
| Send_FullChain_CacheHit  | 202.7 ns |  4.08 ns |  4.70 ns | 0.0122 |     160 B |
| Send_CircuitBreakerOnly  | 621.8 ns | 12.46 ns | 18.64 ns | 0.0076 |     104 B |
