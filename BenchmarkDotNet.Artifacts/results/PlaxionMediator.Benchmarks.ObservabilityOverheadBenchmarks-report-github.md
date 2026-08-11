```

BenchmarkDotNet v0.14.0, Windows 11 (10.0.26200.8875)
12th Gen Intel Core i7-12700K, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.202
  [Host]   : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2
  ShortRun : .NET 9.0.7 (9.0.725.31616), X64 RyuJIT AVX2

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------- |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| Publish_NoObserver    | 144.1 ns | 244.5 ns | 13.40 ns |  0.99 |    0.09 | 0.0098 |     128 B |        0.64 |
| Send_NoObserver       | 145.7 ns | 117.7 ns |  6.45 ns |  1.00 |    0.05 | 0.0153 |     200 B |        1.00 |
| Publish_OpenTelemetry | 153.0 ns | 290.0 ns | 15.90 ns |  1.05 |    0.10 | 0.0098 |     128 B |        0.64 |
| Send_OpenTelemetry    | 161.7 ns | 325.1 ns | 17.82 ns |  1.11 |    0.11 | 0.0153 |     200 B |        1.00 |
