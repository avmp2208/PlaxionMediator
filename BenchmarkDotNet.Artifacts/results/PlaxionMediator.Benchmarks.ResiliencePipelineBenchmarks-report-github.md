```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]   : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  ShortRun : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                   | Mean     | Error     | StdDev   | Gen0   | Allocated |
|------------------------- |---------:|----------:|---------:|-------:|----------:|
| Send_FullChain_CacheHit  | 154.5 ns |  26.19 ns |  1.44 ns | 0.0122 |     160 B |
| Send_FullChain_CacheMiss | 155.9 ns |  19.55 ns |  1.07 ns | 0.0122 |     160 B |
| Send_CircuitBreakerOnly  | 441.8 ns | 197.46 ns | 10.82 ns | 0.0076 |     104 B |
